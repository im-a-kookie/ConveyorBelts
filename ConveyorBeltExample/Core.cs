using ConveyorBeltExample.Blocks;
using ConveyorBeltExample.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace ConveyorBeltExample
{
    public class Core : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Camera myCam;

        public Core()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;

            TargetElapsedTime = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond / 240L));
            _graphics.SynchronizeWithVerticalRetrace = true;

            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteManager.LoadSpritesAndGeneratePage(GraphicsDevice, "Content");
            myCam = new Camera(GraphicsDevice.Viewport);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            myCam.UpdateCamera(GraphicsDevice.Viewport, gameTime);
            base.Update(gameTime);
        }


        private float Frame = 0f;
        private float rtime = 0f;
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Frame += (float)gameTime.ElapsedGameTime.TotalSeconds * 16f;

            rtime = (50 * rtime + (float)gameTime.ElapsedGameTime.TotalSeconds) / 51f;

            if (Frame > 16)
            {
                Debug.WriteLine(Math.Round(1f / rtime));
                Frame %= 16;
            }

            myCam.StartBatch(_spriteBatch);

            _spriteBatch.Draw(SpriteManager.TexturePage, new Rectangle(0, 0, SpriteManager.TexturePage.Width, SpriteManager.TexturePage.Height), Color.White);

            //simple render a sprite thing
            var r = SpriteManager.SpriteMappings["ore_iron"];
            _spriteBatch.Draw(SpriteManager.TexturePage, new Rectangle(SpriteManager.TexturePage.Width, SpriteManager.TexturePage.Width, 16, 16), r, Color.White);


            var c0 = SpriteManager.SpriteMappings["conveyor"];
            var c1 = SpriteManager.SpriteMappings["conveyor_rotated"];

            int[,] things = new int[16, 16];
            for(int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    things[i, j] = -1;
                }
            }
            things[5, 4] = 0;
            things[4, 4] = 1;
            things[6, 4] = 3;
            things[5, 5] = 0;
            things[5, 3] = 0;
            things[6, 3] = 2;

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    Point p = new Point(i, j);
                    int n = things[p.X, p.Y];
                    if (n < 0) continue;

                    Dir dir = (Dir)n;
                    int mask = 0;
                    foreach(var pp in p.GetAdjacent())
                    {
                        int nn = things[pp.X, pp.Y];
                        if (nn >= 0 && pp.Step((Dir)nn) == p)
                        {
                            mask |= (1 << ((nn + 2) & 0x3));
                        }
                    }

                    var conveyorParts = BlockConveyor.GetRects(dir, mask, ((int)Frame));

                    foreach(var c in conveyorParts)
                    {
                        var _r = (c.flag ? c0 : c1);

                        _spriteBatch.Draw(SpriteManager.TexturePage,
                            new Rectangle(80 + c.P.X + 16 * i, 80 + c.P.Y + 16 * j, c.R.Width, c.R.Height),
                            new Rectangle(_r.X + c.R.X, _r.Y + c.R.Y, c.R.Width, c.R.Height),
                            Color.White
                         );

                    }


                }
            }


            _spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
