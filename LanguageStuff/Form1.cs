
using System.Buffers;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Versioning;
using System.Text;

namespace LanguageStuff
{
    public partial class Form1 : Form
    {

        Image textthing;

        public static Dictionary<char, string> CharToPre = new ()
        {
            { '0', "P" },   { '1', "B" },    { '2', "K" },    { '3', "G" },
            { '4', "Pr" },  { '5', "Br" },   { '6', "Kr" },   { '7', "Gr" },
            { '8', "Pl" },  { '9', "Bl" },   { 'A', "Kl" },   { 'B', "Gl" },
            { 'C', "Spr" }, { 'D', "Spl" },  { 'E', "D" },  { 'F', "Sp" },
        };

        public static Dictionary<char, string> CharToVowel = new()
        {
            { '0', "a" },   { '1', "o" },    { '2', "e" },    { '3', "u" },
            { '4', "ar" },  { '5', "or" },   { '6', "er" },   { '7', "ur" },
            { '8', "ai" },  { '9', "oi" },   { 'A', "i" },   { 'B', "ioo" },
            { 'C', "ayi" }, { 'D', "ore" },  { 'E', "ee" }, { 'F', "oo" },
        };

        public static Dictionary<char, string> CharToSuffix = new()
        {
            { '0', "np" },  { '1', "nb" },   { '2', "nk" },  { '3', "ng" },
            { '4', "p" },   { '5', "b" },    { '6', "k" },   { '7', "g" },
            { '8', "v" },   { '9', "f" },    { 'A', "n" },  { 'B', "sh" },
            { 'C', "mb" },  { 'D', "nd" },   { 'E', "j" },   { 'F', "l" },
        };

        //We need some key classifiers

        //Plain -> Time,  Place,  Thing,  Person, Thought
        //R     -> Do,    Go,     Be,     Use,    Ask
        //L     -> Cause, Take,   Give,   Want,   Know
        public Dictionary<string, string> Classifiers = new()
        {
            { "Kr", "Thing" },
            { "Br", "Time" },
            { "Pr", "Process" },
            { "Gr", "Place" },

            { "K", "Have" },
            { "B", "Be" },
            { "P", "Do" },
            { "G", "Go" },

            { "Kl", "Take" },
            { "Bl", "Make" },
            { "Pl", "Cause" },
            { "Gl", "Give" },

            { "Sp", "Organism" },
            { "Spl", "Machine" },
            { "Spr", "Want" },

            { "D", "Number" }, 

        };


        public static Dictionary<string, string> BasicWords = new()
        {
            //every word group has a word for cookie
            

        };

        public static Dictionary<string, string> Nouns = new Dictionary<string, string>()
        {

        };

        public static Dictionary<string, char> ConjunctionToChar;


        public static Dictionary<string, char> PrefixToChar;
        public static Dictionary<string, char> VowelToChar;
        public static Dictionary<string, char> SuffixToChar;
        public static Dictionary<string, string> WordLookup;
        public static Dictionary<char, Rectangle> CharMap;

        public Form1()
        {
            InitializeComponent();

            int dimension = 8;

            textthing = new Bitmap(Resource1.language);
            BuildWordCaches();

            panel1.Paint += Panel1_Paint;


        }

        public static void BuildWordCaches(int cell_size = 8, int character_width = 4, int character_height = 4, int accent_space = 2)
        {
            CharMap = new();

            string letters = "0123456789ABCDEF";
            string numbers = "abcdefghijklmnop";
            string raw_letters = "GHIJKLMNOPQRSTUV";
            string symbols = "*-.\'↑→↓←+_×÷^¬⌠Ø";
            string symbols2 = "=≠?!";

            //read the characters
            for (int i = 0; i < 16; i++)
            {
                int x = (i & 0x3) * cell_size;
                int y = (i >> 2) * cell_size;
                CharMap.Add(raw_letters[i], new Rectangle(x, y, character_width, character_height));
                CharMap.Add(letters[i], new Rectangle(x, y, character_width, character_height + accent_space));
                CharMap.Add(numbers[i], new Rectangle(x, y + 4 * cell_size, character_width, character_height + accent_space));
                CharMap.Add(symbols[i], new Rectangle(x + 4 * cell_size, y, character_width, character_height + accent_space));
                if (i < 4) CharMap.Add(symbols2[i], new Rectangle(x + 4 * cell_size, y + 4 * cell_size, character_width, character_height + accent_space));
            }

            PrefixToChar = new();
            foreach (var k in CharToPre) PrefixToChar.Add(k.Value.ToLower(), k.Key);
            VowelToChar = new();
            foreach (var k in CharToVowel) VowelToChar.Add(k.Value.ToLower(), k.Key);
            SuffixToChar = new();
            foreach (var k in CharToSuffix) SuffixToChar.Add(k.Value.ToLower(), k.Key);

            ConjunctionToChar = new();
            foreach (var k in CharToPre) SuffixToChar.Add(k.Value.ToLower() + CharToVowel[k.Key].ToLower(), k.Key);

        }

