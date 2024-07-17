using ConveyorBeltExample.Graphics;
using ConveyorBeltExample;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConveyorEngine.Graphics.Conveyors.ConveyorHelper;
using Microsoft.Xna.Framework;
using ConveyorBeltExample.GameWorld;
using ConveyorBeltExample.Graphing;
using System.Threading;
using ConveyorBeltExample.Items;
using ConveyorEngine.Util;
using ConveyorEngine.Items;

namespace ConveyorEngine.Graphics.Conveyors
{
    /// <summary>
    /// A parent class that contains the render buffers for the conveyor tiles.
    /// <para
    /// </summary
    [Finalizable(Stage = EngineStage.Game)]
    public class ConveyorSpriteManager
    {
        /// <summary>
        /// The size of the conveyor buffer texture (it's square)
        /// </summary>
        public static int Size;

        /// <summary>
        /// Whether or not this object hass been finalized
        /// </summary>
        private static bool IsFinalized = false;

        private static List<ConveyorTextureComponent> textureComponents = new List<ConveyorTextureComponent>();

        static Dictionary<int, Point> maps = new Dictionary<int, Point>();

        /// <summary>
        /// The collection of render targets for each frame of animation
        /// </summary>
        public static RenderTarget2D[] Frames = new RenderTarget2D[4];

        /// <summary>
        /// Gets the buffer for the given frame of the given sprite
        /// </summary>
        /// <param name="conveyor"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static (RenderTarget2D frame, Point offset) GetFrameBuffer(string conveyor, int frame)
        {
            return GetFrameBuffer(SpriteManager.SpriteIndexMapping[conveyor], frame);
        }

        public static Rectangle GetUV(int id, int dir, int mask)
        {
            int posX = (dir | (mask << 2)) & 7;
            int posY = (dir | (mask << 2)) / 8;
            var p = maps[id];
            return new Rectangle(p.X + (posX << 3), p.Y + (posY << 3), 8, 8);
        }

        public static (RenderTarget2D frame, Point offset) GetFrameBuffer(int id, int frame)
        {
            return (Frames[frame], maps[id]);
        }

        /// <summary>
        /// Adds sprites to the buffer builder, via their sprite identifier.
        /// </summary>
        /// <param name="conveyorSprite"></param>
        /// <exception cref="Exception"></exception>
        public static void AddMember(string conveyorSprite)
        {
            if (IsFinalized) throw new Exception("Cannot register conveyors after buffer initialization!");

            //generate the buffer and add it
            ConveyorTextureComponent piece = new ConveyorTextureComponent();
            piece.sprite = SpriteManager.SpriteIndexMapping[conveyorSprite];
            Rectangle tex = SpriteManager.SpritesByIndex[piece.sprite].R;
            piece.GenerateBuffer(Core.Instance._spriteBatch, tex, ConveyorHelper.ConveyorBounds8);
            textureComponents.Add(piece);
        }

        /// <summary>
        /// By design, textures should be loaded at the start. To prevent weird things happening later,
        /// we load the textures and lock the buffer.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void FinalizeObject()
        {
            if (IsFinalized)
                if (IsFinalized) throw new Exception("Conveyor Buffer is already constructed!");

            //1. Assign a point to each thing
            //each conveyor will fit in a 4x4 of sprites so...

            //1. map each convetor sprite tothe top corner initially
            //Otherwise skip it and remove it
            for (int i = 0; i < textureComponents.Count; i++)
            {
                if (!maps.ContainsKey(textureComponents[i].sprite)) maps.Add(textureComponents[i].sprite, Point.Zero);
                else textureComponents.RemoveAt(i--);
            }
            //now calculate the size
            Size = 8;
            //There's a loopless logarithm method but this is simpler and it doesn't matter
            //this literally just grows the texture size until it fits all the frame parts
            while (Size * Size < maps.Count * 8 * 8) Size <<= 1;
            for (int i = 0; i < textureComponents.Count; i++)
            {
                //find where it fits in the larger map.
                Point pos = new Point(64 * (i % (Size / 8)), 64 * (i / (Size / 8)));
                maps[textureComponents[i].sprite] = pos;
            }
            Size *= 8;

            //now render each thing to its pos
            for (int i = 0; i < 4; i++)
            {
                Frames[i] = new RenderTarget2D(Core.Instance.GraphicsDevice, Size, Size, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                Core.Instance.GraphicsDevice.SetRenderTarget(Frames[i]);
                Core.Instance.GraphicsDevice.Clear(Color.White);
                Core.Instance._spriteBatch.Begin();
                for (int b = 0; b < textureComponents.Count; b++)
                {
                    //draw the buffer from the thing to the place
                    Core.Instance._spriteBatch.Draw(textureComponents[b].buffers[i], new Rectangle(maps[textureComponents[b].sprite], new Point(64, 64)), Color.White);
                }
                Core.Instance._spriteBatch.End();
            }
            //clean up
            for (int i = 0; i < 4; i++)
            {
                for (int b = 0; b < textureComponents.Count; b++)
                {
                    textureComponents[b].buffers[i].Dispose();
                }
                textureComponents.Clear();
            }

            //and we're done.
            IsFinalized = true;
        }
    }
}
