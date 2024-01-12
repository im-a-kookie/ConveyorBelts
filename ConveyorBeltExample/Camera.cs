using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;
using ConveyorBeltExample.Graphics;

namespace ConveyorBeltExample
{
    public class Camera
    {
        public float MinZoom = 1f;
        public float MaxZoom = 16f;

      


        public Rectangle Bounds { get; protected set; }
        public Rectangle VisibleArea { get; protected set; }
        public Matrix Transform { get; protected set; }

        private float _wheelPos, _wheelDelta;

        /// <summary>
        /// The target for the camera to look at
        /// </summary>
        Vector2 CameraTarget = Vector2.Zero;

        Vector2 CameraPosition = Vector2.Zero;

        float ZoomTarget = 4f;
        public float ZoomCurrent = 0.5f;

        /// <summary>
        /// A clamped camera position. To be used with ZoomClamped.
        /// Camera position scaled to a single screen pixel.
        /// 1. We find the camera clamping position at an integer X. and ZoomClamped is adjusted so that we have an integral value at the end
        /// </summary>
        Vector2 CameraClamped => new Vector2((int)CameraPosition.X, (int)CameraPosition.Y);

        /// <summary>
        /// Create a camera on the given viewport
        /// </summary>
        /// <param name="viewport"></param>
        public Camera(Viewport viewport)
        {
            Bounds = viewport.Bounds;
            ZoomCurrent = ZoomTarget = (MinZoom + MaxZoom) / 2f;
            CameraTarget = CameraPosition = Vector2.Zero;
        }

        /// <summary>
        /// Updates the visible area of the camera internally
        /// </summary>
        private void UpdateVisibleArea()
        {
            var inverseViewMatrix = Matrix.Invert(Transform);

            var tl = Vector2.Transform(Vector2.Zero, inverseViewMatrix);
            var tr = Vector2.Transform(new Vector2(Bounds.X, 0), inverseViewMatrix);
            var bl = Vector2.Transform(new Vector2(0, Bounds.Y), inverseViewMatrix);
            var br = Vector2.Transform(new Vector2(Bounds.Width, Bounds.Height), inverseViewMatrix);

            var min = new Vector2(
                MathHelper.Min(tl.X, MathHelper.Min(tr.X, MathHelper.Min(bl.X, br.X))),
                MathHelper.Min(tl.Y, MathHelper.Min(tr.Y, MathHelper.Min(bl.Y, br.Y))));

            var max = new Vector2(
                MathHelper.Max(tl.X, MathHelper.Max(tr.X, MathHelper.Max(bl.X, br.X))),
                MathHelper.Max(tl.Y, MathHelper.Max(tr.Y, MathHelper.Max(bl.Y, br.Y))));

            VisibleArea = new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        }

        public Matrix CreateScale(float x, float y)
        {
            return new Matrix(x, 0, 0, 0, 0, y, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        }

        //Internally updates the translation matrix
        private void UpdateTransformMatrix()
        {
            Transform = 
                Matrix.CreateTranslation(
                    CameraPosition.X, CameraPosition.Y, 0)
                * CreateScale(ZoomCurrent, ZoomCurrent) *
                    Matrix.CreateTranslation(new Vector3(Bounds.Width * 0.5f, Bounds.Height * 0.5f, 0));

            UpdateVisibleArea();
        }


        public void StartBatch(SpriteBatch sb)
        {
            sb.Begin
                (transformMatrix: Transform, 
                samplerState: ZoomCurrent > 1.5 ? SamplerState.PointClamp : SamplerState.AnisotropicClamp, 
                sortMode: SpriteSortMode.Deferred);
        }



        public void UpdateCamera(Viewport bounds, GameTime time = null)
        {
            Bounds = bounds.Bounds;
            float fN = (float)time.ElapsedGameTime.TotalSeconds;

            float targetPanVelocity = 1000f * fN / float.Sqrt(ZoomCurrent);

            if (Keyboard.GetState().IsKeyDown(Keys.W))
                CameraTarget.Y += targetPanVelocity;
            
            if (Keyboard.GetState().IsKeyDown(Keys.S))
                CameraTarget.Y -= targetPanVelocity;
            
            if (Keyboard.GetState().IsKeyDown(Keys.A))
                CameraTarget.X += targetPanVelocity;
            
            if (Keyboard.GetState().IsKeyDown(Keys.D))
                CameraTarget.X -= targetPanVelocity;
            
            _wheelDelta = _wheelPos;
            
            _wheelPos = Mouse.GetState().ScrollWheelValue;
            var d = _wheelPos - _wheelDelta;

            
            if (d < 0)
            {
                if (ZoomTarget <= 1) ZoomTarget *= 0.9f;
                else
                {
                    ZoomTarget = Math.Max(1, ZoomTarget - 1);
                }
            }
            else if (d > 0)
            {
                if (ZoomTarget < 1) ZoomTarget = Math.Min(1, ZoomTarget * (1f / 0.9f));
                else ZoomTarget += 1;
            }

            ZoomTarget = float.Max(float.Min(ZoomTarget, MaxZoom), MinZoom);

            CameraPosition = Vector2.Multiply(CameraPosition + CameraTarget, 0.05f);
            ZoomCurrent = ZoomCurrent + (ZoomTarget - ZoomCurrent) * 5f * fN;

            if (Math.Min(ZoomCurrent, ZoomTarget) / Math.Max(ZoomCurrent, ZoomTarget) > 0.995) ZoomCurrent = ZoomTarget;

            UpdateTransformMatrix();

        }
    }
}