        public struct SourceDest
        {
            public Rectangle Source;
            public Point Dest;
            public SourceDest()
            {
                Source = new Rectangle(0, 0, 0, 0);
                Dest = new Point(0, 0);
            }

            public SourceDest(Rectangle source, Point dest)
            {
                Source = source;
                Dest = dest;
            }
        }

        public static string Romanize(string text)
        {
            StringBuilder sb = new StringBuilder();


            var lines = text.Split('\n').ToArray();
            for (int line = 0; line < lines.Length; line++)
            {
                int line_y = line * 14;
                string t = lines[line];
                //"text" has been processed at this stage into the basic representation
                //Which now describes the written syntax;
                //PVSVS P-PVS P'VSVS ...
                //Note that "commas" or pauses are represented by spaces
                //as words are differentiated by the prefix placement.
                int stage = 0;
                int pos = 0;
                bool last_was_prefix = false;
                bool last_was_negate = false;

                for (int i = 0; i < t.Length; i++)
                {
                    if (t[i] == '.')
                    {
                        sb.Append(". ");
                        stage = 0;
                        continue;
                    }
                    if (t[i] == ',')
                    {
                        sb.Append(", ");
                        stage = 0;
                        continue;
                    }
                    if (t[i] == ' ')
                    {
                        sb.Append(" ");
                        stage = 0;
                        continue;
                    }
                    else if (t[i] == '-')
                    {
                        //draw the dash in center line and advance
                        if (stage == 2) throw new Exception("Invalid spelling at line " + line + ", pos: " + i);
                        //draw the dash center line
                        //result.Add(new SourceDest(CharMap['-'], new Point(pos, 3)));

                        if (i > 0 && CharToVowel.ContainsKey(t[i-1]))
                        {
                            sb.Append(CharToVowel[t[i - 1]]);
                        }

                        sb.Append("-");
                        stage = 0;
                        last_was_negate = false;
                    }
                    else if (t[i] == '.')
                    {
                        //draw the dash in center line and advance
                        if (stage == 2) throw new Exception("Invalid spelling at line " + line + ", pos: " + i);
                        //draw the dash center line
                        //result.Add(new SourceDest(CharMap['.'], new Point(pos, 3)));
                        stage = 0;
                        last_was_negate = false;
                    }
                    else if (t[i] == '\'' || t[i] == '!')
                    {

                        if (last_was_prefix)
                        {
                            //result.Add(new SourceDest(CharMap['\''], new Point(pos - pos_step, 0)));
                            sb.Append("'");
                        }
                        else
                        if (stage == 1)
                        {
                            //result.Add(new SourceDest(CharMap['!'], new Point(pos, 3)));
                            sb.Append("'");
                            last_was_negate = true;
                        }
                        else throw new Exception("Invalid negative at line " + line + ", pos: " + i + ", stage: " + stage);
                    }
                    //star refers to "many"
                    else if (t[i] == '*')
                    {
                        //Append the negative word
                        if (stage != 2)
                        {
                            sb.Append("*");
                            //result.Add(new SourceDest(CharMap['*'], new Point(pos - pos_step, 0)));
                            last_was_negate = false;
                        }
                        else throw new Exception("Invalid star at line " + line + ", pos: " + i);
                    }
                    else
                    {
                        //draw the character
                        Dictionary<char, string> relevant = (Dictionary<char,string>)new object[]{CharToPre, CharToVowel, CharToSuffix }[stage] ;
                        if (relevant.ContainsKey(t[i]))
                        {
                            last_was_prefix = false;
                            switch (stage)
                            {
                                case 0:
                                    if (last_was_negate) pos += 2;
                                    //result.Add(new SourceDest(src, new Point(pos, 3)));
                                    sb.Append(relevant[t[i]]);
                                    stage = 1;
                                    last_was_prefix = true;
                                    break;
                                case 1:
                                    //result.Add(new SourceDest(src, new Point(pos, 0)));
                                    sb.Append(relevant[t[i]]);

                                    stage = 2;
                                    break;
                                case 2:
                                    //result.Add(new SourceDest(src, new Point(pos, 6)));
                                    sb.Append(relevant[t[i]]);

                                    stage = 1;
                                    break;
                            }
                        }
                        last_was_negate = false;
                    }

                }


                //Digits are written using Underlined characters generally
                //Which allows simple maths to be written, such as with the inlining; 1+2=3
                //Complete maths expressions are written using a different renderer that I may or may not make
                //but it would be kinda cool



                sb.Append("\n");

            }
            return sb.ToString();
        }


