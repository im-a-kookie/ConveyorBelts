using ConveyorEngine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks
{
    internal class BlockDecorator : BlockDefinition
    {

        public override int GetLayer()
        {
            return LayerManager.PlantLayer;
        }


    }
}
