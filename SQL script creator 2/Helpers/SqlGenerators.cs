using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQL_script_creator_2.Helpers
{
    public static class SqlGenerators
    {
        /// <summary>
        /// Drops a specified column from the specified table.
        /// </summary>
        /// <param name="tableName">The table name you want to take action upon.</param>
        /// <param name="columnToDrop">The column that you want to drop from the table.</param>
        /// <returns>SQL string statement</returns>
        public static string RemoveColumnFromTable(string tableName, string columnToDrop)
        {
            string workingString;
            workingString = SqlTemplate.DropColumnFromTable;
            workingString = workingString.Replace("[TABLE]", tableName);
            workingString = workingString.Replace("DROPPEDCOLUMN", columnToDrop);
            return workingString;
        }

        /// <summary>
        /// Creates a merge statement for updating a target table from source data when specified condition matches.
        /// </summary>
        /// <param name="targetTable">The table name you want to act upon.</param>
        /// <param name="targetSource">The table you're taking data from.</param>
        /// <param name="matchCondition">The condition that specifies when you want to trigger action.</param>
        /// <param name="fieldSettingOperation">The action you want to take when the match condition passes.</param>
        /// <returns>SQL string statement</returns>
        public static string CreateMergeStatement(string targetTable, string targetSource, string matchCondition, string fieldSettingOperation)
        {
            string workingString;
            List<string> resultMerge = new List<string>();
            workingString = SqlTemplate.MergeStatement;
            workingString = workingString.Replace("TABLETARGET", targetTable);
            workingString = workingString.Replace("TABLESOURCE", targetSource);
            workingString = workingString.Replace("MATCHCONDITION", matchCondition);
            workingString = workingString.Replace("[FIELDSETTINGOPERATION]", fieldSettingOperation);
            return workingString;

        }

        /// <summary>
        /// Adds a specified column to the specified table.
        /// </summary>
        /// <param name="targetTable">The table you want to modify.</param>
        /// <param name="newColumnName">The new column you want to add to the table.</param>
        /// <param name="newDataType">The data type you want for the new column.</param>
        /// <param name="nullOrNot">Whether the new column should be null or not null.</param>
        /// <returns></returns>
        public static string AddColumnToTable(string targetTable, string newColumnName, string newDataType, string nullOrNot)
        {
            string workingString;

            workingString = SqlTemplate.AddColumnToTable;
            workingString = workingString.Replace("[TABLE]", "[" + targetTable + "]");
            workingString = workingString.Replace("NEWCOLUMN", newColumnName);
            workingString = workingString.Replace("DATATYPE", newDataType);
            workingString = workingString.Replace("NULLORNOT", nullOrNot);

            return workingString;
        }
    }
}
