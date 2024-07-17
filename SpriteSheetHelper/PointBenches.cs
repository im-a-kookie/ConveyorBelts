using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpriteSheetHelper
{
    public class PointBenches
    {

        public static void Perform()
        {


        }

        /// <summary>
        /// Binomial search. Ascending. Finds largest index >= value
        /// </summary>
        /// <param name="l"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int FindIndex(List<int> l, int val)
        {
            //we find the largest index with a progress that is less than the parameter
            if (l.Count == 0) return 0;
            int a = 0;
            int b = l.Count() - 1;
            while (a <= b)
            {
                int mid = (a + b) >> 1;
                int here = l[mid];
                if (here < val) b = mid - 1;
                else a = mid + 1;
            }
            return a;
        }

    }




}
