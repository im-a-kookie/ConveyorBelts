using ConveyorBeltExample;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Graphics
{
    /// <summary>
    /// A cache/wrapper for vertex buffers.
    /// <para>Static buffer equivalent to <see cref="DynamicVertexBufferWrapper"/></para>
    /// </summary>
    public class VertexBufferWrapper
    {

        /// <summary>
        /// A tag to store any flags (not sure if need)
        /// </summary>
        public int tag = 0;
        /// <summary>
        /// The vertices stored in this buffer
        /// </summary>
        public VertexPositionColorTexture[] vertices = null;
        /// <summary>
        /// the triangle indexing
        /// </summary>
        public int[] indices = null;
        /// <summary>
        /// The vertex buffer buit from this caching wrapper
        /// </summary>
        public VertexBuffer _buffer;
        /// <summary>
        /// The index buffer built from this wrapper
        /// </summary>
        public IndexBuffer _indices;
        /// <summary>
        /// The number of tiles in this wrapper
        /// </summary>
        public int tileCount = 0;
        /// <summary>
        /// The dimensions of the texture for this wrapper
        /// </summary>
        public Point TextureSize = new Point(0, 0);

        /// <summary>
        /// The expected vertex count from the number of tiles
        /// </summary>
        public int vertex_count => 4 * tileCount;
        /// <summary>
        /// The projected index count from the number of tiles
        /// </summary>
        public int index_count => 6 * tileCount;

        public VertexBufferWrapper(int tw, int th)
        {
            TextureSize = new Point(tw, th);
        }

        public VertexBufferWrapper(int size)
        {
            TextureSize = new Point(size, size);
        }

        public void Dispose()
        {
            if (_buffer != null) _buffer.Dispose();
            if (_indices != null) _indices.Dispose();
        }

        /// <summary>
        /// Resets and clears the buffers
        /// </summary>
        public void Reset(bool rescale = false)
        {

            //if the arrays are really big, we may want to scale them down
            if (rescale && vertices.Length > 1024)
            {
                ArrayPool<VertexPositionColorTexture>.Shared.Return(vertices);
                vertices = ArrayPool<VertexPositionColorTexture>.Shared.Rent(128);
            }
            if (rescale && indices.Length > 1024)
            {
                ArrayPool<int>.Shared.Return(indices);
                indices = ArrayPool<int>.Shared.Rent(128);
            }

            if (_buffer != null) _buffer.Dispose();
            if (_indices != null) _indices.Dispose();

            tileCount = 0;

        }

        /// <summary>
        /// Gets the actual buffers. Regenerates them if they are dirty.
        /// </summary>
        /// <returns></returns>
        public (VertexBuffer vertices, IndexBuffer indices) ConstructBuffer()
        {
            if (tileCount == 0) return (null, null);
            if (_buffer == null)
            {
                //now fill the vertices
                _buffer = new VertexBuffer(Core.Instance.GraphicsDevice, typeof(VertexPositionColorTexture), vertex_count, BufferUsage.WriteOnly);
                _buffer.SetData(vertices, 0, vertex_count);
            }
            if (_indices == null)
            {
                //now fill the indices
                _indices = new IndexBuffer(Core.Instance.GraphicsDevice, IndexElementSize.ThirtyTwoBits, index_count, BufferUsage.WriteOnly);
                _indices.SetData(indices, 0, index_count);
            }
            return (_buffer, _indices);
        }

        /// <summary>
        /// Validates the size of the arrays and reallocates if necessary
        /// </summary>
        public void ValidateArrays()
        {
            if (indices == null) indices = ArrayPool<int>.Shared.Rent(192);
            if (index_count + 5 >= indices.Length)
            {
                int[] _new = ArrayPool<int>.Shared.Rent(indices.Length << 1);
                Array.Copy(indices, _new, indices.Length);
                ArrayPool<int>.Shared.Return(indices);
                indices = _new;
            }
            if (vertices == null) vertices = ArrayPool<VertexPositionColorTexture>.Shared.Rent(128);
            if (vertex_count + 3 >= vertices.Length)
            {
                VertexPositionColorTexture[] _new = ArrayPool<VertexPositionColorTexture>.Shared.Rent(vertices.Length << 1);
                Array.Copy(vertices, _new, vertices.Length);
                ArrayPool<VertexPositionColorTexture>.Shared.Return(vertices);
                vertices = _new;
            }
        }

        public void Draw(BasicEffect basicEffect, Texture2D texture)
        {
            if (tileCount > 0)
            {
                Core.Instance.GraphicsDevice.SetVertexBuffer(_buffer);
                Core.Instance.GraphicsDevice.Indices = _indices;

                basicEffect.Texture = texture;
                foreach (var pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Core.Instance.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, tileCount * 2);
                }
            }
        }

        /// <summary>
        /// Adds a tile at the given coordinates
        /// </summary>
        /// <param name="tx">World x</param>
        /// <param name="ty">World y</param>
        /// <param name="rect">The bounds for the tile</param>
        /// <param name="tint">Tint color</param>
        /// <param name="flippedHorizontally">Whether to mirror horizontally</param>
        /// <param name="flippedVertically">Whether to mirror vertically</param>
        public void AddTile(float tx, float ty, Rectangle rect, Color tint, bool flippedHorizontally, bool flippedVertically)
        {
            ValidateArrays();

            //GPU handles UV from 0.0-1.0 rather than 0-dimension
            float textureSizeX = 1f / TextureSize.X;
            float textureSizeY = 1f / TextureSize.Y;

            //Now calculate the UV for each triangle
            float left = rect.Left * textureSizeX;
            float right = rect.Right * textureSizeX;
            float bottom = rect.Bottom * textureSizeY;
            float top = rect.Top * textureSizeY;

            //handle flipping
            if (flippedHorizontally)
            {
                float temp = left;
                left = right;
                right = temp;
            }
            if (flippedVertically)
            {
                float temp = top;
                top = bottom;
                bottom = temp;
            }

            //The tile is made of two triangles which bisect the quad,
            //so we only need the outer corners of the quad
            int vertexCount = tileCount * 4;
            vertices[vertexCount] = new VertexPositionColorTexture(new Vector3(tx, ty, 0), tint, new Vector2(left, top));
            vertices[vertexCount + 1] = new VertexPositionColorTexture(new Vector3(tx + rect.Width, ty, 0), tint, new Vector2(right, top));
            vertices[vertexCount + 2] = new VertexPositionColorTexture(new Vector3(tx, ty + rect.Height, 0), tint, new Vector2(left, bottom));
            vertices[vertexCount + 3] = new VertexPositionColorTexture(new Vector3(tx + rect.Width, ty + rect.Height, 0), tint, new Vector2(right, bottom));

            //and now define the triangles
            int indexCount = tileCount * 6;
            indices[indexCount] = vertexCount;
            indices[indexCount + 1] = (vertexCount + 1);
            indices[indexCount + 2] = (vertexCount + 2);

            indices[indexCount + 3] = (vertexCount + 3);
            indices[indexCount + 4] = (vertexCount + 2);
            indices[indexCount + 5] = (vertexCount + 1);

            tileCount++; //bonk
        }



    }

}
