using System;
using System.Collections.Generic;
using System.Text;

using System.Data;
using System.Data.Common;
using System.Xml;
using Microsoft.Practices.EnterpriseLibrary.Data;

using Vanilla.Dialect;

namespace Vanilla.Data
{
    public class DataStore
    {
        #region Properties

        /// <summary>
        /// Global Configuration
        /// </summary>
        private Configuration cfg;
        public Configuration Cfg
        {
            get
            {
                if (cfg == null)
                {
                    cfg = new Configuration();
                }
                return this.cfg;
            }
        }

        private SqlDialect dialect;
        public SqlDialect Dialect
        {
            get { return this.dialect; }
        }

        public IEventLogger Logger = null;

        public string DatabaseName { get; set; }
        public Database Database { get; set; }

        public string DictionaryPath { get; set; }

        public ICacheAdapter CacheContainer { get; set; }

        private DataDictionary dictionary;
        public DataDictionary Dictionary
        {
            get { return dictionary; }
        }

        public string CacheID { get; set; }
        private CacheContainer cache;
        public CacheContainer Cache
        {
            get
            {
                if (CacheContainer != null)
                {
                    cache = (CacheContainer)CacheContainer.Get(this.CacheID);
                }
                if (cache == null)
                {
                    cache = new CacheContainer();
                    if (CacheContainer != null)
                    {
                        CacheContainer.Add(this.CacheID, cache, TimeSpan.FromDays(1));
                    }
                }
                return cache;
            }
        }

        /// <summary>
        /// For fail-safe of dictionary updating process. 
        /// While updating the dictionary, the renamed table will be stored here. If the error occurs in any step, the renamed table will be recovered.
        /// </summary>
        private Dictionary<string, string> UpdateRecords;

        #endregion

        #region Constructor

        public DataStore(string dbName, string dictionaryPath, ICacheAdapter cache)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new Exception("Dictionary file does not exists!");
            }

            this.UpdateCfg();

            this.DatabaseName = dbName;
            this.Database = DatabaseFactory.CreateDatabase(dbName);

            this.DictionaryPath = dictionaryPath;
            this.dictionary = new DataDictionary(this.DictionaryPath);
            foreach (ObjectTable table in this.dictionary.Tables)
            {
                this.CreateTable(table);
            }

            this.CacheContainer = cache;
            this.CacheID = Guid.NewGuid().ToString();

