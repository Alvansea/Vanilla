using System;
using System.Collections.Generic;
using System.Text;

using System.Data;

namespace Vanilla.Data
{
    public class TableCache : IDataCache
    {
        public string DataType { get; set; }

        protected List<Query> Keys;
        protected DataTable DataTable;

        public TableCache(string type)
        {
            this.DataType = type;
            this.Keys = new List<Query>();
        }

        public void AddItems(Query query, object items)
        {
            if (this.Keys.IndexOf(query) < 0)
            {
                DataTable table = (DataTable)items;
                if (this.DataTable == null)
                {
                    this.DataTable = table;
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        query.CacheItems.Add(i);
                    }
                }
                else
                {
                    int index = 0;
                    while (table.Rows.Count > 0)
                    {
                        DataRow row = table.Rows[0];
                        index = this.DataTable.Rows.IndexOf(row);
                        if (index < 0)
                        {
                            index = this.DataTable.Rows.Count;
                            this.DataTable.Rows.Add(row.ItemArray);
                        }
                        query.CacheItems.Add(index);

                        table.Rows.Remove(row);
                    }
                }
                this.Keys.Add(query);
            }
        }

        public DataObject[] GetItems(Query query)
        {
            if (this.DataTable == null)
            {
                return null;
            }

            List<DataObject> list = new List<DataObject>();
            int index = this.Keys.IndexOf(query);
            if (index >= 0)
            {
                foreach (int i in this.Keys[index].CacheItems)
                {
                    DataRow row = this.DataTable.Rows[i];
                    DataObject obj = new DataObject(this.DataType);
                    obj.Load(row);
                    list.Add(obj);
                }
            }
            else if (query.RowCount >= 1 && string.IsNullOrEmpty(query.OrderedColumn))  // non-ordered "select top" will be checked here
            {
                DataRow[] rows = this.DataTable.Select(query.GetFilter());
                for(int i = 0; i < rows.Length; i++)
                {
                    DataObject obj = new DataObject(this.DataType);
                    obj.Load(rows[i]);
                    list.Add(obj);
                    if (i == query.RowCount - 1)
                    {
                        break;
                    }
                }
                if (list.Count <= 0)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            DataObject[] items = list.ToArray();
            if (!string.IsNullOrEmpty(query.OrderedColumn))
            {
                DataUtility.Sort(items, query.OrderedColumn, query.IsDesc);
            }
            return items;
        }

        public void Clear()
        {
            this.Keys.Clear();
            this.DataTable = null;
        }
    }
}
