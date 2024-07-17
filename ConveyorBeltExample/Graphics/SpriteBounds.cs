using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphics
{
    public struct SpriteBounds
    {

        public int X, Y, Width, Height;

        public SpriteBounds(int X, int Y, int W, int H)
        {
            this.X = X; 
            this.Y = Y;
            this.Width = W;
            this.Height = H;
        }

        public SpriteBounds this[int val] => new SpriteBounds(X + val * Height, Y, Height, Height);

        public SpriteBounds offsetW(int x = 0)
        {
            return new SpriteBounds(X + x, Y, Height, Height);
        }

        public SpriteBounds offsetH(int y = 0)
        {
            return new SpriteBounds(X, Y + y, Width, Width);
        }

        public Rectangle R => new Rectangle(X, Y, Width, Height);
    }
}
