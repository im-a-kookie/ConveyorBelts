using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.GameWorld
{
    internal class WorldManager
    {

        public string WorldName = "";

        public string WorldPath
        {
            get
            {
                string s = "\\Worlds\\" + WorldName;
                Directory.CreateDirectory(s);
                return s;
            }
        }

        public WorldManager(string WorldName) 
        {
            this.WorldName = WorldName;
        }








    }
}
