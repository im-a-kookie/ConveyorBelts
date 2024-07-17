using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;

namespace ConveyorEngine.Util
{
    /// <summary>
    /// Implements an indexable collection using a tree of arrays.
    /// Should be significantly faster than hashtable (around 3x)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TreeArray<T> : IEnumerable<T>
    {
        //the array divides into 4 repeatedly from the root
        //then the indices feed forward
        public int Scale = 5;
        public int Step = 5;
        public int Bound;
        public int Mask;
        public TreeArray()
        {
            Bound = Scale;
            while (Bound < 32 - Step) Bound += Step;
            Mask = (1 << Step) - 1;
        }
        //so basically the QuadTree expands outwardly until it gets to the thingimy thingy
        object array;
        public T[,] check(int x, int y)
        {
            if (array == null) array = new object[1 << Step, 1 << Step];
            object n = array;
            //find the depth that we need and convolute the x/y sequentially
            for (int i = Bound - Step; i >= Scale + Step; i -= Step)
            {
                //So now we can get the outermost coord for our current index
                var _x = (x >> i) & Mask;
                var _y = (y >> i) & Mask;
                //walk into n and do the stuff
                if (((object[,])n)[_x, _y] == null) lock(n)
                    ((object[,])n)[_x, _y] = new object[1 << Step, 1 << Step];
                n = ((object[,])n)[_x, _y];
            }
            //now we have object[,] of T[scale]
            x = (x >> Scale) & Mask;
            y = (y >> Scale) & Mask;
            if (((object[,])n)[x, y] is not T[,]) lock(n)
            {
                ((object[,])n)[x, y] = new T[1 << Scale, 1 << Scale];
            }
            return (T[,])((object[,])n)[x, y];
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<object> list = new List<object>();
            list.Add(array);

            while (list.Count > 0)
            {
                object n = list[0];
                list.RemoveAt(0);
                if (n is object[,] o)
                {
                    for (int i = 0; i < 1 << Step; i++)
                    {
                        for (int j = 0; j < 1 << Step; j++)
                        {
                            if (o[i, j] is object[,] oo)
                            {
                                list.Add(oo);
                            }
                            else if (o[i,j] is T[,] t)
                            {
                                for(int x = 0; x < 1 << Scale; x++)
                                {
                                    for(int y = 0; y < 1 << Scale; y++)
                                    {
                                        yield return t[x, y];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public T this[int x, int y]
        {

            get => check(x, y)[x & ((1 << Scale) - 1), y & ((1 << Scale) - 1)];
            set => check(x, y)[x & ((1 << Scale) - 1), y & ((1 << Scale) - 1)] = value;
        }


    }
}
