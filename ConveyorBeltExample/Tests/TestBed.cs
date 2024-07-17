using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Tests
{

    public class TestBed
    {
        public static string Tab(int t)
        {
            string s = "";
            for (int i = 0; i < t; i++)
            {
                s += "\t";
            }
            return s;
        }

        public static string ToSpanString(TimeSpan t)
        {
            if (t.TotalMinutes < 1)
            {
                if (t.TotalSeconds < 1)
                {
                    //if (t.TotalMilliseconds < 1)
                    //{
                        //return Math.Round(1000 * t.TotalMicroseconds / 1000d) + "us";
                    //}
                    return Math.Round(1000 * t.TotalMilliseconds) / 1000d + "ms";
                }
                return Math.Round(1000 * t.TotalSeconds) / 1000d + "s";
            }
            int m = (int)Math.Floor(t.TotalMinutes);
            int s = (int)Math.Floor(t.TotalSeconds - 60 * m);
            int ms = (int)Math.Floor(t.TotalSeconds - 60 * m - s) * 10000;
            string mms = ms.ToString();
            while (mms.Length < 4) mms = "0" + mms;
            return m + "m, " + s + "." + mms + "s";
        }


        public static void GetAndPerformTestables()
        {

            var type = typeof(TestUnit);
            Stopwatch t_0 = Stopwatch.StartNew();

            Debug.WriteLine("<=======================================================>");
            Debug.WriteLine("<====                 Running Tests!                ====>");
            Debug.WriteLine("<=======================================================>");
            Debug.WriteLine("");
            Debug.WriteLine("");

            int total = 0;
            int succeeeded = 0;
            int failed = 0;
            List<Type> failures = new List<Type>();
            Dictionary<string, List<TestUnit>> sections = new Dictionary<string, List<TestUnit>>();

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var atypes = a.GetTypes().Where(p => type.IsAssignableFrom(p) && p != type);
                if (atypes.Count() > 0)
                {

                    Debug.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                    Debug.WriteLine("Assembly:    " + a.GetName());
                    Debug.WriteLine("Tests Found: " + atypes.Count());
                    Debug.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                    Debug.WriteLine("Generating Tests...");

                    Stopwatch t_1 = Stopwatch.StartNew();
                    foreach (Type t in atypes)
                    {
                        ++total;
                        try
                        {
                            TestUnit runner = (TestUnit)Activator.CreateInstance(t);
                            //look for attributes
                            var attribs = (TestSection[])t.GetCustomAttributes(typeof(TestSection), true);
                            string section = "";
                            if (attribs.Length > 0)
                            {
                                var myAttribute = attribs[0];
                                section = myAttribute.Section;
                            }
                            if (section == null || section.Trim().Length <= 0) section = "";
                            if (!sections.ContainsKey(section)) sections.Add(section, new());
                            sections[section].Add(runner);
                        }
                        catch
                        {
                            Debug.WriteLine(Tab(1) + " * Failure Instantiating: " + t.FullName);
                            ++failed;
                            failures.Add(t);
                        }
                    }

                    t_1.Stop();
                    Debug.WriteLine("Done in " + ToSpanString(t_1.Elapsed));
                    Debug.WriteLine("Performing...");
                    Debug.WriteLine("-----------------------------------------------------");
                    Debug.WriteLine("");

                    List<string> ordered_sections = new List<string>();
                    ordered_sections.AddRange(sections.Keys);
                    ordered_sections.Sort();
                    foreach(string s in ordered_sections)
                    {
                        var l = sections[s];
                        if (l == null || l.Count == 0) continue;
                        Debug.WriteLine(s.Length == 0 ? "Generic Tests: " : s + ":");
                        foreach (var t in l)
                        {
                            try
                            {
                                Stopwatch t_2 = Stopwatch.StartNew();
                                var vresult = t.Validate();
                                t_2.Stop();
                                if (!vresult) throw new Exception("Validation failure!");
                                ++succeeeded;
                                string ss = t.name ?? t.GetType().Name + " Passed! ";
                                ss = ss.PadRight(27);
                                Debug.WriteLine(Tab(1) + ss + Math.Round(t_2.Elapsed.TotalMilliseconds * 1000d) / 1000d + "ms");
                            }
                            catch (Exception ex)
                            {
                                ++failed;
                                failures.Add(t.GetType());
                                Debug.WriteLine(Tab(1) + " * Failure Testing " + t.name ?? t.GetType().Name);
                                Debug.WriteLine(Tab(1) + " * " + ex.Message);
                                var st = ex.StackTrace.Split('\n');
                                for (int i = 0; i < 4 && i > st.Length; i++) Debug.WriteLine(Tab(2) + " * " + st[i]);
                                Debug.WriteLine("");

                            }
                        }
                        Debug.WriteLine("-----------------------------------------------------");
                    }

                }
            }

            Debug.WriteLine("\n<=======================================================>");
            Debug.WriteLine("All Tests Completed.");

            t_0.Stop();
           
            Debug.WriteLine("Succeeded: " + succeeeded + "/" + total);
            if (failed != 0 || failures.Count > 0)
            {
                Debug.WriteLine("");
                Debug.WriteLine("Failures: " + failed);
                foreach(var t in failures) Debug.WriteLine(" * " + t.FullName);
            }
            t_0.Stop();
        }
    }

    public enum ResultFlag
    {
        SUCCESS, FAILURE, FAILURE_NO_INSTANCE
    }

    public class Result
    {
        ResultFlag success;
        TimeSpan time;
        Type t;
        public Result(Type t, bool success, TimeSpan time)
        {
            this.t = t;
            this.success = success ? ResultFlag.SUCCESS : ResultFlag.FAILURE;
            this.time = time;
        }
        public Result(Type t, ResultFlag flag, TimeSpan time)
        {
            this.t = t;
            this.success = flag;
            this.time = time;
        }

    }

    // This defaults to Inherited = true.
    [AttributeUsage(AttributeTargets.Class)]
    public class TestSection : Attribute
    {
        public string Section { get; set; }
    }


    public abstract class TestUnit
    {
        public string section = "";
        public string name = null;
        public virtual bool Validate() => true;
        public void EnterSection(string section)
        {
            this.section = section;
        }
    }


}
