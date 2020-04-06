using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Web;

namespace EnergyStarSubmissionTool.Utilities
{
    public static class ExcelUtilities
    {
        public static DataTable ExtractSubmissionDataFromExcelTemplate(out string eMessage, HttpPostedFileBase _excelFilePath)
        {
            DataTable ds = null;
            eMessage = string.Empty;

            if (_excelFilePath.ContentLength > 0)
            {
                //Parse excel document
                string query = "SELECT * FROM [Sheet1$]";

                var target = new MemoryStream();

                try
                {
                    _excelFilePath.InputStream.CopyTo(target);
                    string fileType = Path.GetExtension(_excelFilePath.FileName);
                    ds = ConvertExcelDocumentToDataTable(target.ToArray(), query, fileType); //hydrate from excel
                }
                catch (Exception e)
                {
                    {
                        eMessage = e.Message;
                        return ds;
                    }
                }
            }
            return ds;
        }

        public static DataTable ConvertExcelDocumentToDataTable(byte[] fileBytes, string queryString, string fileExtension)
        {
            var filename = Path.GetTempFileName();

            // Assuming that fileBytes is a byte[] containing what you read from your database        
            System.IO.File.WriteAllBytes(filename, fileBytes);

            var connection = string.Empty;

            if (fileExtension == ".xls")
            {
                connection = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={filename};Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1;TypeGuessRows=0;ImportMixedTypes=Text\"";
            }
            else if (fileExtension == ".xlsx")
            {
                connection = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filename};Extended Properties=\"Excel 12.0;HDR=Yes;IMEX=2\"";
            }
            else
            {
                throw new ArgumentException(nameof(fileExtension), "Unsupported file type");
            }

            //var connection = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filename + ";Extended Properties=\"Excel 12.0;HDR=YES\"";




            OleDbConnection oledbConn = new OleDbConnection(connection);
            DataTable dt = new DataTable();
            try
            {

                oledbConn.Open();
                using (OleDbCommand cmd = new OleDbCommand(queryString, oledbConn))
                {
                    OleDbDataAdapter oleda = new OleDbDataAdapter { SelectCommand = cmd };
                    DataSet ds = new DataSet();
                    oleda.Fill(ds);

                    dt = ds.Tables[0];
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //Cleanup
                oledbConn.Close();
                File.Delete(filename);
            }

            return dt;

        }
    }
}