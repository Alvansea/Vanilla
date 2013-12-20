using System;
using System.Collections.Generic;
using System.Text;

using System.Data;

namespace Vanilla.Data
{
    public class DataObject : Dictionary<string, object>, IEquatable<DataObject>
    {
        public string DataType { set; get; }

        private DataStore Environment;
        private List<string> LazyColumns;

        private bool IsLazy
        {
            get { return this.LazyColumns.Count > 0; }
        }

        public new object this[string key]
        {
            get
            {
                if (!this.ContainsKey(key))
                {
                    return null;
                }
                if (this.IsLazy && this.LazyColumns.Contains(key))
                {
                    this.LoadAllColumns(this.Environment);
                }
                return base[key];
            }
            set
            {
                // make sure the object is loaded when assign new value to a column, otherwise when saving object to DB, the new value will be overridden by Load() method
                // this check will not cause performance issue for the no-environment object
                if (this.IsLazy)
                {
                    this.LoadAllColumns(this.Environment);
                }
                base[key] = value;
            }
        }

        public DataObject(string type)
        {
            this.DataType = type;
            this.LazyColumns = new List<string>();
        }

        public void Load(DataRow dr)
        {
            int count = dr.Table.Columns.Count;
            string name;
            for (int i = 0; i < count; i++)
            {
                name = dr.Table.Columns[i].ColumnName;
                base[name] = dr.ItemArray[i];
            }
        }

        public void Load(DataRow dr, DataStore env, bool lazy)
        {
            ObjectTable table = env.Dictionary.GetTable(this.DataType);
            if (table == null)
            {
                throw new InvalideDataTypeException(this.DataType);
            }

            this.Environment = env;
            this.LazyColumns.Clear();

            if (table.HasLazyColumn && lazy)
            {
                int count = dr.Table.Columns.Count;
                string name;
                for (int i = 0; i < count; i++)
                {
                    name = dr.Table.Columns[i].ColumnName;
                    if (table.LazyColumns.Contains(name))
                    {
                        this.LazyColumns.Add(name);
                    }
                    else
                    {
                        base[name] = dr.ItemArray[i];
                    }
                }
            }
            else
            {
                this.Load(dr);
            }
        }

        public void Load(IDataReader dr, DataStore env, bool lazy)
        {
            ObjectTable table = env.Dictionary.GetTable(this.DataType);
            if (table == null)
            {
                throw new InvalideDataTypeException(this.DataType);
            }

            this.Environment = env;
            this.LazyColumns.Clear();

            int count = dr.FieldCount;
            string name;

            if (table.HasLazyColumn && lazy)
            {
                for (int i = 0; i < count; i++)
                {
                    name = dr.GetName(i);
                    if (table.LazyColumns.Contains(name))
                    {
                         this.LazyColumns.Add(name);
                    }
                    else
                    {
                        base[name] = dr.GetValue(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    name = dr.GetName(i);
                    base[name] = dr.GetValue(i);
                }
            }
        }

        /// <summary>
        /// This method will load all columns, including lazy columns to this object
        /// Object with no primary key or being already loaded will be ignored
        /// </summary>
        /// <param name="env"></param>
        protected void LoadAllColumns(DataStore env)
        {
            if (env != null)
            {
                env.FillObject(this);
            }
        }

        public object GetBaseValue(string key)
        {
            return base[key];
        }

        public bool Equals(DataObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.DataType != this.DataType)
            {
                return false;
            }
            if (obj.Keys.Count != this.Keys.Count)
            {
                return false;
            }
            foreach (string key in obj.Keys)
            {
                if (!this.ContainsKey(key))
                {
                    return false;
                }
                if (!this.GetBaseValue(key).Equals(obj.GetBaseValue(key)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
