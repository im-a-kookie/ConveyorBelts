using ConveyorBeltExample.Graphics;
using ConveyorEngine.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Blocks
{
    internal class Tree : Plant
    {

        public SpriteBounds Sprite = SpriteManager.SpriteMappings["tree_pine_0"];

        public Tree()
        {
            Bounds = new Rectangle(0, 0, 1, 1);
        }




    }
}
