using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQL_script_creator_2.Models;

namespace SQL_script_creator_2.Helpers
{
    public static class AddBusinessLogicExtensions
    {

        private static TupleList<string, string> ExceptionsForStandards = new TupleList<string, string>()
        {
            {"StandardYear","2019"},
            {"CountryIntent","USA"},
            {"StandardStatusId","1"},
            {"IsHarmonized","0"},
            {"IsDeleted","0"},

        };

        public static string AddExceptionsForStandardFields(string fieldsSoFar)
        {
            foreach (Tuple<string, string> exceptionsForStandard in ExceptionsForStandards)
            {
                fieldsSoFar += ", [" + exceptionsForStandard.Item1 + "]";
            }
            return fieldsSoFar;
        }

        public static string AddExceptionsForStandardValues(string valuesSoFar)
        {
            foreach (Tuple<string, string> exceptionsForStandard in ExceptionsForStandards)
            {
                valuesSoFar += ", '" + exceptionsForStandard.Item2 + "'";
            }
            return valuesSoFar;
        }
    }
}
