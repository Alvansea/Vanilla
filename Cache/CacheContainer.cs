using System;
using System.Collections.Generic;
using System.Text;

using System.Data;

namespace Vanilla.Data
{
    public class CacheContainer
    {
        private List<string> _TablesCreated;
        public List<string> TableCreated
        {
            get
            {
                if (this._TablesCreated == null)
                {
                    this._TablesCreated = new List<string>();
                }
                return _TablesCreated;
            }
        }

        private Dictionary<string, IDataCache> caches;
        public Dictionary<string, IDataCache> Caches
        {
            get
            {
                if (this.caches == null)
                {
                    this.caches = new Dictionary<string, IDataCache>();
                }
                return caches;
            }
        }

        public void Clear()
        {
            this.Caches.Clear();
            this.TableCreated.Clear();
        }

        public void Clear(string type)
        {
            if (this.Caches.ContainsKey(type))
            {
                this.Caches[type].Clear();
            }
        }

        public void AddCache(Query query, DataObject[] items)
        {
            string type = query.DataType;
            if (!this.Caches.ContainsKey(type))
            {
                IDataCache cache = new ObjectCache(type);
                this.Caches.Add(type, cache);
            }
            this.Caches[type].AddItems(query, items);
        }

        public void AddCache(Query query, DataTable table)
        {
            string type = query.DataType;
            if (!this.Caches.ContainsKey(type))
            {
                IDataCache cache = new TableCache(type);
                this.Caches.Add(type, cache);
            }
            this.Caches[type].AddItems(query, table);
        }

        public DataObject[] GetItems(Query query)
        {
            string type = query.DataType;
            if (this.Caches.ContainsKey(type))
            {
                return this.Caches[type].GetItems(query);
            }
            return null;
        }
    }
}
