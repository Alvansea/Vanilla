using System;
using System.Collections.Generic;
using System.Text;

using System.Data;

namespace Vanilla.Data
{
    /// <summary>
    /// Storing DataQueries and DataObject list
    /// </summary>
    public class ObjectCache : IDataCache
    {
        public string DataType { get; set; }

        protected List<Query> Keys;
        protected List<DataObject> Items;

        public ObjectCache(string dataType)
        {
            this.DataType = dataType;
            this.Keys = new List<Query>();
            this.Items = new List<DataObject>();
        }

        private int AddItem(DataObject item)
        {
            int index = this.Items.IndexOf(item);
            if (index < 0)
            {
                this.Items.Add(item);
                index = this.Items.Count - 1;
            }
            return index;
        }

        public void AddItems(Query query, object items)
        {
            if (this.Keys.IndexOf(query) < 0)
            {
                DataObject[] objects = (DataObject[])items;
                query.CacheItems.Clear();
                foreach (DataObject obj in objects)
                {
                    int index = this.AddItem(obj);
                    query.CacheItems.Add(index);
                }
                this.Keys.Add(query);
            }
        }

        public DataObject[] GetItems(Query query)
        {
            int index = this.Keys.IndexOf(query);
            if (index < 0)
            {
                return null;
            }

            List<DataObject> list = new List<DataObject>();
            foreach (int i in this.Keys[index].CacheItems)
            {
                list.Add(this.Items[i]);
            }

            if (list.Count > 0)
            {
                DataObject[] items = list.ToArray();
                if (!string.IsNullOrEmpty(query.OrderedColumn))
                {
                    DataUtility.Sort(items, query.OrderedColumn, query.IsDesc);
                }
                return items;
            }
            return null;
        }

        public void Clear()
        {
            this.Keys.Clear();
            this.Items.Clear();
        }
    }
}
