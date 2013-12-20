using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Dialect
{
    public class DialectFactory
    {
        public const string MsSql2005 = "MsSql2005";
        public const string MySQL = "MySQL";
        public const string Oracle = "Oracle";
        public const string Default = "default";

        public static SqlDialect CreateDialect(string type)
        {
            SqlDialect dialect = null;
            switch (type)
            {
                case MsSql2005:
                    dialect = new MsSql2005Dialect();
                    break;
                case MySQL:
                    dialect = new MySQLDialect();
                    break;
                case Oracle:
                    dialect = new OracleDialect();
                    break;
                default:
                    dialect = new SqlDialect();
                    break;
            }
            return dialect;
        }
    }
}
