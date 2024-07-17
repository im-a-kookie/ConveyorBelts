using ConveyorBeltExample.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphing
{
    public class Promise
    {
        public Branch owner;
        public Branch target;
        public Item item;
        /// <summary>
        /// The time that this promise is made
        /// </summary>
        public long time;
        /// <summary>
        /// The number of ticks that are resolved on the owner by this promise
        /// <para>Used to update owner.LastResolvedOutputTick</para>
        /// </summary>
        public long resolved_tick;
        public int ext = 0;
        public int slot = 0;


        /// <summary>
        /// Consumes this promise from the branch that owns it
        /// </summary>
        public void Consume()
        {

        }



        /// <summary>
        /// Finds the insertion index of the given promise into a list
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static int GetInsertionIndex(IReadOnlyList<Promise> collection, Promise p)
        {
            //we find the largest index with a time that is less than our time
            if (collection.Count == 0) return 0;
            int a = 0;
            int b = collection.Count() - 1;
            while(a <= b)
            {
                int mid = (a + b) >> 1;
                long val = collection[mid].time;
                if (val > p.time) b = mid - 1;
                else a = mid + 1;
            }
            return a;
        }


    }
}
