using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQL_script_creator_2.Helpers
{
    public static class SplitListUtility
    {
        public static List<List<T>> SplitList<T>(List<T> collection, int size)
        {
            var tempList = new List<List<T>>();
            var count = 0;
            var temp = new List<T>();

            foreach (var element in collection)
            {
                if (count++ == size)
                {
                    tempList.Add(temp);
                    temp = new List<T>();
                    count = 1;
                }
                temp.Add(element);
            }

            tempList.Add(temp);


            return tempList;
        }
    }
}
