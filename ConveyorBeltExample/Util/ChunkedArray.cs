using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{
    /// <summary>
    /// A simple chunked array that expands dynamically
    /// (does not shrink automatically)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChunkedArray<T>
    {
        //a spinlock variable
        private volatile int visits = 0;

        readonly int ChunkScale;
        readonly int ScaleMask;
        public int Width;
        public int Height;
        public T[][][,] Items;

        /// <summary>
        /// Creates a new chunked array.
        /// </summary>
        /// <param name="max_width">Max width</param>
        /// <param name="max_height">Max Height.</param>
        /// <param name="ChunkSize">Size of sub-chunks. 2^n. e.g size=4 means 16x16 subsections</param>
        public ChunkedArray(int max_width = 128, int max_height = 128, int ChunkSize = 4)
        {
            ChunkScale = ChunkSize;
            ScaleMask = (1 << ChunkScale) - 1;
            Width = max_width;
            Height = max_height;

            //initial width height construction populates the internal array
            //so e.g a 512x512 array with chunk size of 4
            //will instead wrap down to 32x32 or a 256x reduction
            if (Width > 0 && Height > 0)
            {
                Items = new T[Width >> ChunkSize][][,];
                for (int i = 0; i < (Width >>> ChunkSize); i++)
                {
                    Items[i] = new T[Height >> ChunkSize][,];
                }
            }
        }

       public void SetLock(int x, int y, T val)
        {
            int cx = x & ScaleMask;
            int cy = y & ScaleMask;
            int _x = x >> ChunkScale;
            int _y = y >> ChunkScale;
            while (Items[_x][_y] == null)
            {
                lock(this)
                {
                    if (Items[_x][_y] == null)
                        Items[_x][_y] = new T[1 << ChunkScale, 1 << ChunkScale];
                }
            }
            Items[_x][_y][cx, cy] = val;
        }

        public void SetInterlock(int x, int y, T val)
        {
            int cx = x & ScaleMask;
            int cy = y & ScaleMask;
            int _x = x >> ChunkScale;
            int _y = y >> ChunkScale;
            while (Items[_x][_y] == null)
            {
                if (Interlocked.Increment(ref visits) != 1)
                {
                    if (Items[_x][_y] == null)
                        Items[_x][_y] = new T[1 << ChunkScale, 1 << ChunkScale];
                }
                Interlocked.Decrement(ref visits);
            }
            Items[_x][_y][cx, cy] = val;
        }


        public T this[int x, int y]
        {
            get
            {
                int cx = x & ScaleMask;
                int cy = y & ScaleMask;
                int _x = x >> ChunkScale;
                int _y = y >> ChunkScale;
                if (Items[_x][_y] == null) return default;
                return Items[_x][_y][cx, cy];
            }
            set
            {
                
                int cx = x & ScaleMask;
                int cy = y & ScaleMask;
                int _x = x >> ChunkScale;
                int _y = y >> ChunkScale;
                while (Items[_x][_y] == null)
                {
                    lock (this)
                    {
                        if (Items[_x][_y] == null)
                            Items[_x][_y] = new T[1 << ChunkScale, 1 << ChunkScale];
                    }
                }
                Items[_x][_y][cx, cy] = value;
            }
        }

    }
}
