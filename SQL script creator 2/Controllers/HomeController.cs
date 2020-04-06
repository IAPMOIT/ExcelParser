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
        public bool isCreatingFolderStructureForLfSdk = true;

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SubmitExcelForScripting(ExcelFile excelFile)
        {
            HttpPostedFileBase model = excelFile.UploadedFile;

            DataTable ds = ExcelUtilities.ExtractSubmissionDataFromExcelTemplate(out var eMessage, model);

            List<string> result = new List<string>();
            bool firstRow = true;

            if (isSageChangeExcelDocument)
            {
                ExtractAndWriteSqlForSageCountryCodeUpdates(ds, firstRow, result);
            }

            if (isCreatingFolderStructureForLfSdk)
            {
                ExtractAndCreateNewFolderStructureForLfSdk(ds, firstRow, result);
            }


            return View("Index");
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
    }
}