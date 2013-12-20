using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Dialect
{
    public class MsSql2005Dialect : SqlDialect
    {
        public new string CreateTable = "CREATE TABLE [$TableName$] ($Columns$ $Constraint$) ON [PRIMARY]";
    }
}
