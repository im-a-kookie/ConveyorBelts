using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.GameWorld;
using ConveyorEngine.Graphics;
using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks
{
    internal class Plant : BlockDefinition
    {
        

        public Plant()
        {
        }

        public virtual void Grow(World w, Chunk c, KPoint pos, BlockData data)
        {
            //the data stores an int and we typically increment it here

        }


        public override int GetLayer()
        {
            return LayerManager.PlantLayer;
        }


    }
}
