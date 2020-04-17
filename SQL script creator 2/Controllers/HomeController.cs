using EnergyStarSubmissionTool.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SQL_script_creator_2.Helpers;
using SQL_script_creator_2.Models;
using System.IO;
using System.Web.Hosting;
using System.Web.Configuration;
using Microsoft.Ajax.Utilities;

namespace SQL_script_creator_2.Controllers
{
    public class HomeController : Controller
    {
        public bool isSageChangeExcelDocument = false;
        public bool isCreatingFolderStructureForLfSdk = false;

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SubmitExcelForScripting(ExcelFile excelFile)
        {
            HttpPostedFileBase model = excelFile.UploadedFiles.FirstOrDefault();

            DataTable ds = ExcelUtilities.ExtractSubmissionDataFromExcelTemplate(out var eMessage, model);

            List<string> result = new List<string>();
            bool firstRow = true;

            if (bool.Parse(WebConfigurationManager.AppSettings["isSageChangeExcelDocument"]))
            {
                ExtractAndWriteSqlForSageCountryCodeUpdates(ds, firstRow, result);
            }

            if (bool.Parse(WebConfigurationManager.AppSettings["isCreatingFolderStructureForLfSdk"]))
            {
                ExtractAndCreateNewFolderStructureForLfSdk(ds, firstRow, result);
            }


            return View("Index");
        }


        /// <summary>
        /// Make sure to switch both the project build to 64bit and the IIS compiler to 64bit.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitExcelStandardImport(ExcelFile files)
        {
            List<string> result = new List<string>();
            HttpPostedFileBase std1 = files.UploadedFiles.Where(x => x.FileName.Contains("super")).FirstOrDefault();
            HttpPostedFileBase std2 = files.UploadedFiles.Where(x => !x.FileName.Contains("super")).FirstOrDefault();


            DataTable dsSuper = ExcelUtilities.ExtractSubmissionDataFromExcelTemplate(out var eMessage, std1);
            DataTable dsStandards = ExcelUtilities.ExtractSubmissionDataFromExcelTemplate(out var eMessage2, std2);

            int iterationCount = int.Parse(WebConfigurationManager.AppSettings["iterationCount"].ToString());

            string workingString = "";
            List<string> restTempAndCol = new List<string>();


            //Temp table is required if relationships need to be maintained across documents.
            //Add column to standard table that olds the old standard IDs from Mirobase.
            workingString = SqlTemplate.AddColumnToTable;
            workingString = workingString.Replace("NEWCOLUMN", "standard_id");
            workingString = workingString.Replace("DATATYPE", "int");
            workingString = workingString.Replace("NULLORNOT", "null");

            result.Add(workingString);
            result.Add(SqlTemplate.CreateTempTable);
            workingString = "";



            //Sql template to create.
            var templateString = SqlTemplate.StandardsInsertSqlTemplateWithOutputInsertToTemp;

            //List of fields we want to import. Only need to modify this list in order to add new fields to the import or remove existing fields.
            List<string> fieldNamesToBeImported = new List<string>()
            {
                "standard_number",
                "standard_title",
                "agency",
                "status",
                "Remarks",
                "accepted_by_src",
                "side By Side Status",
                "ics_code",
                "keywords",
                "standard_id"
            };

            bool recordNewlyInsertedValuesToTempTable = true;

            result.AddRange(DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(dsStandards, fieldNamesToBeImported, templateString, "Standards", recordNewlyInsertedValuesToTempTable, iterationCount));

            //Dealing with Superceding data second.
            //List of fields we want to import. Only need to modify this list in order to add new fields to the import or remove existing fields.
            templateString = SqlTemplate.StandardsInsertSqlTemplate;

            List<string> fieldSuperNamesToBeImported = new List<string>()
            {
                "oldStd",
                "superStd"
            };

            recordNewlyInsertedValuesToTempTable = false;

            result.AddRange(DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(dsSuper, fieldSuperNamesToBeImported, templateString, "StandardsSuperseded", recordNewlyInsertedValuesToTempTable, iterationCount));

            //Creating final merge statements to update the super results with the new ids form the standard insert statements.
            List<string> resultMerge = new List<string>();
            workingString = SqlTemplate.MergeStatement;
            workingString = workingString.Replace("TABLETARGET", "StandardsSuperseded");
            workingString = workingString.Replace("TABLESOURCE", "#WorkingTempTable");
            workingString = workingString.Replace("MATCHCONDITION", "t.oldStd = s.oldId");
            workingString = workingString.Replace("FIELDSETTINGOPERATION", "t.oldStd = s.newlyInsertedId");
            resultMerge.Add(workingString);
            workingString = "";

            workingString = SqlTemplate.MergeStatement;
            workingString = workingString.Replace("TABLETARGET", "StandardsSuperseded");
            workingString = workingString.Replace("TABLESOURCE", "#WorkingTempTable");
            workingString = workingString.Replace("MATCHCONDITION", "t.superStd = s.oldId");
            workingString = workingString.Replace("FIELDSETTINGOPERATION", "s.superStd = s.newlyInsertedId");
            resultMerge.Add(workingString);
            workingString = "";
            result.AddRange(resultMerge);

            result.Add(SqlTemplate.DropTempTable);

            //Add column to standard table that olds the old standard IDs from Mirobase.
            workingString = SqlTemplate.DropColumnFromTable;
            workingString = workingString.Replace("TABLE", "Standards");
            workingString = workingString.Replace("DROPPEDCOLUMN", "standard_id");

            result.Add(workingString);
            workingString = "";


            WriteSqlStringListToFile(result, "standardAndSupercedeImportSql");

            return View("Index");
        }