            this.UpdateRecords = new Dictionary<string, string>();
        }

        #endregion

        #region Common Methods

        public void InitLogger(IEventLogger logger)
        {
            this.Logger = logger;
        }

        protected void UpdateCfg()
        {
            this.dialect = DialectFactory.CreateDialect(this.Cfg.DialectType);
        }

        public void InitConfiguration(Configuration cfg)
        {
            this.cfg = cfg;
            this.UpdateCfg();
        }

        public void Log(string type, string msg)
        {
            if (this.Logger != null)
            {
                string log = string.Format("[{0}] {1}", type.ToUpper(), msg);
                this.Logger.Log(log);
            }
        }

        public Session OpenSession()
        {
            Session session = new Session(this);
            return session;
        }

        public Query From(string dataType)
        {
            Query query = new Query(this, dataType);
            return query;
        }

        public DataObject NewObject(string type)
        {
            ObjectTable table = this.Dictionary.GetTable(type);
            if (table != null)
            {
                DataObject obj = table.NewObject();
                return obj;
            }
            return null;
        }

        /// <summary>
        /// Load lazy columns for the DataObject
        /// </summary>
        /// <param name="obj"></param>
        public void FillObject(DataObject obj)
        {
            ObjectTable table = this.Dictionary.GetTable(obj.DataType);
            if (table == null)
            {
                throw new InvalideDataTypeException(obj.DataType);
            }

            if (!table.HasPrimaryKey)
            {
                return;
            }

            Query query = new Query(this, obj.DataType);
            foreach (string pk in table.PrimaryKeys)
            {
                query.And(pk, "=", obj[pk]);
            }

            query.FillObject(obj);
        }

        #endregion

        #region DataDictionary Management

        /// <summary>
        /// Potential risk - failure in updating table or saving to file will cause the environment goto chaos >_<
        /// </summary>
        /// <param name="newDictionary"></param>
        /// <returns></returns>
        public bool UpdateDictionary(DataDictionary newDictionary)
        {
            if (newDictionary == null || newDictionary.Count <= 0)
            {
                return false;
            }

            bool sqlError = false;

            foreach (ObjectTable newTable in newDictionary.Tables)
            {
                ObjectTable oldTable = this.Dictionary.GetTable(newTable.DataType);
                if (oldTable != null && !oldTable.Equals(newTable))
                {
                    if (this.UpdateTable(newTable))
                    {
                        this.Cache.Clear(newTable.DataType);
                    }
                    else
                    {
                        sqlError = true;
                        this.Log("Error", "Updating table <" + oldTable.DataType + "> failed.");
                        break;
                    }
                }
            }

            if (sqlError)
            {
                // reverse database changes
                foreach (string key in this.UpdateRecords.Keys)
                {
                    this.DropTable(key);
                    this.RenameTable(UpdateRecords[key], key);
                }
                this.UpdateRecords.Clear();
                return false;
            }
            else
            {
                this.SaveDictionary(newDictionary);
                this.UpdateRecords.Clear();
                return true;
            }
        }

        /// <summary>
        /// Save new dictionary to file
        /// </summary>
        /// <param name="newDictionary"></param>
        protected void SaveDictionary(DataDictionary newDictionary)
        {
            // bake old dictionary file
            string bakPath = this.DictionaryPath.Replace(".xml", "_" + DataUtility.TimeStamp() + ".xml");
            this.dictionary.Xml.Save(bakPath);

            // update dictionary file
            this.dictionary = newDictionary;
            this.dictionary.Xml.Save(this.DictionaryPath);
        }

        protected bool UpdateTable(ObjectTable table)
        {
            this.Cache.TableCreated.Clear();

            ObjectTable old = this.Dictionary.GetTable(table.DataType);

            string ts = "_" + DataUtility.TimeStamp();
            string name = table.TableName;
            string bakName = name + ts;

            this.UpdateRecords.Add(name, bakName);

            if (this.RenameTable(name, bakName))
            {
                this.CreateTable(table);
            }
            else
            {
                return false;
            }

            // copy data from old table to new table
            string sqlTemplate = string.Format(Dialect.InsertSelect, name, bakName);
            string columns = string.Empty;
            string values = string.Empty;

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (ObjectColumn newCol in table.Columns)
            {
                columns += string.Format("[{0}],", newCol.Name);

                if (!string.IsNullOrEmpty(newCol.Evaluation))   // check evaluation first
                {
                    values += newCol.Evaluation + ",";
                }
                else if (old.GetColumn(newCol.Name) != null)    // copy value from old table
                {
                    values += string.Format("[{0}],", newCol.Name);
                }
                else // set default value
                {
                    string key = "@" + newCol.Name;
                    values += key + ",";
                    parameters.Add(key, newCol.DefaultValue);
                }
            }
            columns = columns.Remove(columns.Length - 1);
            values = values.Remove(values.Length - 1);
            string sql = sqlTemplate.Replace("$Columns$", columns).Replace("$Values$", values);

            DbCommand cmd = this.Database.GetSqlStringCommand(sql);
            foreach (ObjectColumn col in table.Columns)
            {
                string key = "@" + col.Name;
                if (parameters.ContainsKey(key))
                {
                    this.Database.AddInParameter(cmd, key, DataUtility.GetDbType(col.Type), parameters[key]);
                }
            }
            try
            {
                this.Database.ExecuteNonQuery(cmd);
                return true;
            }
            catch (Exception e)
            {
                string msg = string.Format("Migrating data from {0} to {1} faided. System Message: {2}", bakName, name, e.Message);
                this.Log("Exception", msg);
                return false;
            }
        }

        protected bool RenameTable(string name, string newName)
        {
            try
            {
                DbCommand renameTable = this.Database.GetStoredProcCommand("sp_rename");
                this.Database.AddInParameter(renameTable, "objname", DbType.String, name);
                this.Database.AddInParameter(renameTable, "newname", DbType.String, newName);
                this.Database.ExecuteNonQuery(renameTable);

                string sql = Dialect.CheckPK.Replace("$PK$", "PK_" + name);
                DbCommand checkPK = this.Database.GetSqlStringCommand(sql);
                IDataReader dr = this.Database.ExecuteReader(checkPK);
                if (dr.Read())
                {
                    DbCommand renamePK = this.Database.GetStoredProcCommand("sp_rename");
                    this.Database.AddInParameter(renamePK, "objname", DbType.String, "PK_" + name);
                    this.Database.AddInParameter(renamePK, "newname", DbType.String, "PK_" + newName);
                    this.Database.ExecuteNonQuery(renamePK);
                }

                return true;
            }
            catch (Exception e)
            {
                string msg = string.Format("Renaming table from '{0}' to '{1}' failed. System Message: {2}", name, newName, e.Message);
                this.Log("Exception", msg);
                return false;
            }
        }

        #endregion

        #region Table Management

        /// <summary>
        /// Create a table with the table name and suffix specified
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public bool CreateTable(ObjectTable table)
        {
            string tableName = table.TableName;
            if (this.Cache.TableCreated.Contains(tableName))
            {
                return true;
            }
            else
            {
                this.Cache.TableCreated.Add(tableName);
            }

            string sql = string.Empty;
            if (table.Columns.Count <= 0)
            {
                return false;
            }

            string templateTab = string.Format(Dialect.TableNotExists, Dialect.CreateTable);
            string templateCol = "[{0}] [{1}] {2} {3},";
            string templateCon = Dialect.PrimaryKey;

            // check columns
            string columns = string.Empty;
            foreach (ObjectColumn column in table.Columns)
            {
                string len = string.IsNullOrEmpty(column.MaxLength) ? string.Empty : "(" + column.MaxLength + ")";
                string isNull = column.AllowNulls ? "NULL" : "NOT NULL";
                string col = string.Format(templateCol, column.Name, column.Type, len, isNull);

                columns += col;
            }
            columns = columns.Remove(columns.Length - 1);
            sql = templateTab.Replace("$Columns$", columns);

            // check constraint
            string constraint = string.Empty;
            if (table.HasPrimaryKey)
            {
                constraint = templateCon.Replace("$TableName$", tableName);
                constraint = constraint.Replace("$PrimaryKey$", table.PrimaryKeyString);
            }
            sql = sql.Replace("$Constraint$", constraint);

            sql = sql.Replace("$TableName$", tableName);

            DbCommand cmd = this.Database.GetSqlStringCommand(sql);
            this.Log("Operation", "Create Table " + tableName);
            this.Database.ExecuteNonQuery(cmd);

            return true;
        }

        public bool DropTable(string tableName)
        {
            string sql = string.Format(Dialect.TableExists, Dialect.DropTable);
            sql = sql.Replace("$TableName$", tableName);
            DbCommand cmd = this.Database.GetSqlStringCommand(sql);
            this.Log("Operation", "Drop Table " + tableName);
            try
            {
                this.Database.ExecuteNonQuery(cmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Caution! The cleared tables could not be recovered!
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public bool ClearBakeTables(string dataType)
        {
            ObjectTable table = this.Dictionary.GetTable(dataType);
            string sql = string.Format("select name from sys.objects where name like '{0}_%'", table.TableName);
            DbCommand cmd = this.Database.GetSqlStringCommand(sql);
            IDataReader dr = this.Database.ExecuteReader(cmd);
            while (dr.Read())
            {
                string tablename = dr.GetString(0);
                bool result = this.DropTable(tablename);
                if (result == false)
                {
                    return result;
                }
            }
            return true;
        }

        #endregion

        #region Mapping Management

        public ObjectTable GetMappingTable(string type1, string type2)
        {
            string type = ObjectTable.GetMappingType(type1, type2);
            ObjectTable e = this.Dictionary.GetTable(type);
            return e;
        }

        public DataObject GetMappingObject(DataObject item1, DataObject item2)
        {
            ObjectTable tMapping = this.GetMappingTable(item1.DataType, item2.DataType);
            if (tMapping == null)
            {
                return null;
            }

            DataObject mapping = tMapping.NewObject();
            ObjectTable t1 = this.Dictionary.GetTable(item1.DataType);
            ObjectTable t2 = this.Dictionary.GetTable(item2.DataType);
            foreach (string pk in t1.PrimaryKeys)
            {
                mapping[t1.DataType + "_" + pk] = item1[pk];
            }
            foreach (string pk in t2.PrimaryKeys)
            {
                mapping[t2.DataType + "_" + pk] = item2[pk];
            }
            return mapping;
        }

        #endregion
    }
}
