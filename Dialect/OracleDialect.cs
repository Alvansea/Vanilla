using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Dialect
{
    public class OracleDialect : SqlDialect
    {
        public new string SelectTop = "SELECT {1} FROM [$TableName$] AS [$DataType$] WHERE ROWNUM <= {0} $Where$";
    }
}
