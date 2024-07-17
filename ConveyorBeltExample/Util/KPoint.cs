using ConveyorEngine.Tests;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{

    /// <summary>
    /// A simple point class that does stuff because yes
    /// </summary>
    public struct KPoint
    {
        //public ulong val;
        /// <summary>
        /// The x component of this point
        /// </summary>
        //public int x => (int)(val & 0xFFFFFFFF);
        /// <summary>
        /// The y component of this point
        /// </summary>
        //public int y => (int)(val >> 32);

        public int x, y;

        public KPoint(int _x, int _y)
        {
            //val = (((ulong)_x & 0xFFFFFFFF) | (((ulong)_y) << 32));
            this.x = _x;
            this.y = _y;
        }
        public KPoint(KPoint p)
        {
            x = p.x;
            y = p.y;

        }

        public KPoint(Point p) : this(p.X, p.Y)
        {
        }

        public KPoint(KPoint p, int dir)
        {
            x = (p.x + ((2 - (dir &= 0x3)) * (dir & 0x1)));
            y = (p.y + (dir - 1) * (~dir & 0x1));

            //We need to optimize direction stepping, because we will do it a lot
            //val = (
            //    ((ulong)(p.x + ((2 - (dir &= 0x3)) * (dir & 0x1)) & 0xFFFFFFFF))
            //    | (((ulong)(p.y + (dir - 1) * (~dir & 0x1))) << 32)
            //);
        }

        //public KPoint(ulong l)
        //{
        //    val = l;
        //}

        /// <summary>
        /// Steps this point in the given direction
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public KPoint Step(int dir) => new(this, dir);

        /// <summary>
        /// Gets the direction indicated by this point, from 0, 0.
        /// Clamps
        /// </summary>
        /// <returns></returns>
        public Dir Direction()
        {
            return GetDirection(0, 0, x, y);
        }

        /// <summary>
        /// Gets the direction indicated by this point, from 0, 0.
        /// Clamps
        /// </summary>
        /// <returns></returns>
        public Dir Direction(KPoint p2)
        {
            return GetDirection(p2.x, p2.y, x, y);
        }


        public static Dir GetDirection(int x1, int y1, int x2, int y2)
        {
            double angle = Math.Atan2(y2 - y1, x2 - x1);
            angle = (angle + Math.PI / 2) % (2 * Math.PI);
            angle /= (Math.PI / 2);
            return (Dir)((int)(Math.Round(angle)) & 0x3);

        }

        /// <summary>
        /// The points adjacent to this point
        /// </summary>
        public KPoint[] Adjacent => [Step(0), Step(1), Step(2), Step(3)];
        /// <summary>
        /// The point in the given direction
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public KPoint this[int dir] => Step(dir);
        /// <summary>
        /// Gets an Xna point from this KPoint
        /// </summary>
        /// <returns></returns>
        public Point ToPoint() => new Point(x, y);
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is KPoint p && (p.x == x && p.y == y));
        public override int GetHashCode() => x ^ y;
        public override string ToString() => "[" + x + "," + y + "]";

        public static bool operator ==(KPoint a, KPoint b) => (a.x == b.x && a.y == b.y);
        public static bool operator !=(KPoint a, KPoint b) => (a.x != b.x || a.y != b.y);
        public static KPoint operator +(KPoint x, KPoint y) => new(x.x + y.x, x.y + y.y);
        public static KPoint operator -(KPoint x, KPoint y) => new(x.x - y.x, x.y - y.y);
        //public static KPoint operator *(KPoint x, int s) => new(x.val * (ulong)s);
        //public static KPoint operator *(KPoint x, float s) => new(x.val * (ulong)s);
        //public static KPoint operator *(KPoint x, double s) => new(x.val * (ulong)s);
        //public static KPoint operator *(KPoint x, short s) => new(x.val * (ulong)s);
        //public static KPoint operator *(KPoint x, ulong s) => new(x.val * s);
        public static KPoint operator +(KPoint x, KIntPoint t) => new(x.x + t.x, x.y + t.y);
        public static KPoint operator -(KPoint x, KIntPoint t) => new(x.x - t.x, x.y - t.y);

        public static Rectangle operator |(KPoint x, KPoint y) => new Rectangle(int.Min(x.x, y.x), int.Min(x.y, y.y), int.Abs(x.x - y.x), int.Abs(x.y - y.y));
    }

    /// <summary>
    /// A simple point class that does stuff because yes
    /// </summary>
    public struct KIntPoint
    {
        public uint val;
        /// <summary>
        /// The x component of this point
        /// </summary>
        public int x => (int)(val & 0xFFFFFFFF);
        /// <summary>
        /// The y component of this point
        /// </summary>
        public int y => (int)(val >> 32);

        public KIntPoint(short _x, short _y)
        {
            val = (((uint)_x & 0xFFFF) | (((uint)_y) << 16));
        }

        public KIntPoint(int _x, int _y)
        {
            val = (((uint)_x & 0xFFFF) | (((uint)_y) << 16));
        }

        public KIntPoint(KIntPoint p)
        {
            this.val = p.val;
        }
        public KIntPoint(KIntPoint p, int dir)
        {
            //We need to optimize direction stepping, because we will do it a lot
            val = (
                ((uint)(p.x + ((2 - (dir &= 0x3)) * (dir & 0x1)) & 0xFFFF))
                | (((uint)(p.y + (dir - 1) * (~dir & 0x1))) << 16)
            );
        }

        public KIntPoint(uint l)
        {
            val = l;
        }

        /// <summary>
        /// Steps this point in the given direction
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public KIntPoint Step(int dir) => new(this, dir);
        /// <summary>
        /// The points adjacent to this point
        /// </summary>
        public KIntPoint[] Adjacent => [Step(0), Step(1), Step(2), Step(3)];
        /// <summary>
        /// The point in the given direction
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public KIntPoint this[int dir] => Step(dir);
        /// <summary>
        /// Gets an Xna point from this KPoint
        /// </summary>
        /// <returns></returns>
        public Point ToPoint() => new Point(x, y);
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is KPoint p && p.x == x && p.y == y);
        public override int GetHashCode() => (int)(val & 0xFFFF ^ (val >> 48));
        public override string ToString() => "[" + x + "," + y + "]";
        public static bool operator ==(KIntPoint x, KIntPoint y) => x.val == y.val;
        public static bool operator !=(KIntPoint x, KIntPoint y) => x.val != y.val;
        public static KIntPoint operator +(KIntPoint x, KIntPoint y) => new(x.x + y.x, x.y + y.y);
        public static KIntPoint operator -(KIntPoint x, KIntPoint y) => new(x.x - y.x, x.y - y.y);
        public static KIntPoint operator *(KIntPoint x, int s) => new(x.val * (uint)s);
        public static KIntPoint operator *(KIntPoint x, float s) => new(x.val * (uint)s);
        public static KIntPoint operator *(KIntPoint x, double s) => new(x.val * (uint)s);
        public static KIntPoint operator *(KIntPoint x, short s) => new(x.val * (uint)s);
        public static KIntPoint operator *(KIntPoint x, ulong s) => new(x.val * (uint)s);
        public static KIntPoint operator *(KIntPoint x, uint s) => new(x.val * s);
        public static Rectangle operator |(KIntPoint x, KIntPoint y) => new Rectangle(int.Min(x.x, y.x), int.Min(x.y, y.y), int.Abs(x.x - y.x), int.Abs(x.y - y.y));

    }


    /// <summary>
    /// A simple rectangle, probably not need ever
    /// </summary>
    public struct KRect
    {
        public KPoint pos;
        public KPoint size;
        public KRect(int x, int y, int w, int h)
        {
            pos = new(x, y);
            size = new(w, h);
        }
        public KRect(KPoint pos, KPoint size)
        {
            this.pos = pos;
            this.size = size;
        }

        public int x => pos.x;
        public int y => pos.y;
        public int w => size.x;
        public int h => size.y;
    }
    
}
