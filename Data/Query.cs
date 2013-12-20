using System;
using System.Collections.Generic;
using System.Text;

using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace Vanilla.Data
{
    public delegate void ForeachDelegate(DataObject item);

    public class Query : IEquatable<Query>
    {
        #region Properties

        private DataStore Store;
        public string DataType { set; get; }

        private ObjectTable table;
        protected ObjectTable Table
        {
            get { return this.table; }
        }

        /// <summary>
        /// Count  of the rows to be selected
        /// </summary>
        public int RowCount = 0;

        private static string SqlJoin = "{0} JOIN [{1}] AS [{2}] ON [{3}].[{4}] = [{2}].[{5}]";
        private Queue JoinPhrases = new Queue();

        private ConditionSet ConditionSet = null;

        private string orderedColumn = string.Empty;
        public string OrderedColumn
        {
            get { return this.orderedColumn; }
        }
        private bool isDesc = false;
        public bool IsDesc
        {
            get { return this.isDesc; }
        }

        protected bool UseTableCache = true;

        /// <summary>
        /// The index of cached query results in ObjectCache
        /// </summary>
        public List<int> CacheItems;

        #endregion

        #region Constructor

        public Query(DataStore Store, string dataType)
        {
            this.Store = Store;
            this.DataType = dataType;
            this.table = this.Store.Dictionary.GetTable(dataType);
            if (this.table == null)
            {
                string msg = string.Format("Cannot find '{0}' in DataDictionary", dataType);
                throw new Exception(msg);
            }

            this.ConditionSet = new ConditionSet();

            this.UseTableCache = Store.Cfg.UseTableCahce;
            this.CacheItems = new List<int>();
        }

        #endregion

        #region SQL Methods

        public void Join(string joinType, string tableName, string leftCol, string rightCol)
        {
            string dataType = tableName.Substring(0, tableName.IndexOf("@"));
            string sql = string.Format(SqlJoin, joinType.ToUpper(), tableName, dataType, this.DataType, leftCol, rightCol);
            this.JoinPhrases.Enqueue(sql);
        }

        public Query InnerJoin(string dataType, string leftCol, string rightCol)
        {
            ObjectTable table = this.Store.Dictionary.GetTable(dataType);
            this.Join("INNER", table.TableName, leftCol, rightCol);
            return this;
        }

        public Query LeftJoin(string dataType, string leftCol, string rightCol)
        {
            ObjectTable table = this.Store.Dictionary.GetTable(dataType);
            this.Join("LEFT", table.TableName, leftCol, rightCol);
            return this;
        }

        public Query RightJoin(string dataType, string leftCol, string rightCol)
        {
            ObjectTable table = this.Store.Dictionary.GetTable(dataType);
            this.Join("RIGHT", table.TableName, leftCol, rightCol);
            return this;
        }

        public Query FullJoin(string dataType, string leftCol, string rightCol)
        {
            ObjectTable table = this.Store.Dictionary.GetTable(dataType);
            this.Join("FULL", table.TableName, leftCol, rightCol);
            return this;
        }

        public Query Where(string name, string opt, object value)
        {
            this.ConditionSet.And(name, opt, value);
            return this;
        }

        public Query Where(string name, object value)
        {
            return this.Where(name, "=", value);
        }

        public Query And(string name, string opt, object value)
        {
            this.ConditionSet.And(new ConditionNode(name, opt, value));
            return this;
        }

        public Query Or(string name, string opt, object value)
        {
            this.ConditionSet.Or(new ConditionNode(name, opt, value));
            return this;
        }

        public Query OrderBy(string column, bool reverse)
        {
            this.orderedColumn = column;
            this.isDesc = reverse;
            return this;
        }

        public Query OrderBy(string column)
        {
            return this.OrderBy(column, false);
        }

        public Query Mapping(DataObject item)
        {
            if (this.DataType == item.DataType)
            {
                return this;
            }
            ObjectTable eMapping = this.Store.GetMappingTable(this.DataType, item.DataType);
            if (eMapping == null)
            {
                return this;
            }
            ObjectTable e1 = this.Store.Dictionary.GetTable(this.DataType);
            ObjectTable e2 = this.Store.Dictionary.GetTable(item.DataType);
            this.Join("INNER", eMapping.TableName, e1.PrimaryKeys[0], e1.DataType + "_" + e1.PrimaryKeys[0]);
            foreach (string pk in e2.PrimaryKeys)
            {
                this.ConditionSet.And(eMapping.DataType + "." + e2.DataType + "_" + pk, "=", item[pk]);
            }
            return this;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get filter string that will be used to select data by cached DataTable
        /// </summary>
        /// <returns></returns>
        public string GetFilter()
        {
            string filter = this.ConditionSet.Sql;
            foreach (string key in this.ConditionSet.Parameters.Keys)
            {
                filter = filter.Replace(key, "'" + this.ConditionSet.Parameters[key] + "'");
            }
            return filter;
        }

        public DbCommand GetDbCommand(string sqlTemplate)
        {
            ConditionSet set = this.ConditionSet;
            string sql = sqlTemplate;

            sql = sql.Replace("$DataType$", this.DataType);

            if (string.IsNullOrEmpty(set.Sql))
            {
                sql = sql.Replace("$Where$", string.Empty);
            }
            else if(sql.IndexOf("WHERE") >= 0)
            {
                sql = sql.Replace("$Where$", "AND (" + set.Sql + ")");
            }
            else
            {
                sql = sql.Replace("$Where$", "WHERE " + set.Sql);
            }

            if (!string.IsNullOrEmpty(this.OrderedColumn))
            {
                sql += " ORDER BY " + DataUtility.FormatColumnName(this.OrderedColumn);
                if (this.IsDesc)
                {
                    sql += " DESC";
                }
            }

            string tableName = this.Table.TableName;
            while (this.JoinPhrases.Count > 0)
            {
                tableName += " " + this.JoinPhrases.Dequeue();
            }
            sql = sql.Replace("$TableName$", tableName);
            DbCommand cmd = this.Store.Database.GetSqlStringCommand(sql);
            foreach (string key in set.Parameters.Keys)
            {
                object paramValue = set.Parameters[key];
                if (paramValue is string)
                {
                    this.Store.Database.AddInParameter(cmd, key, DbType.String, paramValue.ToString());
                }
                else
                {
                    this.Store.Database.AddInParameter(cmd, key, DbType.Object, paramValue);
                }
            }
            return cmd;
        }

        /// <summary>
        /// Core funtion for quering data
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        protected DataObject[] LoadObject(ConditionSet set)
        {
            DataObject[] items = this.Store.Cache.GetItems(this);
            if (items != null)
            {
                return items;
            }

            string sql;
            if (this.RowCount > 0)
            {
                sql = string.Format(this.Store.Dialect.SelectTop, this.RowCount, "*");
            }
            else
            {
                sql = string.Format(this.Store.Dialect.Select, "*");
            }

            DbCommand cmd = this.GetDbCommand(sql);
            List<DataObject> list = new List<DataObject>();

            if (this.UseTableCache)
            {
                DataSet ds = this.Store.Database.ExecuteDataSet(cmd);
                if (ds.Tables.Count > 0)
                {
                    DataTable table = ds.Tables[0];
                    foreach (DataRow row in table.Rows)
                    {
                        DataObject obj = this.Table.NewObject();
                        obj.Load(row, this.Store, true);
                        list.Add(obj);
                    }
                    items = list.ToArray();
                    this.Store.Cache.AddCache(this, table);
                }
            }
            else
            {
                IDataReader dr = this.Store.Database.ExecuteReader(cmd);
                while (dr.Read())
                {
                    DataObject obj = this.Table.NewObject();
                    obj.Load(dr, this.Store, true);
                    list.Add(obj);
                }
                dr.Close();

                items = list.ToArray();
                this.Store.Cache.AddCache(this, items);
            }

            return items;
        }

        public DataObject[] Select()
        {
            DataObject[] items = this.LoadObject(this.ConditionSet);
            return items;
        }

        public DataObject[] Select(int count)
        {
            if (count > 0)
            {
                this.RowCount = count;
            }
            return this.Select();
        }

        public DataObject SelectTop()
        {
            DataObject[] items = this.Select(1);
            if (items.Length > 0)
            {
                return items[0];
            }
            return null;
        }

        public object[] Select(string column)
        {
            DataObject[] items = this.Select();
            object[] l = new object[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                l[i] = items[i][column];
            }
            return l;
        }

        public void FillObject(DataObject obj)
        {
            string sqlTemplate = string.Format(this.Store.Dialect.SelectTop, 1, "*");
            DbCommand cmd = this.GetDbCommand(sqlTemplate);
            IDataReader reader = this.Store.Database.ExecuteReader(cmd);
            if (reader.Read())
            {
                obj.Load(reader, this.Store, false);
            }
        }

        public int Count()
        {
            string sCount = string.Format(this.Store.Dialect.Count, "*");
            string sql = string.Format(this.Store.Dialect.Select, sCount);
            int count = 0;
            DbCommand cmd = this.GetDbCommand(sql);
            IDataReader dr = this.Store.Database.ExecuteReader(cmd);
            if (dr.Read())
            {
                count = dr.GetInt32(0);
            }
            return count;
        }

        public double Sum(string column)
        {
            try
            {
                column = DataUtility.FormatColumnName(column);
                string sSum = string.Format(this.Store.Dialect.Sum, column);
                string template = string.Empty;

                template = string.Format(this.Store.Dialect.Select, sSum);

                DbCommand cmd = this.GetDbCommand(template);

                double sum = 0;
                IDataReader dr = this.Store.Database.ExecuteReader(cmd);
                if (dr.Read() && !dr.IsDBNull(0))
                {
                    Type type = dr.GetFieldType(0);

                    if (type.Name.Equals("Int32"))
                    {
                        sum = dr.GetInt32(0);
                    }
                    else if (type.Name.Equals("Decimal"))
                    {
                        sum = (double)dr.GetDecimal(0);
                    }
                    else
                    {
                        sum = dr.GetDouble(0);
                    }
                }
                return sum;
            }
            catch (Exception e)
            {
                string msg = string.Format("Query for sum failed. System Message: {0}", e.Message);
                this.Store.Log("Exception", msg);
                return 0;
            }
        }

        public DataTable SelectTable()
        {
            return null;
        }

        public int Delete()
        {
            DbCommand cmd = this.GetDbCommand(this.Store.Dialect.Delete);
            int count = this.Store.Database.ExecuteNonQuery(cmd);

            this.Store.Cache.Clear(this.DataType);

            return count;
        }

        #endregion

        #region MapReduce Methods

        private int Segment = 0;

        public Query SegmentBy(int length)
        {
            if (length < 0)
            {
                throw new Exception("Segment length cannot be less than 0.");
            }
            this.Segment = length;
            return this;
        }

        public string Foreach(ForeachDelegate d)
        {
            Session dm = this.Store.OpenSession();
            string errors = string.Empty;
            string error = string.Empty;
            DataObject[] items = this.Segment == 0 ? this.Select() : this.Select(Segment);
            while (items.Length > 0)
            {
                foreach (DataObject item in items)
                {
                    d(item);
                }
                error = dm.Commit();
                if (!string.IsNullOrEmpty(error))
                {
                    errors += error + "\n";
                }
                if (Segment == 0)
                {
                    break;
                }
                else
                {
                    items = this.Select(Segment);
                }
            }
            return errors;
        }

        #endregion

        #region IEquatable

        public bool Equals(Query query)
        {
            if (!this.DataType.Equals(query.DataType))
            {
                return false;
            }

            if (this.RowCount != query.RowCount)
            {
                return false;
            }

            if(this.RowCount > 0)
            {
                // check the order only when the row count > 0
                if (!this.OrderedColumn.Equals(query.OrderedColumn, StringComparison.CurrentCultureIgnoreCase) || this.IsDesc != query.IsDesc)
                {
                    return false;
                }
            }

            if (this.JoinPhrases.Count != query.JoinPhrases.Count)
            {
                return false;
            }
            else
            {
                foreach (object item in this.JoinPhrases)
                {
                    if (!query.JoinPhrases.Contains(item))
                    {
                        return false;
                    }
                }
            }

            if (!this.ConditionSet.Equals(query.ConditionSet))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
