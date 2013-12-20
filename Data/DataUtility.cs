using System;
using System.Collections.Generic;
using System.Text;

using System.Data;
using System.Text.RegularExpressions;

namespace Vanilla.Data
{
    public class DataUtility
    {
        private static string FormatColumnName(Match m)
        {
            return "[" + m.Value + "]";
        }

        public static string FormatColumnName(string column)
        {
            return Regex.Replace(column, @"\b\w+\b", DataUtility.FormatColumnName);
        }

        public static void Sort(DataObject[] items, string column, bool reverse)
        {
            DataComparer c = new DataComparer(column, reverse);
            Array.Sort(items, c);
        }

        public static void Merge(DataObject[] sourceArray, ref DataObject[] destArray)
        {
            if (sourceArray.Length > 0)
            {
                Array.Resize<DataObject>(ref destArray, destArray.Length + sourceArray.Length);
                Array.Copy(sourceArray, 0, destArray, destArray.Length - sourceArray.Length, sourceArray.Length);
            }
        }

        public static DataObject[] GetPage(DataObject[] source, int pageSize, int index)
        {
            if (source.Length <= pageSize)
            {
                return source;
            }
            int start = Math.Min(index * pageSize, source.Length - 1);
            int len = Math.Min(start + pageSize, source.Length) - start;
            DataObject[] dest = new DataObject[len];
            Array.Copy(source, start, dest, 0, len);
            return dest;
        }

        public static string TimeStamp()
        {
            DateTime now = DateTime.Now;
            string ts = string.Format("{0:u}", DateTime.Now).Replace("-", "").Replace(" ", "").Replace(":", "");
            return ts;
        }

        public static object ValidateValue(string type, object value)
        {
            if (value == null)
            {
                return value;
            }
            type = type.ToLower();
            switch (type)
            {
                case "uniqueidentifier":
                    try
                    {
                        return new Guid(value.ToString());
                    }
                    catch
                    {
                        return Guid.Empty;
                    }
                case "nvarchar":
                case "varchar":
                    return value.ToString();
                case "datetime":
                    return Convert.ToDateTime(value);
                case "decimal":
                    return Convert.ToDecimal(value);
                case "bigint":
                    return Convert.ToInt64(value);
                case "int":
                    return Convert.ToInt32(value);
                case "smallint":
                    return Convert.ToInt16(value);
                case "tinyint":
                    return Convert.ToByte(value);
                case "bit":
                    return Convert.ToBoolean(value);
                default:
                    return value;
            }
        }

        public static DbType GetDbType(string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "uniqueidentifier":
                    return DbType.Guid;
                case "nvarchar":
                    return DbType.String;
                case "datetime":
                    return DbType.DateTime;
                case "decimal":
                    return DbType.Decimal;
                case "float":
                    return DbType.Double;
                case "bigint":
                    return DbType.Int64;
                case "int":
                    return DbType.Int32;
                case "smallint":
                    return DbType.Int16;
                case "tinyint":
                    return DbType.Byte;
                case "bit":
                    return DbType.Boolean;
                default:
                    return DbType.Object;
            }
        }

        #region DataComparer(internal class)

        internal class DataComparer : IComparer<DataObject>
        {
            private string _Column = string.Empty;
            private bool _Reverse = false;

            public DataComparer(string column, bool reverse)
            {
                this._Column = column;
                this._Reverse = reverse;
            }

            public int Compare(DataObject x, DataObject y)
            {
                int c = string.Compare(x.DataType, y.DataType, true);
                if (c != 0)
                {
                    return c;
                }

                try
                {
                    IComparable cx = (IComparable)(x[_Column]);
                    IComparable cy = (IComparable)(y[_Column]);

                    if (this._Reverse)
                    {
                        return cy.CompareTo(cx);
                    }
                    else
                    {
                        return cx.CompareTo(cy);
                    }
                }
                catch
                {
                    return 0;
                }
            }
        }

        #endregion
    }
}
