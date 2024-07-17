using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.GameWorld
{
    public class WorldDataLayer
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

        public WorldDataLayer(string WorldName) 
        {
            this.WorldName = WorldName;
        }








    }
}
