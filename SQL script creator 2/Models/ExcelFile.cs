using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SQL_script_creator_2.Models
{
    public class ExcelFile
    {
        public List<HttpPostedFileBase> UploadedFiles { set; get; }
    }
}