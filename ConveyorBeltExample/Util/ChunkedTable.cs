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
    public class ChunkedTable<T>
    {
        readonly int ChunkScale;
        readonly int ScaleMask;
        public int Width;
        public int Height;
        public T[][][,] Items;

        /// <summary>
        /// Creates a new chunked array.
        /// </summary>
        /// <param name="max_width">Max width. -1 for no limit.</param>
        /// <param name="max_height">Max Height. -1 for no limit.</param>
        /// <param name="ChunkSize">Size of sub-chunks. 2^n. e.g size=4 means 16x16 subsections</param>
        public ChunkedTable( int ChunkSize = 4)
        {
            ChunkScale = ChunkSize;
            ScaleMask = (1 << ChunkScale) - 1;
        }

        /// <summary>
        /// Internally, ensures that the array is big enough. Can be slow for creation under contention,
        /// but access is fast and cheap.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void Check(int x, int y)
        {
            //Get the index of the chunk and the internal coords
            int cx = x & ScaleMask;
            int cy = y & ScaleMask;
            int _x = x >> ChunkScale;
            int _y = y >> ChunkScale;

            //make sure the x and y chunks exist
            if (Items == null || _x >= Items.Length)
            {
                lock (this)
                {
                    T[][][,] _items = new T[_x + 1][][,];
                    if (Items != null) Array.Copy(Items, _items, Items.Length);
                    Items = _items;
                }
            }
            if (Items[_x] == null || _y >= Items[_x].Length)
            {
                lock (this)
                {
                    T[][,] _i = new T[_y + 1][,];
                    if (Items[_x] != null) Array.Copy(Items[_x], _i, Items[_x].Length);
                    Items[_x] = _i;
                }
            }
            //Create the internal chunk
            if (Items[_x][_y] == null) lock (this) Items[_x][_y] = new T[1 << ChunkScale, 1 << ChunkScale];
        }

        public T this[int x, int y]
        {
            get
            {
                int cx = x & ScaleMask;
                int cy = y & ScaleMask;
                int _x = x >> ChunkScale;
                int _y = y >> ChunkScale;
                Check(x, y);
                return Items[_x][_y][cx, cy];
            }
            set
            {
                int cx = x & ScaleMask;
                int cy = y & ScaleMask;
                int _x = x >> ChunkScale;
                int _y = y >> ChunkScale;
                Check(x, y);
                Items[_x][_y][cx, cy] = value;
            }
        }

    }
}
