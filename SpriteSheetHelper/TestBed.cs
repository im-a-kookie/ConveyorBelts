using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SpriteSheetHelper
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

        public static void GetAndPerformTestables()
        {

            var type = typeof(Testable);

            Debug.WriteLine("<============================================>");
            Debug.WriteLine("<====           Running Tests!           ====>");
            Debug.WriteLine("<============================================>");
            Debug.WriteLine("");
            Debug.WriteLine("");

            int total = 0;
            int succeeeded = 0;
            int failed = 0;
            List<Type> failures = new List<Type>();

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var atypes = a.GetTypes().Where(p => type.IsAssignableFrom(p) && p != typeof(Testable));
                if (atypes.Count() > 0)
                {

                    Debug.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                    Debug.WriteLine("Assembly: " + a.FullName);
                    Debug.WriteLine("Testable: " + atypes.Count());
                    Debug.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    Debug.WriteLine("");
                    foreach (Type t in atypes)
                    {
                        ++total;
                        Debug.WriteLine("Testing " + t.FullName);
                        var method = t.GetMethod("generate");
                        var obj = (Testable?)Activator.CreateInstance(t);
                        try
                        {
                            var tests = (Dictionary<string, Test>)method.Invoke(obj, null);
                            foreach (var test in tests)
                            {
                                try
                                {
                                    Debug.WriteLine(Tab(1) + test.Key);
                                    var result = test.Value.perform();
                                    Debug.WriteLine(Tab(2) + "Expect: " + test.Value.Expect);
                                    Debug.WriteLine(Tab(2) + "Result: " + result.result);
                                    Debug.WriteLine(Tab(2) + "Success: " + result.success);
                                    Debug.WriteLine("");
                                    Debug.WriteLine(Tab(2) + "Metrics:");
                                    Debug.WriteLine(Tab(3) + "Setup:      " + Math.Round(result.instantiation_time * 100) / 100f + "ms");
                                    Debug.WriteLine(Tab(3) + "Time:       " + Math.Round(result.execution_time * 100) / 100f + "ms");
                                    Debug.WriteLine(Tab(3) + "Iterations: " + test.Value.Iterations);
                                    if (result.success) ++succeeeded;
                                    else
                                    {
                                        ++failed;
                                        failures.Add(t);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ++failed;
                                    Debug.WriteLine(Tab(1) + "* Error performing tests!");
                                    Debug.WriteLine(Tab(1) + "* " + ex.Message);
                                    var st = ex.StackTrace.Split('\n');
                                    for (int i = 0; i < 4 && i > st.Length; i++) Debug.WriteLine(Tab(1) + "* " + st[i]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ++failed;
                            Debug.WriteLine(Tab(1) + "* Could not generate tests!");
                            Debug.WriteLine(Tab(1) + "* " + ex.Message);
                            var st = ex.StackTrace.Split('\n');
                            for (int i = 0; i < 4 && i > st.Length; i++) Debug.WriteLine(Tab(1) + "* " + st[i]);

                        }
                        Debug.WriteLine("");
                        Debug.WriteLine("");

                    }
                }
            }
            Debug.WriteLine("<===============================================>");
            Debug.WriteLine("All Tests Completed.");
            Debug.WriteLine("Succeeded: " + succeeeded + "/" + total);
            if (failed != 0 || failures.Count > 0)
            {
                Debug.WriteLine("");
                Debug.WriteLine("Failures: " + failed);
                foreach (var t in failures) Debug.WriteLine(" * " + t.FullName);
            }
        }
    }

    public abstract class Test
    {
        public int Iterations { get; set; }
        public object Expect { get; set; }
        public abstract TestResult perform();
    }

    public class IntTest<T> : Test
    {
        Func<int, T, int> predicate;
        Func<int, T> build;
        public int expected { get => (int)Expect; set => Expect = value; }

        public IntTest(Func<int, T> build, Func<int, T, int> predicate, int expect, int iterations = 1000000)
        {
            this.build = build;
            this.predicate = predicate;
            this.expected = expect;
            this.Iterations = iterations;
        }
        public override TestResult perform()
        {
            T[] t = new T[Iterations];
            Stopwatch s = new Stopwatch();
            float instantiate = 0;
            s.Restart();
            for (int i = 0; i < Iterations; i++)
            {
                t[i] = build(i);
            }
            s.Stop();
            instantiate = (float)s.Elapsed.TotalMilliseconds;
            int val = 0;
            s.Restart();
            for (int i = 0; i < Iterations; i++)
            {
                val += predicate(i, t[i]);
            }
            s.Stop();
            return new TestResult(val == expected, val, instantiate, (float)s.Elapsed.TotalMilliseconds);
        }
    }

    public class DoubleTest<T> : Test
    {
        Func<int, double> pars;
        Func<double, T, int> predicate;
        Func<double, T> build;
        public int expected { get => (int)Expect; set => Expect = value; }

        public DoubleTest(Func<int, double> pars, Func<double, T> build, Func<double, T, int> predicate, int expect, int iterations = 1000000)
        {
            this.pars = pars;
            this.build = build;
            this.predicate = predicate;
            this.expected = expect;
            this.Iterations = iterations;
        }
        public override TestResult perform()
        {
            double[] parameters = new double[Iterations];
            for (int i = 0; i < Iterations; i++) parameters[i] = pars(i);
            
            T[] t = new T[Iterations];
            Stopwatch s = new Stopwatch();
            float instantiate = 0;
            s.Restart();
            for (int i = 0; i < Iterations; i++)
            {
                t[i] = build(parameters[i]);
            }
            s.Stop();
            instantiate = (float)s.Elapsed.TotalMilliseconds;
            int val = 0;
            s.Restart();
            for (int i = 0; i < Iterations; i++)
            {
                val += predicate(parameters[i], t[i]);
            }
            s.Stop();
            return new TestResult(val == expected, val, instantiate, (float)s.Elapsed.TotalMilliseconds);
        }
    }


    public class Int2Test<T> : Test
    {
        Func<int, int, T, int> predicate;
        Func<int, int, T> build;
        public Rectangle bounds;
        public int expected { get => (int)Expect; set => Expect = value; }

        public Int2Test(Func<int, int, T> build, Func<int, int, T, int> predicate, int expect, Rectangle bounds)
        {
            this.build = build;
            this.predicate = predicate;
            this.expected = expect;
            this.bounds = bounds;
            this.Iterations = bounds.Width * bounds.Height;
        }
        public override TestResult perform()
        {
            T[,] t = new T[bounds.Width, bounds.Height];
            Stopwatch s = new Stopwatch();
            float instantiate = 0;
            s.Restart();
            for (int x = 0; x < bounds.Width; x++)
            {
                for(int y = 0; y < bounds.Height; y++)
                {
                    t[x, y] = build(x + bounds.Left, y + bounds.Top);
                }
            }
            s.Stop();
            instantiate = (float)s.Elapsed.TotalMilliseconds;
            int val = 0;
            s.Restart();
            for (int x = 0; x < bounds.Width; x++)
            {
                for (int y = 0; y < bounds.Height; y++)
                {
                    val += predicate(x, y, t[x, y]);
                }
            }
            s.Stop();
            return new TestResult(val == expected, val, instantiate, (float)s.Elapsed.TotalMilliseconds);
        }
    }

    public class GenericTest<T, U> : Test
    {
        /// <summary>
        /// The predicate is run "iterations" times and returns a value.
        /// The value is them matched against the expected to generate a test result.
        /// The predicate is then looped and benchmarked
        /// </summary>
        Func<U, T, int> predicate;
        Func<U, T> build;
        Func<int, U> pars;
        public int expected { get => (int)Expect; set => Expect = value; }
        public GenericTest(Func<int, U> pars, Func<U, T> build, Func<U, T, int> predicate, int expect, int iterations = 1000000)
        {
            this.pars = pars;
            this.build = build;
            this.predicate = predicate;
            this.expected = expect;
            this.Iterations = iterations;
        }
        public override TestResult perform()
        {
            T[] t = new T[Iterations];
            U[] u = new U[Iterations];
            for (int i = 0; i < Iterations; i++) u[i] = pars(i);
            Stopwatch s = new Stopwatch();
            float instantiate = 0;
            s.Restart();
            for (int i = 0; i < Iterations; i++)
            {
                t[i] = build(u[i]);
            }
            s.Stop();
            instantiate = (float)s.Elapsed.TotalMilliseconds;
            int val = 0;
            s.Restart();
            for (int i = 0; i < Iterations; i++)
            {
                val += predicate(u[i], t[i]);
            }
            s.Stop();
            return new TestResult(val == expected, val, instantiate, (float)s.Elapsed.TotalMilliseconds);
        }
    }

    public struct TestResult
    {
        public bool success;
        public int result;
        public float instantiation_time;
        public float execution_time;

        public TestResult(bool success, int result, float instantiation_time, float execution_time)
        {
            this.success = success;
            this.result = result;
            this.instantiation_time = instantiation_time;
            this.execution_time = execution_time;
        }
    }

    public interface Testable
    {
        public Dictionary<string, Test> generate();
    }
}