        private static List<string> DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(DataTable excelDataSet,
            List<string> fieldNamesToBeImported, string templateString, string tableName, bool recordNewlyInsertedValuesToTempTable = false, int iterationCount = 2)
        {
            string workingString = ""; //Working string is manipulated to create the SQL statments. Always clear after each SQL statement generation section.
            List<string> result = new List<string>();

            List<int> indexesToBeImported = new List<int>();
            string fieldNames = "";
            string[] fieldNamesAsArray = excelDataSet.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();

            //Creat the SQL that will determine what fields will have values inserted into them.
            int count = iterationCount;
            bool firstRow = true;
            foreach (string field in fieldNamesToBeImported)
            {
                if (count > iterationCount)
                {
                    break;
                }

                if (firstRow)
                {
                    fieldNames += "[" + field + "]";
                    firstRow = false;
                    continue;
                }

                fieldNames += ",[" + field + "]";
                count++;
            }


            //Get a list of index positions that will have values we need to grab from the excel document based on the import fields list above.
            foreach (string s in fieldNamesAsArray)
            {
                if (fieldNamesToBeImported.Contains(s))
                {
                    indexesToBeImported.Add(Array.IndexOf(fieldNamesAsArray, s));
                }
            }

            string tableData = "";
            bool firstDataInsert = true;
            count = iterationCount;
            foreach (DataRow d in excelDataSet.Rows)
            {
                if (count > iterationCount)
                {
                    break;
                }

                workingString = templateString;
                workingString = workingString.Replace("TABLE", tableName);
                workingString = workingString.Replace("TFIELD", fieldNames);

                foreach (int i in indexesToBeImported)
                {
                    if (firstDataInsert)
                    {
                        tableData += "\"" + d[i] + "\"";
                        firstDataInsert = false;
                        continue;
                    }

                    tableData += ", \"" + d[i] + "\"";
                }

                workingString = workingString.Replace("TDATA", tableData);

                result.Add(workingString);
                firstDataInsert = true;
                workingString = "";
                tableData = "";
                count++;
            }

            return result;
        }

        private static void ExtractAndCreateNewFolderStructureForLfSdk(DataTable ds, bool firstRow,
            List<string> result)
        {
            string startingPathForFolderCreation = WebConfigurationManager.AppSettings["StartingPathForFolderCreation"];

            foreach (DataRow d in ds.Rows)
            {
                if (firstRow)
                {
                    firstRow = false;
                    continue;
                }

                result.Add(d[0].ToString());
            }

            result = result.Distinct().ToList();

            foreach (string path in result)
            {
                try
                {
                    // Determine whether the directory exists.
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine("That path exists already.");
                        continue;
                    }

                    // Try to create the directory.
                    DirectoryInfo di = Directory.CreateDirectory(path);
                    Console.WriteLine("The directory was created successfully at {0}.",
                        Directory.GetCreationTime(path));

                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }
            }
        }


        private static void ExtractAndWriteSqlForSageCountryCodeUpdates(DataTable ds, bool firstRow, List<string> result)
        {
            string templateString = SqlTemplate.CountryCodeUpdateSqlTemplate;
            foreach (DataRow d in ds.Rows)
            {
                string workingString = templateString;
                string tableName = d[1].ToString();
                string fieldName = d[0].ToString();

                if (firstRow || fieldName.Contains("AVSCountryCode"))
                {
                    firstRow = false;
                    continue;
                }

                workingString = workingString.Replace("TABLE", tableName);
                workingString = workingString.Replace("TFIELD", fieldName);

                result.Add(workingString);
            }

            try
            {
                using (StreamWriter writer = new StreamWriter("SageCountryCodesUpdateSQL.sql"))
                {
                    foreach (string s in result)
                    {
                        writer.WriteLine(s);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void StandardsExcelDocImport(DataTable ds, bool firstRow, List<string> result)
        {
            string templateString = SqlTemplate.CountryCodeUpdateSqlTemplate;
            foreach (DataRow d in ds.Rows)
            {
                string workingString = templateString;
                string tableName = d[1].ToString();
                string fieldName = d[0].ToString();

                if (firstRow || fieldName.Contains("AVSCountryCode"))
                {
                    firstRow = false;
                    continue;
                }

                workingString = workingString.Replace("TABLE", tableName);
                workingString = workingString.Replace("TFIELD", fieldName);

                result.Add(workingString);
            }

            WriteSqlStringListToFile(result, "StandardsExcelDocImport");
        }

        private static void WriteSqlStringListToFile(List<string> sqlStringListToWriteResult, string newFileName)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter("C:\\" + newFileName + ".sql"))
                {
                    foreach (string s in sqlStringListToWriteResult)
                    {
                        writer.WriteLine(s);
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