using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Dialect
{
    public class MySQLDialect : SqlDialect
    {
        public new string SelectTop = "SELECT {1} FROM [$TableName$] AS [$DataType$] $Where$ LIMIT {0}";
    }
}
