using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Dialect
{
    public class SqlDialect
    {
        public string CreateTable = "CREATE TABLE [$TableName$] ($Columns$ $Constraint$)";
        public string DropTable = "DROP TABLE [$TableName$]";
        public string PrimaryKey = ",CONSTRAINT [PK_$TableName$] PRIMARY KEY CLUSTERED ($PrimaryKey$)";

        public string Select = "SELECT {0} FROM [$TableName$] AS [$DataType$] $Where$";
        public string SelectDistinct = "SELECT DISTINCT {0} FROM [$TableName$] AS [$DataType$] $Where$";
        public string SelectTop = "SELECT TOP {0} {1} FROM [$TableName$] AS [$DataType$] $Where$";

        public string CheckTable = "SELECT * FROM sys.objects WHERE object_id = object_id(N'[$TableName$]') AND type in (N'U')";
        public string CheckPK = "SELECT * FROM sys.objects WHERE object_id = object_id(N'[$PK$]') AND type in (N'PK')";

        public string Insert = "INSERT INTO [$TableName$] ($Columns$) VALUES ($Values$)";
        public string InsertSelect = "INSERT INTO {0}($Columns$) SELECT $Values$ FROM [{1}]";
        public string InsertOrUpdate = "IF NOT EXISTS (SELECT * FROM [$TableName$] $Where$) BEGIN INSERT INTO [$TableName$] ($Columns$) VALUES ($Values$) END ELSE BEGIN UPDATE [$TableName$] SET $Assignment$ $Where$ END";
        public string Delete = "DELETE FROM [$TableName$] $Where$";

        public string TableExists = "IF EXISTS (SELECT * FROM sys.objects WHERE object_id = object_id(N'[$TableName$]') AND type in (N'U')) BEGIN {0} END";
        public string TableNotExists = "IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = object_id(N'[$TableName$]') AND type in (N'U')) BEGIN {0} END";

        public string Sum = "SUM({0})";
        public string Count = "COUNT({0})";
    }
}