        public static void ComputeParts(string text, out SourceDest[] parts, out int count)
        {
            var result = new List<SourceDest>();
            count = 0;

            int word_space = 1;
            int word_width = 4;
            int markup_step = 4;

            int pos_step = word_space + word_width;

            //split the text into lines
            //and then into words

            var lines = text.Split('\n').ToArray();
            for(int line = 0; line < lines.Length; line++)
            {
                int line_y = line * 14;
                string t = lines[line];
                //"text" has been processed at this stage into the basic representation
                //Which now describes the written syntax;
                //PVSVS P-PVS P'VSVS ...
                //Note that "commas" or pauses are represented by spaces
                //as words are differentiated by the prefix placement.
                int stage = 0;
                int pos = 0;
                bool last_was_prefix = false;
                bool last_was_negate = false;

                for (int i = 0; i < t.Length; i++)
                {
                    if (t[i] == ',')
                    {
                        stage = 0;
                        pos += 4;
                        continue;
                    }
                    if (t[i] == ' ')
                    {
                        stage = 0;
                        continue;
                    }
                    else if (t[i] == '-')
                    {
                        //draw the dash in center line and advance
                        if (stage == 2) throw new Exception("Invalid spelling at line " + line + ", pos: " + i);
                        //draw the dash center line
                        result.Add(new SourceDest(CharMap['-'], new Point(pos, 3)));
                        stage = 0;
                        pos += markup_step;
                        last_was_negate = false;
                    }
                    else if (t[i] == '.')
                    {
                        //draw the dash in center line and advance
                        if (stage == 2) throw new Exception("Invalid spelling at line " + line + ", pos: " + i);
                        //draw the dash center line
                        result.Add(new SourceDest(CharMap['.'], new Point(pos, 3)));
                        stage = 0;
                        pos += markup_step;
                        last_was_negate = false;
                    }
                    else if (t[i] == '\'' || t[i] == '!')
                    {

                        if (last_was_prefix)
                        {
                            result.Add(new SourceDest(CharMap['\''], new Point(pos - pos_step, 0)));
                        }
                        else
                        if (stage == 1)
                        {
                            result.Add(new SourceDest(CharMap['!'], new Point(pos, 3)));
                            pos += markup_step - 1;
                            last_was_negate = true;
                        }
                        else throw new Exception("Invalid negative at line " + line + ", pos: " + i + ", stage: " + stage);
                    }
                    else if (t[i] == '*')
                    {
                        //Append the negative word
                        if (stage == 1)
                        {
                            result.Add(new SourceDest(CharMap['*'], new Point(pos - pos_step, 0)));
                            last_was_negate = false;
                        }
                        else throw new Exception("Invalid star at line " + line + ", pos: " + i);
                    }
                    else
                    {
                        //draw the character
                        if (CharMap.ContainsKey(t[i]))
                        {
                            Rectangle src = CharMap[t[i]];
                            last_was_prefix = false;
                            switch (stage)
                            {
                                case 0:
                                    if (last_was_negate) pos += 2;
                                    result.Add(new SourceDest(src, new Point(pos, 3)));
                                    pos += pos_step;
                                    stage = 1;
                                    last_was_prefix = true;
                                    break;
                                case 1:
                                    result.Add(new SourceDest(src, new Point(pos, 0)));
                                    stage = 2;
                                    break;
                                case 2:
                                    result.Add(new SourceDest(src, new Point(pos, 6)));
                                    pos += pos_step;
                                    stage = 1;
                                    break;
                            }
                        }
                        last_was_negate = false;
                    }
                }
            }
            parts = result.ToArray();
            count = result.Count;
        }

