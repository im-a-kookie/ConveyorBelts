using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteSheetHelper
{
    internal class Lingual
    {

        //The krumbens use a glyphed language
        //All words are a consonant, a vowel, and a consonant [start, middle, end]
        //Words can be hybridized using the last half of the next
        //e.g Hoob Heek => Hoobeek
        //Hybridization indicates that te words self-modify
        //For example, if UP and NOT are combined, UPOT, then it means Down
        //But if Up and Not are given separately, then it just means not up
        //Or GONOT means Stay and HERENOT means THERE
        //But GO NOT HERENOT means Don't Go There
        //and GONOT HERE NOT means Stay Not Here, and is equivalent to GONOT NOT HERE
        //but gramatically, the action takes pluralization of the object
        //and the object takes negation of the action

        //Each glyph holds up to 9 components to accommodate this
        //Though Krumben can be written in lines of text
        //e.g if a word is 012 and a hybrid is 012-34 and so on
        //Krumben can be written 012 01234 012 012 0123456
        //But these can also be combinated into glphs

        //Written;
        /*
        
        0 1 2

        0 1 2
          3 4     

        0 1 2
        3 4
        5 6

        0 1 2
        3 4 7
        5 6 8

        */


        //     X
        //   X X X  for a full glyph, or  X X X
        //     X

        public static Dictionary<string, string> SyllableMap = new Dictionary<string, string>()
        {


            //There are 9 sacred cookies, these are Om, Im, Em, Erm, Oom, Am, Arm, Oim, Orm, Eem
            //and so M is a sacred letter

            //10 consonants reserved for names
            
            //B, Sp, Bl, Spl, D, Sm, Sn, Spr, Br
            { "n0", "B" },
            { "n1", "Sp" },
            { "n2", "Bl" },
            { "n3", "Spl" },
            { "n4", "D" },
            { "n5", "Sm" },
            { "n6", "Sn" },
            { "n7", "Spr" },
            { "n8", "Br" },
            { "n9", "Dr" },

            //Generic prefixes
            { "p0", "Kl"},
            { "p1", "Kr"},
            { "p2", "Fl"},
            { "p3", "Y"},
            { "p4", "H"},
            { "p5", "K"},
            { "p6", "G"},
            { "p7", "Gl"},
            { "p8", "Gr"},
            { "p9", "J"},

             //Krumben vowels. They use 10.
            { "v0", "o" }, //o line bonk
            { "v1", "i" }, //i like flick
            { "v2", "e" }, //e like Beg
            { "v3", "u" }, //er like twerk
            { "v4", "oo" }, //oo like floog
            { "v5", "a" }, //a like hat
            { "v6", "ar" }, //a like art, ar
            { "v7", "oi" }, //oi like boink
            { "v8", "or" }, //or like bork
            { "v9", "er" }, //or like Tweet

            //Suffixes are versatile
            { "s0", "g"},
            { "s1", "b"},
            { "s2", "l"},
            { "s3", "p"},
            { "s4", "nk"},
            { "s5", "lk"},
            { "s6", "mb"},
            { "s7", "nt"},
            { "s8", "k"},
            { "s9", "t"},


        };

        public static Dictionary<string, string> WordRoots = new Dictionary<string, string>()
        {
            //Basic reality           
            { "Yes",     "p5,v0,s8" },
            { "No",      "p5,v1,s8" },
            { "Many",    "p5,v2,s8" },
            { "Static",  "p5,v3,s8" },
            { "Fast",    "p5,v4,s8" },
            { "One",     "p5,v5,s8" },
            { "Big",     "p5,v6,s8" },
            { "Smooth",  "p5,v7,s8" },
            { "Sharp",   "p5,v8,s8" },
            { "Hard",    "p5,v9,s8" },

            { "Basic10", "p6,v0,s0" },
            { "Basic11", "p6,v1,s0" },
            { "Basic12", "p6,v2,s0" },
            { "Basic13", "p6,v3,s0" },
            { "Basic14", "p6,v4,s0" },
            { "Basic15", "p6,v5,s0" },
            { "Basic16", "p6,v6,s0" },
            { "Basic17", "p6,v7,s0" },
            { "Basic18", "p6,v8,s0" },
            { "Basic19", "p6,v9,s0" },

            //Positions
            { "Up",      "p3,v0,s9" },
            { "Side",    "p3,v1,s9" },
            { "In",      "p3,v2,s9" },
            { "On",      "p3,v3,s9" },
            { "Ordered", "p3,v4,s9" },

            
            { "Thing", "p1,v3,s1" },
            { "Do", "p4,v3,s8" },

            //Things
            { "Me", "p1,v2,s1" },            
            { "Cookie", "p1,v3,s6" },
            { "Thing", "p0,v3,s1" },

            //Verbs
            { "Have", "p4,v5,s8" },
            { "Speak", "p3,v9,s4" },

            { "Clean", "p4,v2,s7" },

            { "Place", "p7,v4,s3" },

            //Descriptions of the universe
            //All things can be described from here
            //Still
            { "Science0",       "p3,v1,s2" },
            { "Liquid",         "p3,v1,s2" },
            { "Gas",            "p3,v2,s2" },
            { "Information",    "p3,v3,s2" },
            { "Energy",         "p3,v4,s2" },
            { "Force",          "p3,v5,s2" },
            { "Time",           "p3,v6,s2" },
            { "Science1",       "p3,v6,s2" },
            { "Space",          "p3,v8,s2" },
            { "Quantum",        "p3,v9,s2" },

        };

        public static Dictionary<string, string> Compounds = new Dictionary<string, string>()
        {

        };

        public static Dictionary<string, string> WordsEnglish = new Dictionary<string, string>();


        public static void Build()
        {
            WordsEnglish.Clear();



        }


    }
}
