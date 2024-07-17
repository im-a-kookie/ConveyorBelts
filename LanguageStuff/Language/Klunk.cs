using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Forms.VisualStyles;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LanguageStuff.Language
{
    public struct Klunk
    {
        public static string[] Prefixes = new string[]
        {
            "P",  "B",  "K",  "G",
            "Pl", "Bl", "Kl", "Gl",
            "Pr", "Br", "Kr", "Gr",
            "Spl","Spr","Fl", "D",
        };

        public static string[] Bodies = new string[]
        {
            "ar", "or", "er", "ur",
            "au", "oh", "e",  "u",
            "a",  "o",  "i",  "uu",
            "aa", "oi", "oo", "ii",
        };

        public static string[] Suffixes = new string[]
        {
            "p",  "b",  "k",  "g",
            "d",  "t",  "nk", "ng",
            "mp", "mb", "sk", "ch",
            "nd", "nt", "f",  "sh",
        };

        public static string[] Accents = new string[]
        {
            "-", "'", "~", "*", ".", ","
        };

        /// <summary>
        /// The empty character
        /// </summary>
        public static Klunk EMPTY = new Klunk(0);
        public static Klunk NEWLINE = new Klunk(29);
        public static Klunk DOT = new Klunk(18 | 32 | 64); //val>16 indicates character. 32 indicates width = 3, 64 indicates height = 3
        public static Klunk NEGATE_TOP = new Klunk(19 | 32 | 64);
        public static Klunk NEGATE_SIDE = new Klunk(22 | 32);
        public static Klunk DASH = new Klunk(17 | 32 | 64);
        public static Klunk GRAVE = new Klunk(20 | 32 | 64);
        public static Klunk MANY = new Klunk(23 | 32 | 64);
        public static Klunk DIGIT = new Klunk(21);


        public byte val;
        public Klunk(byte val) {  this.val = val; }
        public Klunk(ushort val) { this.val = (byte)val; }


        public static implicit operator byte(Klunk d) => d.val;

        public static explicit operator Klunk(byte b) => new Klunk(b);

        /// <summary>
        /// Gets this character as the raw character string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ((char)val).ToString();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Klunk l && l.val == val) || (obj is byte b && b == val) || (obj is int i && i == val) || (obj is char c && (byte)c == val);
        }

        public string ToAccentString()
        {
            if (val == DOT.val) return ".";
            if (val == NEGATE_TOP.val) return "~";
            if (val == NEGATE_SIDE.val) return "~";
            if (val == DASH.val) return "-";
            if (val == GRAVE.val) return "`";
            if (val == MANY.val) return "*";
            return "";
        }

        public (string prefix, string body, string suffix) PartStrings()
        {
            return (KlonkString.Prefixes[val], KlonkString.Bodies[val], KlonkString.Suffixes[val]);
        }

        public static bool operator ==(Klunk a, Klunk b) => a.val == b.val;
        public static bool operator !=(Klunk a, Klunk b) => a.val != b.val;

        //TODO: if b is NOT or PLURAL then we generate a Klank(0, a, flag)
        public static Klank operator +(Klunk a, Klunk b) => new Klank(a, b);

        public UVXYWH Coords(int scale = 2, bool with_tick = true)
        {
            //the value is simplistically reduced based on (value - 1) which wraps around 8 columns
            if (val == NEWLINE.val) return new UVXYWH(0, 0, 0, 0, 0, 0);
            if (val == 0) return new UVXYWH(0, 0, 0, 0, 2 * scale, 0);
            //there are a few character types
            //accents are >= 64
            //there are two kinds
            //accents that are 3x3 units
            //and accents that are 2 units wide and 6 units high
            int v = (val & 0x1F) - 1;
            int x = (v & 0x7) * 4 * scale;
            int y = (v >> 3) * 6 * scale;
            int w = 4 * scale, h = 6 * scale; 
            if ((val & 32) != 0) w = 2 * scale;
            if ((val & 64) != 0) h = 3 * scale;
            if (v < 16 && !with_tick) h = 4 * scale;
            return new UVXYWH(x, y, 0, 0, w, h);
        }

        /// <summary>
        /// Gets the position for this thingy. Mode = 0 = solo letter, 1 = top, stack, 2 = bottom, 3 = top accent, 4 = under-accent
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public UVXYWH GetPos(int mode, int scale)
        {
            switch(mode)
            {
                case 0:
                    return Coords(scale, true).With(0, 3 * scale + ((scale & 64) >> 6) * 3 * scale);
                case 1:
                    return Coords(scale, true);
                case 2:
                    return Coords(scale, true).With(0, 6 * scale);
                case 3:
                    return Coords(scale, true).With(1, 0);
                case 4: return Coords(scale, true).With(1, 9 * scale);
            }
            return Coords(scale, true);
        }


    }

    public enum Flag
    {
        NONE = 0,
        NEGATE = 0b1000000000000000,
        MULTI =  0b0100000000000000,
        NMULTI = MULTI | NEGATE,
        DIGIT =  0b0010000000000000,
        NDIGIT = DIGIT | NEGATE,
        FLIP   = 0b0001000000000000,
        NFLIP  = FLIP | NEGATE,

    }

    public struct UVXYWH
    {
        public short U, V, X, Y, W, H;

        public UVXYWH(int U, int V, int X, int Y, int W, int H)
        {
            (this.U, this.V, this.X, this.Y, this.W, this.H) = ((short)U, (short)V, (short)X, (short)Y, (short)W, (short)H);
        }
        public UVXYWH With(int x, int y)
        {
            return new UVXYWH(U, V, x, y, W, H);
        }

        public UVXYWH Offset(int x, int y)
        {
            return new UVXYWH(U, V, X + x, Y + y, W, H);
        }

    }

    public struct Klank
    {
        public ushort val;
        public Klank(Klunk a, Klunk b)
        {
            val = (ushort)(a.val | (b.val << 8));
        }

        public Klank(int a, int b)
        {
            val = (ushort)(((Klunk)(byte)a).val | (((Klunk)(byte)b).val << 8));
        }


        public bool IsCompounded => (Top != Klunk.EMPTY && Bot == Klunk.EMPTY);

        public (Klunk top, Klunk bot) Letters => (new((byte)(val & 0xFF)), new((byte)((val >> 8) & 0xFF)));

        public Klunk Top => new ((byte)(val & 0xFF));
        public Klunk Bot => new((byte)((val >> 8) & 0xFF));


        public UVXYWH[] GetRectangles(int size)
        {
            //gets the rectangles for this syllable
            var l = Letters;
            //the rules are simple
            //accent letters at the top of the character get put above the letter
            //accent letters at the bottom of the character get put below the letter
            //accent chars with an empty bottom get placed inline

            //solo letters at the top or bottom are placed centrally
            //dual letters, top letter placed above, bottom letter placed below

            if (l.top == Klunk.NEWLINE || l.bot == Klunk.NEWLINE) return [];

            if (l.top == Klunk.EMPTY)
            {
                if (l.bot == Klunk.EMPTY)
                {
                    //it's a space
                    return [new UVXYWH(0, 0, 0, 0, 4 * size, 0)];
                }
                else
                {
                    return [l.bot.GetPos(0, size)];
                }
            }
            else if (l.bot == Klunk.EMPTY)
            {
                return [l.top.GetPos(0, size)];
            }
            else
            {
                if (l.top.val > 16)
                {
                    return [l.bot.GetPos(0, size), l.top.GetPos(3, size)];
                }
                else if (l.bot.val > 16)
                {
                    return [l.top.GetPos(0, size), l.bot.GetPos(4, size)];
                }
                return [l.top.GetPos(1, size), l.bot.GetPos(2, size)];
            }

            //Prefixes invoke verb and noun classification, so
            //Kl -> doing words
            //Bl -> making words
            //Fl -> going words
            //These can be negated, and they can be inflected
            //A negation is indicated by a ~ visually and by a suffix verbally
            //An inflection is indicated by a ' visually and by a suffix verbally
            //Negations are simple, they just mean "I DON'T do this" so like, "I didn't do it." or "I didn't go there"
            //Inflections are explicitly defined though somewhat intuitive
            //e.g I did this -> this was done to me
            //I go there -> I came from there
            //I give it -> I take it
            //I make it -> I destroy it

            //Kl'onk -> inflected prefix
            //kl~onk -> negated prefix

            //anyway this returns the rectangles


        }

        public string GetRomanized()
        {
            var l = Letters;
            if (l.top == Klunk.NEWLINE || l.bot == Klunk.NEWLINE) return "\n";

            if (l.top == Klunk.EMPTY)
            {
                if (l.bot == Klunk.EMPTY)
                {
                    //it's a space
                    return " ";
                }
                else
                {
                    if (l.bot.val <= 16) return Klunk.Prefixes[l.bot.val - 1];
                }
            }
            else if (l.bot == Klunk.EMPTY)
            {
                if (l.top.val <= 16) return Klunk.Prefixes[l.top.val - 1];
                else return l.top.ToAccentString();
            }
            else
            {
                if (l.top.val > 16)
                {
                    
                    return Klunk.Prefixes[l.bot.val - 1] + l.top.ToAccentString();
                }
                else if (l.bot.val > 16)
                {
                    return Klunk.Prefixes[l.top.val - 1] + l.bot.ToAccentString();

                }
                return Klunk.Bodies[l.top.val - 1] + Klunk.Suffixes[l.bot.val - 1];
            }
            return "";
        }
    }


    public class Klonk
    {
        public Klank[] body;
        public static Image StringTex;

        public Klonk(Klunk[] letters) 
        {
            body = new Klank[letters.Length / 2];
            for(int i = 0; i < letters.Length - 1; i += 2)
            {
                body[i / 2] = new(letters[i], letters[i + 1]);
            }
        }

        public Klonk(Klank[] letters)
        {
            body = letters;
        }

        public Klonk(string romanized)
        {

        }

        public string ToRomanizedString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < body.Length; i++)
            {
                //there are a few cases
                //1. Centralized letter
                //2. Stack (top bottom)
                //3. Accented letter
                //4. Centralized Symbol
                Klank k = body[i];
                sb.Append(k.GetRomanized());
            }
            return sb.ToString();
        }

        public void Render(Graphics g, int x, int y, int size = 2)
        {
            int a = x;
            int b = y;
            for(int i = 0; i < body.Length; i++)
            {
                var u = body[i].GetRectangles(size);
                int stepX = 0;
                int stepY = 0;
                if (u.Length == 0)
                {
                    b += 14 * size;
                    a = x;
                    continue;
                }
                else
                {
                    for(int j = 0; j < u.Length; j++)
                    {
                        //GDI approach
                        g.DrawImage(StringTex,
                            new Rectangle(u[j].X + a, u[j].Y + b, u[j].W, u[j].H),
                            new Rectangle(u[j].U, u[j].V, u[j].W, u[j].H), GraphicsUnit.Pixel);
                        stepX = int.Max(stepX, u[j].W);
                    }
                }
                a += stepX + (2 * size) / 3;
                b += stepY;
            }

        }



        public interface IRenderer
        {
            public void DrawText(Klonk k, int x, int y, int size);
        }

        /// <summary>
        /// A default GDI Renderer implementation
        /// </summary>
        public class GDIRenderer : IRenderer
        {
            public void DrawText(Klonk k, int x, int y, int size)
            {
                int a = x;
                int b = y;
                for (int i = 0; i < k.body.Length; i++)
                {
                    var u = k.body[i].GetRectangles(size);
                    int stepX = 0;
                    int stepY = 0;
                    if (u.Length == 0)
                    {
                        b += 14 * size;
                        a = x;
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < u.Length; j++)
                        {
                            //GDI approach
                            //g.DrawImage(StringTex,
                            //    new Rectangle(u[j].X + a, u[j].Y + b, u[j].W, u[j].H),
                            //    new Rectangle(u[j].U, u[j].V, u[j].W, u[j].H), GraphicsUnit.Pixel);
                            //stepX = int.Max(stepX, u[j].W);
                        }
                    }
                    a += stepX + (2 * size) / 3;
                    b += stepY;
                }

            }
        }

        /// <summary>
        /// A generic implementation for rendering Klonks to a spritebatch
        /// This class should be instantiated via reflection and runtime dynamic resolution
        /// </summary>
        public class MonogameRenderer : IRenderer
        {
            private dynamic spriteBatch;
            private dynamic[] StringTextures;

            public MonogameRenderer(dynamic spriteBatch, dynamic[] stringTextures)
            {
                this.spriteBatch = spriteBatch;
                //loads the textures for the font, each index corresponds to a size
                StringTextures = stringTextures;
                //The size array should contain 1,2,3... where this indicates character width of 2*(size+1) and height of 3*(size+1)
                //if size exceeds this amount then we use the largest and scale it up accordingly>
                //Nay. the size is given in pixels of character height.
                //To this end, 
            }

            public void DrawText(Klonk k, int x, int y, int size)
            {
                int a = x;
                int b = y;
                for (int i = 0; i < k.body.Length; i++)
                {
                    var u = k.body[i].GetRectangles(size);
                    int stepX = 0;
                    int stepY = 0;
                    if (u.Length == 0)
                    {
                        b += 14 * size;
                        a = x;
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < u.Length; j++)
                        {
                            //Spritebatch.Draw goes here
                            //g.DrawImage(StringTex,
                            //    new Rectangle(u[j].X + a, u[j].Y + b, u[j].W, u[j].H),
                            //    new Rectangle(u[j].U, u[j].V, u[j].W, u[j].H), GraphicsUnit.Pixel);
                            stepX = int.Max(stepX, u[j].W);
                        }
                    }
                    a += stepX + (2 * size) / 3;
                    b += stepY;
                }
            }

            // Implement other rendering methods
        }



    }




}
