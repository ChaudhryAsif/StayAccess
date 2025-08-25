using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.Extensions
{
    public static class DbContextExtensions
    {
        public static void AddTemporalTable(this MigrationBuilder builder, string tableName)
        {
            builder.Sql($@"ALTER TABLE dbo.{tableName}
    ADD
    PeriodStart datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        DEFAULT CAST('0001-01-01T00:00:00.0000000' AS datetime2)
,
    PeriodEnd datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
        DEFAULT CAST('9999-12-31 23:59:59.9999999' AS datetime2),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd);

            ALTER TABLE dbo.{tableName}
    SET(SYSTEM_VERSIONING = ON(HISTORY_TABLE = dbo.{tableName}History));");
        }

        public static void RemoveTemporalTable(this MigrationBuilder builder, string tableName)
        {
            builder.Sql($@"ALTER TABLE dbo.{tableName} SET (SYSTEM_VERSIONING = OFF)
                        DROP TABLE dbo.{tableName}History;

DECLARE @PeriodStartConstraintName NVARCHAR(MAX)
SET @PeriodStartConstraintName = 

(SELECT [Name] FROM sys.objects
WHERE type_desc LIKE '%DEFAULT_CONSTRAINT' and [name] like '%{tableName}%PeriodStar%')
if (LEN(@PeriodStartConstraintName) > 0)
BEGIN
EXEC('ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT '+ @PeriodStartConstraintName)
ALTER TABLE [dbo].[{tableName}] DROP COLUMN [PeriodStart]
END

SET @PeriodEndConstraintName = 
(SELECT [Name] FROM sys.objects
WHERE type_desc LIKE '%DEFAULT_CONSTRAINT' and [name] like '%{tableName}%PeriodEnd%')
if (LEN(@PeriodEndConstraintName) > 0)
BEGIN
EXEC('ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT '+ @PeriodEndConstraintName)
ALTER TABLE [dbo].[{tableName}] DROP COLUMN [PeriodEnd]
END");
            
        }
    }
}
