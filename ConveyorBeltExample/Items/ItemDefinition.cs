using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Items
{
    public class ItemDefinition
    {
        public string Name;
        public short StackLimit = 100;
        public short ConveyorStackLimit = 1;
        public string Texture = null;
        public int ID = -1;
        public string sId;
        public ItemDefinition(string name, short stackLimit = 100, short conveyorLimit = 1, string tex = null)
        {
            Name = name;
            StackLimit = stackLimit;
            ConveyorStackLimit = conveyorLimit;
            Texture = tex;
        }

    }
}
