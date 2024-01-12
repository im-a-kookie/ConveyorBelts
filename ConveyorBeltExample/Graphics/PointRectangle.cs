using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorBeltExample.Graphics
{
    public class PointRectangle
    {
        public bool flag = false;
        public Rectangle R { get; set; }
        public Point P { get; set; }
        
        public PointRectangle(Rectangle r, Point p)
        {
            R = r;
            P = p;
        }
        public PointRectangle(Point p , Rectangle r)
        {
            R = r;
            P = p;
        }

        public PointRectangle(int x, int y, Rectangle r)
        {
            R = r;
            P = new Point(x, y);
        }

        public PointRectangle(int x, int y, int u, int v, int w, int h)
        {
            R = new Rectangle(u, v, w, h);
            P = new Point(x, y);
        }

    }
}
