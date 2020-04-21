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

            //Temp table is required if relationships need to be maintained across documents.
            //Add column to standard table that olds the old standard IDs from Mirobase.
            result.Add(SqlGenerators.AddColumnToTable("Standards","standard_id", "int", "null"));
            result.Add(SqlTemplate.CreateTempTable);
            

            //Sql template to create.
            var templateString = SqlTemplate.StandardsInsertSqlTemplateWithOutputInsertToTemp;

            WriteUtilties.WriteSqlStringListToFile(result, "standardAndSupercedeImportSql");
            result.Clear();

            //List of fields we want to import. Only need to modify this list in order to add new fields to the import or remove existing fields.
            var fieldNamesToBeImported = new TupleList<string, string>()
            {
                {"standard_number", "StandardNumber"},
                {"standard_title", "Title"},
                {"agency","StandardAgency"},
                //{"status","StandardStatusId"},
                {"Remarks","Remarks"},
                {"date_ok_src","AcceptedBySrc"},
                //{"side By Side Status",""}, TBD.
                //{"ics_code",""}, -- Not currently in the MyPLC Standard model yet.
                {"keywords","SearchTags"},
                {"standard_id","standard_id"},
            };

            if (bool.Parse(WebConfigurationManager.AppSettings["addImportExceptionsForStandards"]))
            {
                result.AddRange(DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(dsStandards,
                    fieldNamesToBeImported, templateString, "Standards", iterationCount, true));
            }
            else
            {
                result.AddRange(DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(dsStandards,
                    fieldNamesToBeImported, templateString, "Standards", iterationCount));
            }

            WriteUtilties.WriteSqlStringListToFile(result, "standardAndSupercedeImportSql");
            result.Clear();

            //Dealing with Superceding data second.
            //List of fields we want to import. Only need to modify this list in order to add new fields to the import or remove existing fields.
            templateString = SqlTemplate.StandardsInsertSqlTemplate;

            var fieldSuperNamesToBeImported = new TupleList<string, string>()
            {
                {"oldStd","PreviousStandard_Id"},
                {"superStd","SupersededByStandard_Id"}
            };

            result.AddRange(DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(dsSuper, fieldSuperNamesToBeImported, templateString, "StandardsSuperseded", iterationCount));

            WriteUtilties.WriteSqlStringListToFile(result, "standardAndSupercedeImportSql");
            result.Clear();

            //Creating final merge statements to update the super results with the new ids form the standard insert statements.
            result.Add(SqlGenerators.CreateMergeStatement("StandardsSuperseded", "#WorkingTempTable", "t.PreviousStandard_Id = s.oldId", "t.PreviousStandard_Id = s.newlyInsertedId"));
            result.Add(SqlGenerators.CreateMergeStatement("StandardsSuperseded", "#WorkingTempTable", "t.SupersededByStandard_Id = s.oldId", "t.SupersededByStandard_Id = s.newlyInsertedId"));

            result.Add(SqlTemplate.DropTempTable);

            //Drop column to standard table that olds the old standard IDs from Mirobase.
            result.Add(SqlGenerators.RemoveColumnFromTable("Standards", "standard_id"));

            WriteUtilties.WriteSqlStringListToFile(result, "standardAndSupercedeImportSql");
            result.Clear();

            return View("Index");
        }

        private static List<string> DynamicallyExtractExcelDocumentDataAndCreateSqlInsertStatement(
            DataTable excelDataSet,
            TupleList<string, string> fieldNamesToBeImported, string templateString, string tableName,
            int iterationCount = 2, bool addImportExceptionsForStandards = false)
        {
            string workingString = ""; //Working string is manipulated to create the SQL statments. Always clear after each SQL statement generation section.
            List<string> result = new List<string>();

            List<int> indexesToBeImported = new List<int>();
            string newFieldNames = null;
            string[] fieldNamesAsArray = excelDataSet.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();

            //Create the SQL that will determine what fields will have values inserted into them.
            bool firstRow = true;
            foreach (Tuple<string, string> field in fieldNamesToBeImported)
            {
                if (firstRow)
                {
                    newFieldNames += "[" + field.Item2 + "]";
                    firstRow = false;
                    continue;
                }

                newFieldNames += ",[" + field.Item2 + "]";
            }

            foreach (var item in fieldNamesToBeImported)
            {
                for (int i = 0; i < fieldNamesAsArray.Length; i++)
                {
                    if (item.Item1 == fieldNamesAsArray[i])
                    {
                        indexesToBeImported.Add(i);
                    }
                }
            }

            string tableData = "";
            bool firstDataInsert = true;
            int count = 0;
            foreach (DataRow d in excelDataSet.Rows)
            {
                if (count > iterationCount && iterationCount != 0)
                {
                    break;
                }

                workingString = templateString;
                workingString = workingString.Replace("TABLE", tableName);

                if (addImportExceptionsForStandards)
                {
                    newFieldNames =
                        AddBusinessLogicExtensions
                            .AddExceptionsForStandardFields(
                                newFieldNames); //This adds exceptions in the data that Mirobase doesn't hold.
                }

                workingString = workingString.Replace("TFIELD", newFieldNames);

                foreach (int i in indexesToBeImported)
                {
                    if (firstDataInsert)
                    {
                        if (string.IsNullOrWhiteSpace(d[i].ToString()))
                        {
                            tableData += "null";
                        }
                        else
                        {
                            tableData += "'" + d[i] + "'";
                        }

                        firstDataInsert = false;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(d[i].ToString()))
                    {
                        tableData += ",null";
                    }
                    else
                    {
                        tableData += ", '" + d[i] + "'";
                    }
                        
                }

                if (addImportExceptionsForStandards)
                {
                    tableData = AddBusinessLogicExtensions.AddExceptionsForStandardValues(tableData);
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

            WriteUtilties.WriteSqlStringListToFile(result, "StandardsExcelDocImport");
        }
    }
}