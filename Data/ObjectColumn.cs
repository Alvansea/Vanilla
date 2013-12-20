using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;

namespace Vanilla.Data
{
    public class ObjectColumn : IEquatable<ObjectColumn>
    {
        public string Name = string.Empty;
        public string Type = string.Empty;
        public string MaxLength = string.Empty;
        public bool IsPrimary = false;

        public bool AllowNulls = false;
        public object DefaultValue = null;
        public bool IsLazy = false;
        public string Evaluation = string.Empty;

        public ObjectColumn(string name, string type, string maxLength, bool isPrimary)
        {
            this.Name = name;
            this.Type = type;
            this.MaxLength = maxLength;
            this.DefaultValue = this.GetValue(null);
            this.IsPrimary = isPrimary;
        }

        public ObjectColumn(XmlNode node)
        {
            this.Name = node.Attributes["name"].Value;
            this.Type = node.Attributes["type"].Value;
            this.DefaultValue = this.GetValue(null);

            if (node.Attributes["length"] != null)
            {
                this.MaxLength = node.Attributes["length"].Value;
            }
            if (node.Attributes["allownulls"] != null)
            {
                this.AllowNulls = Convert.ToBoolean(node.Attributes["allownulls"].Value);
            }
            if (node.Attributes["default"] != null)
            {
                this.DefaultValue = this.GetValue(node.Attributes["default"].Value);
            }
            if (node.Attributes["primary"] != null)
            {
                this.IsPrimary = Convert.ToBoolean(node.Attributes["primary"].Value);
            }
            if (node.Attributes["lazy"] != null)
            {
                this.IsLazy = Convert.ToBoolean(node.Attributes["lazy"].Value);
            }
            if (node.Attributes["eval"] != null)
            {
                this.Evaluation = node.Attributes["eval"].Value;
            }
        }

        public object GetValue(string text)
        {
            text = text == null ? string.Empty : text;
            switch (this.Type)
            {
                case "uniqueidentifier":
                    try { return new Guid(text); }
                    catch { return Guid.Empty; }
                case "nvarchar":
                case "text":
                    return text;
                case "datetime":
                    try { return Convert.ToDateTime(text); }
                    catch { return DateTime.Now; }
                case "decimal":
                    try { return Convert.ToDecimal(text); }
                    catch { return new decimal(0); }
                case "float":
                case "money":
                    try { return Convert.ToDouble(text); }
                    catch { return 0; }
                case "bit":
                    try { return Convert.ToBoolean(text); }
                    catch { return false; }
                case "smallint":
                    try { return Convert.ToInt16(text); }
                    catch { return (short)0; }
                case "tinyint":
                    try { return Convert.ToByte(text); }
                    catch { return (byte)0; }
                case "bigint":
                    try { return Convert.ToInt64(text); }
                    catch { return (long)0; }
                default:
                    if (this.Type.IndexOf("int") >= 0)
                    {
                        try { return Convert.ToInt32(text); }
                        catch { return 0; }
                    }
                    break;
            }
            return null;
        }

        /// <summary>
        /// Default Value does not affect the database schema, so it won't be compared
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool Equals(ObjectColumn column)
        {
            if (column == null)
            {
                return false;
            }
            if (!this.Name.Equals(column.Name))
            {
                return false;
            }
            if (!this.Type.Equals(column.Type))
            {
                return false;
            }
            if (this.MaxLength != column.MaxLength)
            {
                return false;
            }
            if (this.IsPrimary != column.IsPrimary)
            {
                return false;
            }
            if (this.AllowNulls != column.AllowNulls)
            {
                return false;
            }
            return true;
        }

        public ObjectColumn Clone()
        {
            ObjectColumn col = new ObjectColumn(this.Name, this.Type, this.MaxLength, this.IsPrimary);
            col.AllowNulls = this.AllowNulls;
            col.DefaultValue = this.DefaultValue;
            col.IsLazy = this.IsLazy;
            return col;
        }
    }
}
