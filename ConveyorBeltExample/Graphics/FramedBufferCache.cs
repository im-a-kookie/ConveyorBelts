using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphics
{
    /// <summary>
    /// A cache of vertex buffers used to apply animations
    /// </summary>
    internal class FramedBufferCache
    {

        public int Frames = 4;

        public VertexBufferWrapper[] Vertices;

        public FramedBufferCache(int frames)
        {
            this.Frames = frames;
            Vertices = new VertexBufferWrapper[Frames];
        }

    }
}
