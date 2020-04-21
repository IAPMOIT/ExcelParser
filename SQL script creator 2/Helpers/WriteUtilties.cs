using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Configuration;
using SQL_script_creator_2.Helpers;

namespace SQL_script_creator_2.Controllers
{
    public static class WriteUtilties
    {
        public static void WriteSqlStringListToFile(List<string> sqlStringListToWriteResult, string newFileName)
        {

            List<List<string>> dataToWrite = new List<List<string>>();

            try
            {
                string writePath = WebConfigurationManager.AppSettings["FileWritePath"];
                int maxLinesToWrite = int.Parse(WebConfigurationManager.AppSettings["maxLinesToWrite"]);

                if (sqlStringListToWriteResult.Count > maxLinesToWrite)
                {
                    string serverName = WebConfigurationManager.AppSettings["serverName"]; ;
                    string databaseName = WebConfigurationManager.AppSettings["databaseName"];
                    string batchCommand =
                        "@echo off ECHO % USERNAME % started the batch process at % TIME %  > output.txt for %% f in (*.sql) do (sqlcmd.exe - S servername - E - d databasename - i %% f >> output.txt) pause";

                    batchCommand = batchCommand.Replace("servername", serverName);
                    batchCommand = batchCommand.Replace("databasename", databaseName);

                    using (StreamWriter writer = new StreamWriter(writePath + "RunAllSql.bat"))
                    {
                        writer.WriteLine(batchCommand);
                    }

                    dataToWrite = SplitListUtility.SplitList(sqlStringListToWriteResult, maxLinesToWrite);
                }
                else
                {
                    dataToWrite.Add(sqlStringListToWriteResult);
                }


                for (var i = 0; i < dataToWrite.Count; i++)
                {
                    var dtw = dataToWrite[i];

                    using (StreamWriter writer = new StreamWriter(writePath + newFileName + " " + i +".sql"))
                    {
                        foreach (string s in dtw)
                        {
                            writer.WriteLine(s);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}