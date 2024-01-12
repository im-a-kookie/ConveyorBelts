using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphing
{
    internal class BranchUpdater
    {

        List<Branch> branches = new List<Branch>();
        List<bool> flags = new List<bool>();
        List<Branch> updates = new List<Branch>();

        public int InitialTick = 0;

        /// <summary>
        /// Flags an update into the thing
        /// </summary>
        /// <param name="b"></param>
        public void FlagUpdate(Branch b)
        {
            if (b.Index > 0 && b.Index < branches.Count && branches[b.Index] == b)
            {
                if (flags[b.Index])
                {
                    return;
                }

                lock(this)
                {
                    if (!flags[b.Index])
                    {
                        flags[b.Index] = true;
                        updates.Add(b);
                        return;
                    }

                }
            }

            lock (this)
            {
                b.Index = branches.Count;
                
                branches.Add(b);
                flags.Add(true);
                updates.Add(b);
            }
        }

    }
}
