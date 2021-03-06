﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SQL_script_creator_2.Helpers
{
    public static class SqlTemplate
    {
        public static string CountryCodeUpdateSqlTemplate
        {
            get { return 
                    @"UPDATE [dbo].[TABLE] SET [TABLE].[TFIELD] = T2.[NewCountryCode] FROM #SY_Country [T2] JOIN [dbo].[TABLE] [T1] on [T1].[TFIELD] = [T2].[OldCountryCode]";
            }
        }

        public static string StandardsInsertSqlTemplateWithOutputInsertToTemp
        {
            get
            {
                return
                    @"INSERT INTO [dbo].[TABLE] (TFIELD) OUTPUT INSERTED.[standard_id], INSERTED.[Id] INTO #WorkingTempTable VALUES (TDATA) GO";
            }
        }

        public static string StandardsInsertSqlTemplate
        {
            get
            {
                return
                    @"INSERT INTO [dbo].[TABLE] (TFIELD) VALUES (TDATA) GO";
            }
        }

        public static string CreateTempTable
        {
            get
            {
                return
                    @"CREATE TABLE #WorkingTempTable (oldId int, newlyInsertedId int)";
            }
        }

        public static string DropTempTable
        {
            get
            {
                return
                    @"DROP TABLE #WorkingTempTable";
            }
        }

        public static string AddColumnToTable
        {
            get
            {
                return
                    @"IF NOT EXISTS(SELECT * FROM   sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[TABLE]')AND name = 'NEWCOLUMN') BEGIN ALTER TABLE [dbo].[TABLE] ADD [NEWCOLUMN] DATATYPE NULLORNOT END";

                
            }
        }

        public static string DropColumnFromTable
        {
            get
            {
                return
                    @"IF EXISTS(SELECT * FROM   sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[TABLE]')AND name = 'DROPPEDCOLUMN') BEGIN ALTER TABLE [dbo].[TABLE] DROP COLUMN [DROPPEDCOLUMN] END";
                
            }
        }

        public static string MergeStatement
        {
            get
            {
                return
                    @"MERGE [TABLETARGET] t USING [TABLESOURCE] s ON (MATCHCONDITION) WHEN MATCHED THEN UPDATE SET [FIELDSETTINGOPERATION];";
            }
        }



    }
}