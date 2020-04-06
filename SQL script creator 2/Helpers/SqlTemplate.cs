using System;
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
    }
}