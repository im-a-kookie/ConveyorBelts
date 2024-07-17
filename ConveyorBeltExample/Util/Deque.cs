using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{
    [Serializable]
    [ComVisible(false)]
    public class Deque<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        // fields
        // The collection architecture is broadly as follows;
        //                             head[0]                    tail[0]
        //[  count | <-   head  ->  | deleted   ][   deleted | <-  tail  ->  | count  ] 
        //   [queue.first]          |=>        yeet        <=|          [queue.last]
        //                             This section is yeet
        //When an item is AddFirst/Last then we Add it to the head/tail respectively (List.Add)
        //When an item is TakeFirst/Last, then we take it from the head/tail respectively
        //If we want to Take from an empty list, then we decrement the deletion counter of the opposite list
        //and then take that item
        //Since we only remove to minize list rebounding, this is generally very inexpensive
        //
        /// <summary>
        /// A list representing the items from the head of the queue
        /// </summary>
        public List<T> head;
        /// <summary>
        /// A list representing the items from the tail of the queue
        /// </summary>
        public List<T> tail;
        /// <summary>
        /// The number of items deleted from the head
        /// </summary>
        public int headDeleted;
        /// <summary>
        /// The number of items deleted from the tail
        /// </summary>
        public int tailDeleted;

        // properties

        public int Capacity
        {
            get { return head.Capacity + tail.Capacity; }
        }

        public int Count
        {
            get { return head.Count + tail.Count - headDeleted - tailDeleted; }
        }

        public IEnumerable<T> Reversed
        {
            get
            {
                if (tail.Count - tailDeleted > 0)
                {
                    for (int i = tail.Count - 1; i >= tailDeleted; i--) yield return tail[i];
                }

                if (head.Count - headDeleted > 0)
                {
                    for (int i = headDeleted; i < head.Count; i++) yield return head[i];
                }
            }
        }

        public Deque()
        {
            head = new List<T>();
            tail = new List<T>();
        }

        public Deque(int capacity)
        {
            if (capacity < 0) throw new ArgumentException("Capacity cannot be negative");
            int temp = capacity >> 1;
            int temp2 = capacity - temp;
            head = new List<T>(temp);
            tail = new List<T>(temp2);
        }

        public Deque(IEnumerable<T> backCollection) : this(backCollection, null)
        {
        }

        public Deque(IEnumerable<T> backCollection, IEnumerable<T> frontCollection)
        {
            if (backCollection == null && frontCollection == null) throw new ArgumentException("Collections cannot both be null");
            head = new();
            tail = new();

            if (backCollection != null)
            {
                foreach (T item in backCollection) tail.Add(item);
            }

            if (frontCollection != null)
            {
                foreach (T item in frontCollection) head.Add(item);
            }
        }

        /// <summary>
        /// Add item to the start of this queue
        /// </summary>
        /// <param name="item"></param>
        public void AddFirst(T item)
        {
            //Check to skip list resizing
            if (headDeleted > 0 && head.Count == head.Capacity)
            {
                head.RemoveRange(0, headDeleted);
                headDeleted = 0;
            }
            head.Add(item);
        }

        /// <summary>
        /// Add item at the end of this queue
        /// </summary>
        /// <param name="item"></param>
        public void AddLast(T item)
        {
            //Check to skip list resizing
            if (tailDeleted > 0 && tail.Count == tail.Capacity)
            {
                tail.RemoveRange(0, tailDeleted);
                tailDeleted = 0;
            }
            tail.Add(item);
        }

        /// <summary>
        /// Add all of the above items to this queue
        /// </summary>
        /// <param name="range"></param>
        public void AddRangeFirst(IEnumerable<T> range)
        {
            if (range == null) return;
            //Check to skip list resizing
            if (headDeleted > 0 && head.Count == head.Capacity)
            {
                head.RemoveRange(0, headDeleted);
                headDeleted = 0;
            }
            head.AddRange(range.Reverse());
        }

        public void AddRangeLast(IEnumerable<T> range)
        {
            if (range == null) return;
            //Check to skip list resizing
            if (tailDeleted > 0 && tail.Count == tail.Capacity)
            {
                tail.RemoveRange(0, tailDeleted);
                tailDeleted = 0;
            }
            tail.AddRange(range);
        }

        public void Clear()
        {
            head.Clear();
            tail.Clear();
            headDeleted = 0;
            tailDeleted = 0;
        }

        public bool Contains(T item)
        {
            for (int i = headDeleted; i < head.Count; i++)
            {
                if (Object.Equals(head[i], item)) return true;
            }

            for (int i = tailDeleted; i < tail.Count; i++)
            {
                if (Object.Equals(tail[i], item)) return true;
            }

            return false;
        }

        public bool Remove(T item)
        {
            for (int i = headDeleted; i < head.Count; i++)
            {
                if (Object.Equals(head[i], item))
                {
                    //convert i back to a correct index and TakeArbitrary
                    TakeArbitrary(head.Count - i - 1);
                    return true;
                }
            }

            for (int i = tailDeleted; i < tail.Count; i++)
            {
                if (Object.Equals(tail[i], item))
                {
                    TakeArbitrary(Count - i);
                    return true;
                }
            }
            return false;
        }



        public void CopyTo(T[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (array.Length < index + this.Count) throw new ArgumentException("Index is invalid");
            int i = index;

            foreach (T item in this)
            {
                array[i++] = item;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (head.Count - headDeleted > 0)
            {
                for (int i = head.Count - 1; i >= headDeleted; i--) yield return head[i];
            }

            if (tail.Count - tailDeleted > 0)
            {
                for (int i = tailDeleted; i < tail.Count; i++) yield return tail[i];
            }
        }

        /// <summary>
        /// Looks at the first item in the queue
        /// </summary>
        /// <returns>The item at the front</returns>
        /// <exception cref="InvalidOperationException">Indicates empty collection</exception>
        public T PeekFirst()
        {
            if (head.Count > headDeleted)
            {
                return head[^1];
            }
            else if (tail.Count > tailDeleted)
            {
                return tail[tailDeleted];
            }
            else
            {
                throw new InvalidOperationException("Can't inspect empty collection");
            }
        }

        /// <summary>
        /// Looks at the last item in the queue
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T PeekLast()
        {
            if (tail.Count > tailDeleted)
            {
                return tail[^1];
            }
            else if (head.Count > headDeleted)
            {
                return head[headDeleted];
            }
            else
            {
                throw new InvalidOperationException("Can't inspect empty collection");
            }
        }

        /// <summary>
        /// Retrieves the item at the Nth index of this collection
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public T this[int n]
        {
            get
            {
                //Get the transformed index
                int index = head.Count - 1 - n;
                if (index < headDeleted)
                {
                    //We are not in the head section so we need to go to the tail
                    //and offset by the number of items that are in the head
                    index = n - head.Count + headDeleted + tailDeleted;
                    if (index >= tail.Count) throw new IndexOutOfRangeException();
                    return tail[index];
                }
                else
                {
                    //we are in the tail, so it's simple
                    if (head.Count == 0) throw new IndexOutOfRangeException();
                    return head[index];
                }
            }
            set //same as get but sets instead
            {
                int index = head.Count - 1 - n;
                if (index < headDeleted)
                {
                    index = n - head.Count + headDeleted + tailDeleted;
                    if (index >= tail.Count) throw new IndexOutOfRangeException();
                    tail[index] = value;
                }
                else
                {
                    if (head.Count == 0) throw new IndexOutOfRangeException();
                    head[index] = value;
                }
            }
        }

        /// <summary>
        /// Takes an item from an arbitrary index in the collection.
        /// <para>Use first/last where possible.</para>
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">Indicates index out of collection range</exception>
        /// <exception cref="InvalidOperationException">Indicates collection is probably empty</exception>
        public T TakeArbitrary(int n)
        {
            //surprisingly simple
            //We use the same method as the indexer initially to find the place
            int index = head.Count - 1 - n;
            if (index < headDeleted)
            {
                index = n - head.Count + headDeleted + tailDeleted;
                if (index >= tail.Count) throw new IndexOutOfRangeException();
                //so the index is tail[index]
                var t = tail[index];
                tail.RemoveAt(index); //and we simply remove it from the list
                //it's pretty simple really
                return t;
            }
            else
            {
                if (head.Count == 0) throw new IndexOutOfRangeException();
                var t = head[index];
                head.RemoveAt(index); //and we simply remove it from the list
                return t;
            }

            throw new InvalidOperationException("Something is very very wrong with your Deque, Kookie.");
        }

        /// <summary>
        /// Takes an item from an arbitrary index in the collection.
        /// <para>Use first/last where possible.</para>
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">Indicates index out of collection range</exception>
        /// <exception cref="InvalidOperationException">Indicates collection is probably empty</exception>
        public void RemoveRange(int start, int count)
        {
            //surprisingly simple
            //We use the same method as the indexer initially to find the place
            int index = head.Count - 1 - start;
            if (index < headDeleted)
            {
                index = start - head.Count + headDeleted + tailDeleted;
                if (index >= tail.Count) throw new IndexOutOfRangeException();
                //so the index is tail[index]
                //and we must remove from here to index+count
                //but remember that it will be cut off at tailDeleted
                //so the range is: index to min(count-1, index+count)
                tail.RemoveRange(index, int.Min(tail.Count - index, count));
                return;
                //it's pretty simple really
            }
            else
            {
                if (head.Count == 0) throw new IndexOutOfRangeException();
                var t = head[index];
                //First, see if we need to delete from the tail also
                int d = index - headDeleted + 1;
                head.RemoveRange(index - d + 1, d);
                if (count - d > 0) RemoveRange(start, count - d);
                return;
            }
        }


        /// <summary>
        /// Inserts an item into the collection at the given index
        /// </summary>
        /// <param name="n"></param>
        /// <param name="value"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Insert(int n, T value)
        {
            //surprisingly simple
            //We use the same method as the indexer initially to find the place
            int index = head.Count - 1 - n;
            if (index < headDeleted)
            {
                index = n - head.Count + headDeleted + tailDeleted;
                if (index >= tail.Count) throw new IndexOutOfRangeException();
                tail.Insert(index, value);
                return;
            }
            else
            {
                if (head.Count == 0) throw new IndexOutOfRangeException();
                if (index + 1 >= head.Count) head.Add(value); 
                else head.Insert(index + 1, value);
                return;
            }
            throw new InvalidOperationException("Something is very very wrong with your Deque, Kookie.");
        }

        /// <summary>
        /// Inserts an item into the collection at the given index
        /// </summary>
        /// <param name="n"></param>
        /// <param name="value"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void InsertRange(int n, IEnumerable<T> value)
        {
            //surprisingly simple
            //We use the same method as the indexer initially to find the place
            int index = head.Count - 1 - n;
            if (index < headDeleted)
            {
                index = n - head.Count + headDeleted + tailDeleted;
                if (index >= tail.Count) throw new IndexOutOfRangeException();
                tail.InsertRange(index, value);
                return;
            }
            else
            {
                if (head.Count == 0) throw new IndexOutOfRangeException();
                if (index + 1 >= head.Count) head.AddRange(value.Reverse());
                else head.InsertRange(index + 1, value.Reverse());
                return;
            }
            throw new InvalidOperationException("Something is very very wrong with your Deque, Kookie.");
        }


        /// <summary>
        /// Takes the first item in the collection
        /// </summary>
        /// <returns>The first item</returns>
        /// <exception cref="InvalidOperationException">Indicates empty collection</exception>
        public T TakeFirst()
        {
            //Simple enough
            T result;
            //If we have valid items in the head then take one from there
            if (head.Count > headDeleted)
            {
                result = head[^1];
                head.RemoveAt(head.Count - 1);
            }
            //Otherwise we are in the tail, so take one from there
            else if (tail.Count > tailDeleted)
            {
                result = tail[tailDeleted];
                tailDeleted++;
            }
            else
            {
                throw new InvalidOperationException("Can't retrieve from empty collection");
            }

            return result;
        }

        /// <summary>
        /// Tries to take the first item in the collection
        /// </summary>
        /// <param name="t"></param>
        /// <returns>True on success, false on failure</returns>
        public bool TryTakeFirst(out T t)
        {
            if (Count == 0)
            {
                t = default(T);
                return false;
            }
            t = TakeFirst();
            return true;
        }

        /// <summary>
        /// Takes the last item in the collection.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Indicates empty collection</exception>
        public T TakeLast()
        {
            T result;
            if (tail.Count > tailDeleted)
            {
                result = tail[^1];
                tail.RemoveAt(tail.Count - 1);
            }
            else if (head.Count > headDeleted)
            {
                result = head[headDeleted];
                headDeleted++;
            }
            else
            {
                throw new InvalidOperationException("Can't retrieve from empty collection");
            }

            return result;
        }


        /// <summary>
        /// Tries to take the last item in the collection.
        /// </summary>
        /// <param name="t">Contains the item</param>
        /// <returns>True on success, false on failure</returns>
        public bool TryTakeLast(out T t)
        {
            if (Count == 0)
            {
                t = default(T);
                return false;
            }
            t = TakeLast();
            return true;
        }

        /// <summary>
        /// Reverses teh collection
        /// </summary>
        public void Reverse()
        {
            (tail, head) = (head, tail);
            (tailDeleted, headDeleted) = (headDeleted, tailDeleted);
        }

        /// <summary>
        /// Converts us to an array
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            if (this.Count == 0) return new T[0];
            T[] result = new T[this.Count];
            this.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Trims the excess from this collection
        /// </summary>
        public void TrimExcess()
        {
            if (headDeleted > 0)
            {
                head.RemoveRange(0, headDeleted);
                headDeleted = 0;
            }

            if (tailDeleted > 0)
            {
                tail.RemoveRange(0, tailDeleted);
                tailDeleted = 0;
            }

            head.TrimExcess();
            tail.TrimExcess();
        }

        /// <summary>
        /// Tries to peek the first item in this collection
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryPeekFirst(out T item)
        {
            if (Count <= 0)
            {
                item = default(T);
                return false;
            }
            item = this.PeekFirst();
            return true;
        }

        /// <summary>
        /// Tries to peek the last item in this collection
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryPeekLast(out T item)
        {
            if (Count <= 0)
            {
                item = default(T);
                return false;
                
            }
            item = this.PeekLast();
            return true;
        }


        // explicit property implementations


        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return (ICollection)this; }
        }

        // explicit method implementations

        void ICollection.CopyTo(Array array, int index)
        {
            this.CopyTo((T[])array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
