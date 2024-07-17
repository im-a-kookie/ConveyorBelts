using ConveyorBeltExample.Graphing;
using ConveyorEngine.Util;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{

    /// <summary>
    /// A simple indexable pool of items.
    /// Note that items must be explicitly disposed
    /// Otherwise the pool will leak indices
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IndexedPool<T> : IPool where T : Indexable
    {
        private bool Lock = false;

        public bool IsDirty = false;

        public T[] _cached = null;
        public int _assigned = 0;
        public int rebuild_count = 0;

        public bool IsLocked => Lock;
        public void LockCollection() => Lock = true;
        public void UnlockCollection() => Lock = false;

        public T[] Items;
        private ConcurrentQueue<int> indices = new();

        /// <summary>
        /// Trigger a rebuild of the item cache, ensuring good indexation etc
        /// </summary>
        public void RebuildCache()
        {
            if (_cached == null) _cached = ArrayPool<T>.Shared.Rent(64);
            _assigned = 0;
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i] != null && Items[i].Index > 0)
                {
                    if (_assigned >= _cached.Length)
                    {
                        T[] _new = ArrayPool<T>.Shared.Rent(_cached.Length << 1);
                        Array.Copy(_cached, _new, _cached.Length);
                        ArrayPool<T>.Shared.Return(_cached);
                        _cached = _new;
                    }
                    _cached[_assigned] = Items[i];
                    _assigned++;
                }
            }
            rebuild_count = (rebuild_count + 1) & 0x7FFFFFFF;
            IsDirty = false;
        }

        public int excluder = 0;
        /// <summary>
        /// Creates an indexed pool with the given initial size
        /// </summary>
        /// <param name="size"></param>
        public IndexedPool(int size = 100)
        {
            Items = ArrayPool<T>.Shared.Rent(size);
            //we never add 0 to the pool
            for (int i = 1; i < Items.Length; i++) indices.Enqueue(i);
        }

        /// <summary>
        /// Gets a free index in the pool
        /// </summary>
        /// <returns></returns>
        public int GetFreeIndex()
        {
            int val = -1;
            while (!indices.TryDequeue(out val))
            {
                lock(this)
                { //double check
                    if (!indices.TryDequeue(out val))
                    {
                        //There was no free index, so expand the pool

                        int k = Items.Length;
                        T[] _new = ArrayPool<T>.Shared.Rent((k * 3) / 2);
                        Array.Copy(Items, 0, _new, 0, Items.Length);
                        for (int i = 0; i < Items.Length; i++) _new[i] = Items[i];
                        ArrayPool<T>.Shared.Return(Items);
                        Items = _new;
                        //now everything is copied, we can enqueue the new indices
                        for (int i = k; i < _new.Length; i++) indices.Enqueue(i);
                    }
                }
            }
            return val;
        }

        /// <summary>
        /// Assigns the given item to this pool
        /// </summary>
        /// <param name="item"></param>
        public void Assign(Indexable item)
        {
            if (item is T t)
            {
                int val = GetFreeIndex();
                item.Index = val;
                Items[val] = t;
                item.parent = this;
                IsDirty = true;
            }
        }

        

        public void Free(Indexable i)
        {
            if (Lock) throw new Exception("Attempting to modify freed IPool!!");
            int n = i.Index;
            if (i.Index <= 0) return;
            //set the index so it knows it's been reallocated
            i.Index = -1;
            //Dereferencing is good, but may race on Items during resize
            Items[n] = default(T);
            //Queue the freed slot
            indices.Enqueue(n);
            IsDirty = true;
        }

        public T this[int i]
        {
            get { return Items[i]; }
        }

    }

    public interface IPool
    {
        public void Free(Indexable i);
        public void Assign(Indexable i);
        public bool IsLocked() => false;
    }

    public interface Indexable : IDisposable
    { 
        
        public int Index { get; set; }
        public IPool parent { get; set; }
        public void Free() => parent.Free(this);
        public void Assign() => parent.Assign(this);
        void IDisposable.Dispose() { parent.Free(this); }

    }

}
