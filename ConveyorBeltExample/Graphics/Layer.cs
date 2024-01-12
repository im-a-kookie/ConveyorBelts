using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphics
{
    internal class Layer
    {

        public bool Valid = false;
        public Rectangle UV = Rectangle.Empty;

        public int X => UV.X;
        public int Y => UV.Y;
        public int Width => UV.Width;
        public int Height => UV.Height;


        public bool Mirrored = false;

        //Sprites are complicated things
        //At a basic level, we simply extract the bounds rectangle and draw it at the sprite's location
        //But at a higher level we can modify the rectangle first
        //For example,
        //id //the sprite bounds
        //id(a,b,c,d) //new Rectangle(id.X + a, id.Y + b, c, d)
        //For example, we might take a conveyor
        //bounds=(0,0,32,16)
        //A default frame is at (N,0,16,16)
        //So N-1 moves the conveyor once to the East
        //and N+1 moves the conveyor once to the West

        public Layer(string definition)
        {
            string s = definition;
            if (s.Contains(".flip"))
            {
                s = s.Replace(".flip", "");
                Mirrored = true;
            }

            int n = s.IndexOf('(');

            if (n > 0) s = s.Remove(n);
            
            if (!SpriteManager.SpriteMappings.ContainsKey(s))
            {
                Valid = false;
                return;
            }

            UV = SpriteManager.SpriteMappings[s];
            if (n > 0)
            { 
                string pars = definition.Substring(n + 1, definition.Length - n);
                pars = pars.Replace(")", "");
                var p = pars.Split(',').Where(x => x != null).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                if (p.Count() == 4)
                {
                    int[] vals = new int[4];
                    for(int i = 0; i < 4; i++) int.TryParse(p[i], out vals[i]);
                    UV = new Rectangle(UV.X + vals[0], UV.Y + vals[1], vals[2], vals[3]);
                }
            }
        }

        public Layer(string id, Rectangle region)
        {
            if (!SpriteManager.SpriteMappings.ContainsKey(id))
            {
                Valid = false;
                return;
            }
            UV = SpriteManager.SpriteMappings[id];
            UV = new Rectangle(UV.X + region.X, UV.Y + region.Y, region.Width, region.Height);
        }

        public Layer(string id = "", int X = 0, int Y = 0, int W = 16, int H = 16)
        {
            if (!SpriteManager.SpriteMappings.ContainsKey(id))
            {
                Valid = false;
                return;
            }
            UV = SpriteManager.SpriteMappings[id];
            UV = new Rectangle(UV.X + X, UV.Y + Y, W, H);
        }

    }
}
