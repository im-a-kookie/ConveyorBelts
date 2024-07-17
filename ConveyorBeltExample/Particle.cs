using ConveyorBeltExample.Graphics;
using ConveyorEngine.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine
{
    public class Particle
    {
        public int sprite;
        public Vector2 pos;
        public Vector2 velocity;
        public float life = 1f;
        public float _start_life = 0f;
        public Particle(int sprite, Vector2 pos, Vector2 velocity, float life = 1f)
        {
            this.sprite = sprite;
            this.pos = pos;
            this.velocity = velocity;
            this.life = _start_life = life;
        }
        public Particle(string sprite, Vector2 pos, Vector2 velocity, float life = 1f)
        {
            this.sprite = SpriteManager.SpriteIndexMapping[sprite];
            this.pos = pos;
            this.velocity = velocity;
            this.life = _start_life = life;
        }

    }
}
