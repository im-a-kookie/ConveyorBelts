using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LanguageStuff.Language
{
    internal class KlonkString
    {

        public static string Pres = "KGPBKrGrPrBrKlGlPlBlFlSpSprSpl";
        public static byte[] starts = { 0, 1, 2, 3, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 27, 30 };
        
        public string Prefix(int i)
        {
            var s = new Span<char>(Pres.ToArray());
            return s.Slice(starts[i & 0xF], starts[(i & 0xF) + 1] - starts[i & 0xF]).ToString();
        }


        public static string[] Prefixes = new string[16];
        public static string[] Bodies = new string[16];
        public static string[] Suffixes = new string[16];


        public static byte[] Bounds = new byte[48];
        public static char[] RomanChar = new char[48];

        static KlonkString()
        {

            //the characters all use ASCII char codes so basically everything just goes in an array from 0-255
            string valid = "0123456789ABCDEF-.";

            for(int i = 0; i < 16; i++)
            {
                int x = (i & 0x3) * 4;
                int y = (i >> 2) * 6;
                //the bounds are justt references to the 8x8 grid and fit into one byte
                RomanChar[i] = valid[i];
                Bounds[i] = (byte)(x | (y << 4)); 
            }

            for (int i = 0; i < 16; i++)
            {
                int x = 4 + i & 0x3;
                int y = i >> 2;
                //the bounds are just in pixels 32x32
                //so we can fit it into an int
                RomanChar[i] = valid[i + 16];
                Bounds[i] = (byte)(x | (y << 4));
            }


        }



        public int len = 0;
        public Klunk[] chars = new Klunk[1];


        public KlonkString() { }

        public KlonkString(string romanized)
        {


        }

        public KlonkString(byte[] raw)
        {
            chars = new Klunk[raw.Length];
            var vectorSpan = MemoryMarshal.Cast<byte, Klunk>(raw);
            chars = vectorSpan.ToArray();
        }

        public KlonkString(Klunk[] letters) 
        {
            chars = letters;
        }

        public string ToRomanString()
        {
            //surprisingly complicated
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < chars.Length; i++)
            {
                //possibilities
                //1. (Empty, LETTER) => use the prefix for A
                //2. (Empty, EMPTY) => insert a space
                //3. (LETTER, EMPTY) => the suffix ends here with just a vowel (e.g Kree-) (Question)
                //4. (LETTER, LETTER) => body, suffix
                //5. (SYMBOL, LETTER) => letter has a modifier, use prefix + modifier
                //6. (LETTER, SYMBOL) => Symbol should be, exclusively, a dash
                //7. (EMPTY, SYMBOL) => return the symbol
            }
            return sb.ToString();
        }


        /// <summary>
        /// Strives to condense this Klonk string to a blob of raw word meanings.
        /// </summary>
        /// <returns></returns>
        public string ToDefinitionBlob()
        {
            return "";
        }

        /// <summary>
        /// Converts this string to a raw byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            Span<byte> s = MemoryMarshal.Cast<Klunk, byte>(chars);
            return s.ToArray();
        }


        public void ToTextureBounds(out Rectangle[] rect, out int count)
        {
            rect = ArrayPool<Rectangle>.Shared.Rent(chars.Length);
            count = 0;
            for(int i = 0; i < chars.Length; ++i)
            {

                if (i >= rect.Length)
                {
                    var r = ArrayPool<Rectangle>.Shared.Rent((3 * i) / 2);
                    Array.Copy(rect, r, rect.Length);
                    ArrayPool<Rectangle>.Shared.Return(rect);
                    rect = r;
                }


                //add the bounds from the character



            }

        }




    }
}
