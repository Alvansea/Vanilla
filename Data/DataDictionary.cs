using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;

namespace Vanilla.Data
{
    public class DataDictionary : Dictionary<string, ObjectTable>
    {
        private XmlDocument _Xml;
        public XmlDocument Xml
        {
            get
            {
                if (this._Xml == null)
                {
                    this._Xml = new XmlDocument();
                }
                return this._Xml;
            }
        }

        public Dictionary<string, ObjectTable>.ValueCollection Tables
        {
            get { return this.Values; }
        }

        public DataDictionary(string filePath)
        {
            this.Load(filePath);
        }

        public DataDictionary(XmlDocument xml)
        {
            this.Load(xml);
        }

        protected bool Load(XmlDocument xml)
        {
            try
            {
                this.Clear();
                this._Xml = xml;
                this.ParseXml();
                return true;
            }
            catch
            {
                this.Clear();
                return false;
            }
        }

        protected bool Load(string filePath)
        {
            try
            {
                this.Clear();
                this.Xml.Load(filePath);
                this.ParseXml();
                return true;
            }
            catch
            {
                this.Clear();
                return false;
            }
        }

        protected void ParseXml()
        {
            XmlNodeList tableNodes = this.Xml.SelectNodes("/database/data");
            foreach (XmlNode node in tableNodes)
            {
                ObjectTable table = new ObjectTable(node);
                this.Add(table);
            }

            XmlNodeList mappingNodes = this.Xml.SelectNodes("/database/mapping");
            foreach (XmlNode node in mappingNodes)
            {
                ObjectTable mapping = new ObjectTable(node);
                this.InitMapping(mapping);
                this.Add(mapping);
            }
        }

        protected void InitMapping(ObjectTable mapping)
        {
            string[] types = mapping.DataType.Split(new char[] { '_' });
            if (types.Length < 2 || !this.ContainsKey(types[0]) || !this.ContainsKey(types[1]))
            {
                return;
            }

            ObjectTable t1 = this.GetTable(types[0]);
            ObjectTable t2 = this.GetTable(types[1]);
            if (!t1.HasPrimaryKey || !t2.HasPrimaryKey)
            {
                return;
            }

            foreach (string pk in t1.PrimaryKeys)
            {
                ObjectColumn column = t1.GetColumn(pk).Clone();
                column.Name = t1.DataType + "_" + column.Name;
                mapping.AddColumn(column);
            }
            foreach (string pk in t2.PrimaryKeys)
            {
                ObjectColumn column = t2.GetColumn(pk).Clone();
                column.Name = t2.DataType + "_" + column.Name;
                mapping.AddColumn(column);
            }
        }

        public void Add(ObjectTable table)
        {
            if (this.ContainsKey(table.DataType))
            {
                this[table.DataType] = table;
            }
            else
            {
                this.Add(table.DataType, table);
            }
        }

        public ObjectTable GetTable(string dataType)
        {
            if (this.ContainsKey(dataType))
            {
                return this[dataType];
            }
            return null;
        }

        public new ObjectTable this[string key]
        {
            get
            {
                if(this.ContainsKey(key))
                {
                    return base[key];
                }
                return null;
            }
            set
            {
                base[key] = value;
            }
        }
    }
}
