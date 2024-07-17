using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SpriteSheetHelper
{
    internal class Namer
    {

        public class Syl
        {
            public static Dictionary<string, Syl> map = new Dictionary<string, Syl>();
            public string t="";
            public Dictionary<Syl, HashSet<Syl>> chains = new Dictionary<Syl, HashSet<Syl>>();

            public static void S(IEnumerable<string> ss0, IEnumerable<string> ss1, IEnumerable<string> ss2)
            {
                foreach(string _s0 in ss0)
                {
                    if (!map.ContainsKey(_s0)) map.Add(_s0, new() { t = _s0 });
                    var s0 = map[_s0];
                    foreach(string _s1 in ss1)
                    {
                        if (!map.ContainsKey(_s1)) map.Add(_s1, new() { t = _s1 });
                        var s1 = map[_s1];
                        if (!s1.chains.ContainsKey(s0)) s1.chains.Add(s0, new());
                        var set = s1.chains[s0];
                        foreach(string _s2 in ss2)
                        {
                            //add them to the set
                            if (!map.ContainsKey(_s2)) map.Add(_s2, new() {  t = _s2 });
                            var s2 = map[_s2];
                            set.Add(s2);
                        }

                    }
                }
            }
            public override string ToString()
            {
                return t;
            }

        }

        public static void Build()
        {

            string[] vowels = ["oi", "o", "or", "ar", "i", "ir", "er", "oo", "e", "ee"];
            string[] consonants = ["b", "sp", "bl", "spl", "d", "sm", "sn", "spr", "br"];

            var cons_no_n = consonants.Where(x => !(x.EndsWith("n") | x.EndsWith("m")));
            var cons_only_n = consonants.Where(x => (x.EndsWith("n") | x.EndsWith("m")));
            var cons_only_r = consonants.Where(x => (x.EndsWith("r")));
            var cons_no_r = consonants.Where(x => (!x.EndsWith("r")));
            Syl.S(["-"], cons_no_r, vowels);
            Syl.S(["-"], cons_only_r, vowels.Where(x => !x.EndsWith("r")));

            Syl.S(cons_only_n, vowels, ["k"]);
            Syl.S(cons_no_n, vowels.Where(x => x.EndsWith("r")), ["nk"]);
            Syl.S(cons_no_n, vowels.Where(x => !x.EndsWith("r")), ["k"]);


            Syl.S(["b"], ["oi"], ["nk", "k", "b", "g"]);
            Syl.S(["b"], ["o"], ["nk", "ll", "lk"]);
            Syl.S(["b"], ["or"], ["k", "g", "b", "l"]);
            Syl.S(["b"], ["i"], ["nk", "mp", "lk"]);
            Syl.S(["b"], ["ir"], ["b", "k", "p"]);
            Syl.S(["b"], ["er"], ["g", "b", "p", "k", "t"]);
            Syl.S(["b"], ["ar"], ["g", "p", "l"]);
            Syl.S(["b"], ["oo"], ["b", "g", "p"]);

            Syl.S(["d"], ["oi"], ["nk", "b", "g"]);
            Syl.S(["d"], ["o"], ["nk", "lk"]);
            Syl.S(["d"], ["or"], ["k", "g", "b", "l"]);
            Syl.S(["d"], ["i"], ["nk", "mp"]);
            Syl.S(["d"], ["ir", "ar"], ["b", "g"]);
            Syl.S(["d"], ["er"], ["g", "p", "l"]);
            Syl.S(["d"], ["aa", "oo"], ["g", "b", "k", "nk"]);

            Syl.S(["br", "spr"], ["oi"], ["nk", "b", "g", "p"]);
            Syl.S(["br", "spr"], ["o"], ["nk", "b", "p"]);
            Syl.S(["br"], ["i"], ["b", "p", "k"]);
            Syl.S(["spr"], ["i"], ["nk", "ll"]);
            Syl.S(["spr"], ["oo"], ["g", "l"]);

            Syl.S(["sp"], ["oi"], ["nk", "k", "b", "g"]);
            Syl.S(["sp"], ["o"], ["nk", "p", "b", "g", "nt", "mp", "ll"]);
            Syl.S(["sp"], ["or"], ["k", "g", "b"]);
            Syl.S(["sp"], ["i"], ["nk", "b", "p", "mb", "g"]);
            Syl.S(["sp"], ["ir"], ["b", "k", "g", "t"]);
            Syl.S(["sp"], ["er", "ar"], ["g", "b", "p", "k"]);
            Syl.S(["sp"], ["aa", "oo", "ee"], ["g", "b", "p", "k"]);

            Syl.S(["sm"], vowels, ["p", "b", "k", "nk"]);
            Syl.S(["sn"], vowels.Except(["oo"]), ["p", "b", "k"]);
            Syl.S(["sn"], ["oo"], ["g", "b"]);
            Syl.S(["sm"], ["o"], ["ll"]);

            Syl.S(["bl"], ["oi"], ["nk", "k", "b", "g"]);
            Syl.S(["bl"], ["o", "u"], ["nk", "p", "mp", "mp"]);
            Syl.S(["bl"], ["ir", "er", "aa", "ar", "or", "oo", "ee"], ["k", "b", "g", "p", "t"]);
            Syl.S(["bl"], ["a"], ["b", "g", "p", "t"]);

            Syl.S(["spl"], ["oi"], ["nk", "k", "b", "g"]);
            Syl.S(["spl"], ["o"], ["nk", "p", "b", "g", "nt", "mp"]);
            Syl.S(["spl"], ["a"], ["nk"]);
            Syl.S(["spl"], ["or"], ["k", "p", "b", "g"]);
            Syl.S(["spl"], ["ar", "er", "ir", "a", "or", "o", "oo"], ["k", "p", "b", "g", "d"]);

            Syl.S(vowels, ["k", "nk", "nt", "mp", "nt"], ["le", "len", "en", "ie", "ly"]);
            Syl.S(vowels, ["l", "ll", "lt", "lk"], ["en", "ie", "y"]);
            Syl.S(["*"], ["le", "en", "ie", "ly"], consonants);


            var vshort = vowels.Where(x => x.Length == 1);
            Syl.S(vshort, ["b", "t", "p", "g", "d"], ["le", "len", "en", "ly"]);
            Syl.S(["*"], ["le", "len", "en", "ly"], consonants);

            Syl.S(vshort, ["b"], ["ble", "blen", "ben", "bly"]);
            Syl.S(["*"], ["ble", "blen", "ben", "bly"], consonants);

            Syl.S(vshort, ["p"], ["ple", "plen", "pen", "ply"]);
            Syl.S(["*"], ["ple", "plen", "pen", "ply"], consonants);

            Syl.S(vshort, ["g"], ["gle", "glen", "gen", "gly"]);
            Syl.S(["*"], ["gle", "glen", "gen", "gly"], consonants);

            Syl.S(vshort, ["t"], ["tle", "tlen", "ten", "tly"]);
            Syl.S(["*"], ["tle", "tlen", "ten", "tly"], consonants);



            //clean all chains that end "r" and start "n"
            foreach (var s in Syl.map)
            {
                if (s.Value.t.EndsWith("r"))
                {
                    foreach(var k in s.Value.chains)
                    {
                        k.Value.RemoveWhere(x => x.t.StartsWith("n"));
                    }
                }

            }

        }

        static int n = 0;

        public static string Generate(int reps = 1)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var s in GenerateSyls(reps))
            {
                sb.Append(s);
            }
            return sb.ToString();
        }

        public static HashSet<string> GenerateAll()
        {
            HashSet<string> res = new();
            List<Syl> pres = new List<Syl>();
            List<Syl> mids = new List<Syl>();
            List<Syl> ends = new List<Syl>();
            foreach (var k in Syl.map)
            {
                if (k.Value.chains.ContainsKey(Syl.map["-"])) pres.Add(k.Value);
            }
            if (mids.Count < 0) mids.Add(Syl.map["b"]);

            foreach (var p in pres)
            {

                //now fill the mids list
                foreach (var k in p.chains.Where(x => x.Key.t.Equals("-")))
                {
                    mids.AddRange(k.Value);
                }
                if (mids.Count < 0) mids.Add(Syl.map["oi"]);
                foreach (var m in mids)
                {
                    foreach (var k in m.chains.Where(x => x.Key.t.Equals(p.t) || x.Key.t.Equals("*")))
                        ends.AddRange(k.Value);

                    if (ends.Count == 0) ends.Add(new Syl() { t = "j" });
                    foreach(var e in ends)
                    {
                        res.Add(p.t + m.t + e.t);
                    }
                }
            }
            return res;
        }

        public static List<Syl> GenerateSyls(int reps = 1)
        {
            reps -= 1;
            //generate a random thing
            Random r = new Random((int)DateTime.UtcNow.Ticks ^ Interlocked.Increment(ref n) ^ 298367592);
            List<Syl> pres = new List<Syl>();
            List<Syl> mids = new List<Syl>();
            List<Syl> ends = new List<Syl>();
            foreach(var k in Syl.map)
            {
                if (k.Value.chains.ContainsKey(Syl.map["-"])) pres.Add(k.Value);
            }
            if (mids.Count < 0) mids.Add(Syl.map["b"]);

            var p = pres[r.Next(pres.Count)];
            //now fill the mids list
            foreach (var k in p.chains.Where(x => x.Key.t.Equals("-")))
            {
                mids.AddRange(k.Value);
            }
            if (mids.Count < 0) mids.Add(Syl.map["oi"]);

            var m = mids[r.Next(mids.Count)];
            foreach (var k in m.chains.Where(x => x.Key.t.Equals(p.t) || x.Key.t.Equals("*")))
                ends.AddRange(k.Value);

            if (ends.Count == 0) ends.Add(new Syl() { t = "g" });
            var e = ends[r.Next(ends.Count)];

            List<Syl> res = [p, m, e];
            if (reps > 0)
            {
                var next = GenerateSyls(reps);
                //now get the last thing
                List<Syl> joiner = new List<Syl>();
                foreach(var k in res[res.Count - 1].chains.Where(x => x.Key.t.Equals(res[res.Count - 2].t)))
                {
                    joiner.AddRange(k.Value);
                }

                if (joiner.Count == 0) joiner.Add(new Syl() { t = "le" });
                var n = joiner[r.Next(joiner.Count)];
                res.Add(n);
                res.AddRange(next);
            }


            return res;
        }


        public static string ToGeneratorMapping()
        {
            //every syllable is dumped as prefix/children in an array
            StringBuilder sb = new StringBuilder();
            foreach(var k in Syl.map)
            {
                sb.AppendLine(k.Value.t + ": {");
                foreach (var tt in k.Value.chains)
                {
                    string s = "\t[" + tt.Key + "=";
                    foreach(Syl suf in tt.Value)
                    {
                        s += suf.t + ",";
                    }
                    s = s.Remove(s.Length - 1, 1);
                    s += "],";
                    sb.AppendLine(s);
                }
                sb.Append("};\n");
            }
            return sb.ToString();
        }

    }
}
