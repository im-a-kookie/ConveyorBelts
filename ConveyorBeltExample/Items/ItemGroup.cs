using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Items
{
    /// <summary>
    /// The Item Group represents a list of items and relates them to a consolidated work value
    /// </summary>
    internal class ItemGroup
    {

        public List<Item> Items;
        public float Work;
        public float WorkPromise = 0f;
    }
}