        public static string ProduceTextLiteral(string romanized)
        {
            //this can be divided into words
            //which will contain their own ' and - and so on
            //
            var words = romanized.Replace(".", " . ").Replace("\n", "").Split(' ').Where(x => x.Length > 0);

            StringBuilder sb = new StringBuilder();
            foreach(var w in words)
            {
                if (w.Equals(".")) sb.Append(".");
                else WordToLiteral(w.ToCharArray(), sb);
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static void WordToLiteral(Span<char> word, StringBuilder result)
        {
            //converts a word to the literal hex-ish representation
            int stage = 0;

            //first we need to resolve conjunctions
            //which are done with a "-"
            //and generally only contain prefix-prefix-...-PVS
            //or rather, PV-PV-PVS where PV is the first two letters of the prefix, so... idk

            int j = 0;
            bool had_real_vowel = false;
            for(int i = 0; i < word.Length; i++)
            {
                char c = (char)word[i];

                bool is_fv = "ryw".Contains(c);
                bool is_rv = "aeiou".Contains(c);
                bool isV = is_fv | is_rv;

                if (is_rv) had_real_vowel = true;

                if (c == '-')
                {
                    //j to c is a conjunction
                    var spart = word.Slice(j, i - j - 1);
                    var part = spart.ToString().ToLower();
                    if (ConjunctionToChar.TryGetValue(part, out var r)) result.Append(r);
                    result.Append("-");
                    stage = 0;
                    had_real_vowel = false;
                    i += 1;
                    j = i;
                }
                else if (c == '*')
                {

                    var spart = word.Slice(j, i - j);
                    var part = spart.ToString().ToLower();
                    //apostrophe means we terminate the current block
                    if (stage == 0)
                    {
                        //we were in the prefix so use a prefix
                        if (PrefixToChar.TryGetValue(part, out var r)) result.Append(r);
                    }
                    result.Append("*");
                    i += 1;
                    j = i;
                    stage = 1;
                    had_real_vowel = false;

                }
                else if (c == '\'')
                {

                    var spart = word.Slice(j, i - j);
                    var part = spart.ToString().ToLower();
                    //apostrophe means we terminate the current block
                    if (stage == 0)
                    {
                        //we were in the prefix so use a prefix
                        if (PrefixToChar.TryGetValue(part, out var r)) result.Append(r);
                    }
                    else
                    {
                        if (SuffixToChar.TryGetValue(part, out var r)) result.Append(r);
                    }
                    result.Append("'");
                    i += 1;
                    j = i;
                    stage = 1;
                    had_real_vowel = false;

                }
                else if (stage == 0)
                {
                    //we are in the prefix, and we arrived at a vowel
                    if (isV && had_real_vowel)
                    {
                        var spart = word.Slice(j, i - j);
                        var part = spart.ToString().ToLower();
                        if (PrefixToChar.TryGetValue(part, out var r)) result.Append(r);
                        j = i;
                        stage = 1;
                        had_real_vowel = false;

                    }
                }
                else if (stage == 1)
                {
                    //we are in a vowel section
                    //so we break if it is not a vowel
                    if (!isV)
                    {
                        //grab up to i
                        var spart = word.Slice(j, i - j);
                        var part = spart.ToString().ToLower();
                        if (VowelToChar.TryGetValue(part, out var r)) result.Append(r);

                        stage = 2;
                        j = i;
                        had_real_vowel = false;

                    }


                }
                else if (stage == 2)
                {
                    //we are suffixing, so we break if we hit a vowel
                    if(isV)
                    {
                        var spart = word.Slice(j, i - j);
                        var part = spart.ToString().ToLower();
                        if (SuffixToChar.TryGetValue(part, out var r)) result.Append(r);
                        j = i;
                        stage = 1;
                        had_real_vowel = false;

                    }
                }


            }

            {
                var spart = word.Slice(j, word.Length - j);
                var part = spart.ToString().ToLower();
                switch (stage)
                {
                    case 0:
                        if (PrefixToChar.TryGetValue(part, out var r)) result.Append(r);
                        break;
                    case 1:
                        if (VowelToChar.TryGetValue(part, out r)) result.Append(r);
                        break;
                    case 2:
                        if (SuffixToChar.TryGetValue(part, out r)) result.Append(r);

                        break;

                }
            }
            

        }


        private void Panel1_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.ResetTransform();
            e.Graphics.ScaleTransform(4f, 4f);
            e.Graphics.TranslateTransform(4, 4);
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.Clear(panel1.BackColor);


            //process the syllables
            try
            {
                ComputeParts(ProduceTextLiteral(textBox1.Text), out var parts, out var count);
                for (int i = 0; i < count; i++)
                {
                    e.Graphics.DrawImage(textthing, new Rectangle(parts[i].Dest.X, parts[i].Dest.Y, parts[i].Source.Width, parts[i].Source.Height), parts[i].Source, GraphicsUnit.Pixel);
                }
            }
            catch(Exception ex)
            {
                label1.Text = "Error: " + ex.Message;
            }


            

        }

  

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = ProduceTextLiteral(textBox1.Text);
             
            panel1.Refresh();
        }
    }
}
