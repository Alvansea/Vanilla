using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;

namespace Vanilla.Data
{
    public class ObjectTable : IEquatable<ObjectTable>
    {
        #region Properties

        private string dataType;
        public string DataType
        {
            get { return this.dataType; }
        }

        private string tableName;
        public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(this.tableName))
                {
                    this.tableName = this.DataType + (this.IsMapping ? "@PizzaMapping" : "@Pizza");
                }
                return this.tableName;
            }
            set
            {
                this.tableName = value;
            }
        }


        private Dictionary<string, ObjectColumn> columnDict;
        public Dictionary<string, ObjectColumn>.ValueCollection Columns
        {
            get { return this.columnDict.Values; }
        }

        private List<string> primaryKeys;
        public List<string> PrimaryKeys
        {
            get { return this.primaryKeys; }
        }

        public bool HasPrimaryKey
        {
            get
            {
                return this.PrimaryKeys.Count > 0;
            }
        }

        private List<string> _LazyColumns;
        public List<string> LazyColumns
        {
            get { return this._LazyColumns; }
        }

        public bool HasLazyColumn
        {
            get
            {
                return this.LazyColumns.Count > 0;
            }
        }

        /// <summary>
        /// This property is used only when creating database table, so the efficiency is not a concern
        /// </summary>
        public string PrimaryKeyString
        {
            get
            {
                string pks = string.Empty;
                if (this.HasPrimaryKey)
                {
                    foreach (string pk in this.PrimaryKeys)
                    {
                        pks += "[" + pk + "],";
                    }
                    pks = pks.Remove(pks.Length - 1);
                }
                return pks;
            }
        }

        private bool _IsMapping;
        public bool IsMapping
        {
            get { return this._IsMapping; }
        }

        #endregion

        #region Constructor

        public ObjectTable(string type, string mapping)
        {
            this.dataType = type;
            this.tableName = mapping;
            this.columnDict = new Dictionary<string, ObjectColumn>();
            this.primaryKeys = new List<string>();
            this._LazyColumns = new List<string>();
        }

        public ObjectTable(XmlNode node)
        {
            this.dataType = string.Empty;
            this.tableName = string.Empty;
            this.columnDict = new Dictionary<string, ObjectColumn>();
            this.primaryKeys = new List<string>();
            this._LazyColumns = new List<string>();
            this.ParseXml(node);
        }

        #endregion

        #region Configuration

        public static string GetMappingType(string type1, string type2)
        {
            if (string.IsNullOrEmpty(type1) || string.IsNullOrEmpty(type2))
            {
                return string.Empty;
            }

            int i = string.Compare(type1, type2, true);
            if (i == 0)
            {
                return string.Empty;
            }
            string mappingType = i < 0 ? type1 + "_" + type2 : type2 + "_" + type1;
            return mappingType;
        }

        protected void ParseXml(XmlNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Name == "data" && node.Attributes["type"] != null)
            {
                // parse data type and mapping table
                this.dataType = node.Attributes["type"].Value;
            }
            else if (node.Name == "mapping" && node.Attributes["type1"] != null && node.Attributes["type2"] != null)
            {
                string type1 = node.Attributes["type1"].Value;
                string type2 = node.Attributes["type2"].Value;
                this.dataType = GetMappingType(type1, type2);
                this._IsMapping = true;
            }

            // parse columnDict
            XmlNodeList list = node.SelectNodes("column");
            foreach (XmlNode column in list)
            {
                this.AddColumn(new ObjectColumn(column));
            }

            // parse table name
            if (node.Attributes["table"] != null)
            {
                this.tableName = node.Attributes["table"].Value;
            }
        }

        public void AddColumn(ObjectColumn column)
        {
            if (column == null || this.HasColumn(column.Name))
            {
                return;
            }
            this.columnDict.Add(column.Name, column);
            if (column.IsPrimary)
            {
                this.PrimaryKeys.Add(column.Name);
            }
            if (column.IsLazy)
            {
                this.LazyColumns.Add(column.Name);
            }
        }

        public bool HasColumn(string name)
        {
            return this.columnDict.ContainsKey(name);
        }

        public ObjectColumn GetColumn(string name)
        {
            if (this.HasColumn(name))
            {
                return this.columnDict[name];
            }
            return null;
        }

        public int GetColumnMaxLength(string name)
        {
            ObjectColumn col = this.GetColumn(name);
            if (col == null)
            {
                return 0;
            }
            if (string.IsNullOrEmpty(col.MaxLength))
            {
                return 0;
            }
            if (col.MaxLength == "max")
            {
                return 0;
            }
            return Convert.ToInt32(col.MaxLength);
        }

        public DataObject NewObject()
        {
            DataObject obj = new DataObject(this.DataType);
            foreach (ObjectColumn column in this.Columns)
            {
                obj[column.Name] = column.DefaultValue;
            }
            return obj;
        }

        public DataObject Format(DataObject source)
        {
            DataObject obj = this.NewObject();
            foreach (string key in source.Keys)
            {
                if (obj.ContainsKey(key))
                {
                    obj[key] = source[key];
                }
            }
            return obj;
        }

        public object GetColumnValue(string colName, string text)
        {
            ObjectColumn col = this.GetColumn(colName);
            return col.GetValue(text);
        }

        #endregion

        #region IEquatable

        public bool Equals(ObjectTable table)
        {
            if (table == null)
            {
                return false;
            }
            if (this.DataType != table.DataType)
            {
                return false;
            }
            if (this.TableName != table.TableName)
            {
                return false;
            }
            if (this.Columns.Count != table.Columns.Count)
            {
                return false;
            }
            foreach (ObjectColumn column in this.Columns)
            {
                ObjectColumn column2 = table.GetColumn(column.Name);
                if (column2 == null)
                {
                    return false;
                }
                if (!column.Equals(column2))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Query Methods



        #endregion
    }
}
