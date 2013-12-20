using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

using System.Data;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace Vanilla.Data
{
    public class Session
    {
        #region Properties

        public DataStore Store { set; get; }

        public DataDictionary Dictionary
        {
            get { return this.Store.Dictionary; }
        }

        public CacheContainer Cache
        {
            get { return this.Store.Cache; }
        }

        public bool CacheEnabled { get; set; }

        public Database Database
        {
            get { return this.Store.Database; }
        }

        private Queue<DbCommand> commands;
        public Queue<DbCommand> Commands
        {
            get
            {
                if (this.commands == null)
                {
                    this.commands = new Queue<DbCommand>();
                }
                return this.commands;
            }
        }

        private bool isTransactional = false;
        public bool IsTransactional
        {
            get { return this.isTransactional; }
        }

        #endregion

        #region Contructor

        public Session(DataStore environment)
        {
            this.Store = environment;
            this.CacheEnabled = true;
        }

        #endregion    

        #region Common Methods

        protected ObjectTable GetObjectTable(DataObject obj)
        {
            ObjectTable table = this.Dictionary.GetTable(obj.DataType);
            if (table == null)
            {
                throw new InvalideDataTypeException(obj.DataType);
            }
            return table;
        }

        public Query From(string dataType)
        {
            Query query = new Query(this.Store, dataType);
            return query;
        }

        #endregion

        #region Transaction Management

        public void BeginTransaction()
        {
            this.isTransactional = true;
        }

        /// <summary>
        /// Commit all the DbCommands
        /// </summary>
        /// <returns></returns>
        public string Commit()
        {
            int count = 0;
            try
            {
                while (this.Commands.Count > 0)
                {
                    count++;
                    DbCommand comm = this.Commands.Dequeue();
                    this.Database.ExecuteNonQuery(comm);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            this.isTransactional = false;
            return string.Empty;
        }

        #endregion

        #region Insert/Update/Delete Methods

        protected void SaveObject(DataObject obj, string sqlTemplate)
        {
            ObjectTable table = this.GetObjectTable(obj);

            // initialize sql parts
            string columns = string.Empty;
            string values = string.Empty;
            string assignments = string.Empty;
            foreach (ObjectColumn column in table.Columns)
            {
                columns += "[" + column.Name + "],";
                values += "@" + column.Name + ",";
                assignments += "[" + column.Name + "]=@" + column.Name + ",";
            }

            string where = "WHERE ";
            for (int i = 0; i < table.PrimaryKeys.Count; i++)
            {
                if (i == 0)
                {
                    where += string.Format("[{0}]=@{0}", table.PrimaryKeys[i]);
                }
                else
                {
                    where += string.Format(" AND [{0}]=@{0}", table.PrimaryKeys[i]);
                }
            }

            string sql = sqlTemplate
                .Replace("$TableName$", table.TableName)
                .Replace("$Columns$", columns.Remove(columns.Length - 1))
                .Replace("$Values$", values.Remove(values.Length - 1))
                .Replace("$Assignment$", assignments.Remove(assignments.Length - 1))
                .Replace("$Where$", where);

            // init command and parameters
            DbCommand cmd = this.Database.GetSqlStringCommand(sql);
            foreach (ObjectColumn column in table.Columns)
            {
                string name = "@" + column.Name;
                DbType type = DataUtility.GetDbType(column.Type);
                object value = obj[column.Name];
                this.Database.AddInParameter(cmd, name, type, value);
            }

            this.Commands.Enqueue(cmd);
        }

        /// <summary>
        /// Insert a new record of the obj with the primary key checked. If the record exists, then update the record.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public void SaveObject(DataObject obj)
        {
            if (obj == null)
            {
                return;
            }

            ObjectTable table = this.GetObjectTable(obj);
            
            this.Cache.Clear(obj.DataType);
            string template = table.HasPrimaryKey ? this.Store.Dialect.InsertOrUpdate : this.Store.Dialect.Insert;
            this.SaveObject(obj, template);
        }

        public string DeleteObject(DataObject item)
        {
            ObjectTable table = this.GetObjectTable(item);
            Query query = this.From(item.DataType);
            if (table.HasPrimaryKey)
            {
                foreach (string key in table.PrimaryKeys)
                {
                    query.And(key, "=", item[key]);
                }
            }
            else
            {
                foreach (string key in item.Keys)
                {
                    query.And(key, "=", item[key]);
                }
            }
            int count = query.Delete();

            return string.Empty;
        }

        #endregion

        #region Mapping Management

        public void Map(DataObject item1, DataObject item2)
        {
            DataObject mapping = this.Store.GetMappingObject(item1, item2);
            if (mapping == null)
            {
                throw new Exception("Cannot get mapping types");
            }
            
            this.SaveObject(mapping);
        }

        public string DeleteMapping(DataObject item1, DataObject item2)
        {
            DataObject mapping = this.Store.GetMappingObject(item1, item2);
            if (mapping != null)
            {
                return this.DeleteObject(mapping);
            }
            return string.Empty;
        }

        public string DeleteMapping(DataObject item, string type)
        {
            ObjectTable tMapping = this.Store.GetMappingTable(type, item.DataType);
            if (tMapping == null)
            {
                return string.Empty;
            }

            Query query = new Query(this.Store, tMapping.DataType);
            ObjectTable e = this.Dictionary.GetTable(item.DataType);
            foreach (string pk in e.PrimaryKeys)
            {
                query.And(e.DataType + "_" + pk, "=", item[pk]);
            }
            int count = query.Delete();
            return string.Empty;
        }

        #endregion

        #region Export/Import

        public string Export(string[] types)
        {
            XmlDocument xml = new XmlDocument();
            XmlDeclaration dec = xml.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            xml.AppendChild(dec);
            XmlNode dataset = xml.AppendChild(xml.CreateElement("dataset"));

            foreach (string type in types)
            {
                XmlNode data = xml.CreateElement("data");
                XmlAttribute attrib = xml.CreateAttribute("type");
                attrib.Value = type;
                data.Attributes.Append(attrib);

                DataObject[] items = this.From(type).Select();
                foreach (DataObject item in items)
                {
                    XmlNode row = xml.CreateElement("row");
                    foreach (string key in item.Keys)
                    {
                        XmlAttribute prop = xml.CreateAttribute(key);
                        prop.Value = item[key].ToString();
                        row.Attributes.Append(prop);
                    }
                    data.AppendChild(row);
                }
                dataset.AppendChild(data);
            }
            return xml.OuterXml;
        }

        public void Import(string xmlContent)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlContent);

            XmlNodeList nodes = xml.SelectNodes("/dataset/data");
            foreach (XmlNode node in nodes)
            {
                string type = node.Attributes["type"].Value;
                ObjectTable table = this.Dictionary.GetTable(type);
                foreach (XmlNode row in node.ChildNodes)
                {
                    DataObject item = this.Store.NewObject(type);
                    foreach (XmlAttribute attrib in row.Attributes)
                    {
                        item[attrib.Name] = table.GetColumnValue(attrib.Name, attrib.Value);
                        this.SaveObject(item);
                    }
                }
            }
        }

        #endregion
    }
}
