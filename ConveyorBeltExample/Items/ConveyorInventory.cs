using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Items
{
    internal class ConveyorInventory : Inventory
    {
        /// <summary>
        /// A collection of the items on this conveyor
        /// </summary>
        public List<Item[]> Items = new List<Item[]>();
        public List<int> progresses = new List<int>();

    }
}
