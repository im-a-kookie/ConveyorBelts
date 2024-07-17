using ConveyorEngine.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Tests
{
    internal class DequeTests
    {
        [TestSection(Section = "Deque")]
        private class AddTakeEnds : TestUnit
        {
            public override bool Validate()
            {
                var qq = new Deque<int>();
                for (int i = 0; i < 5000; i++) qq.AddFirst(i);
                bool success = true;
                for (int i = 0; i < 5000; i++) if (qq.TakeLast() != i) success = false;

                qq.Clear();
                for (int i = 0; i < 5000; i++) qq.AddLast(i);
                for (int i = 0; i < 5000; i++) if (qq.TakeFirst() != i) success = false;

                return success;
            }
        }
        [TestSection(Section = "Deque")]
        private class Indexing : TestUnit
        {
            public override bool Validate()
            {
                var qq = new Deque<int>();
                for (int i = 9; i >= 0; i--) qq.AddFirst(i);
                for (int i = 10; i < 20; i++) qq.AddLast(i);
                bool success = true;
                for (int i = 0; i < 20; i++) if (qq[i] != i) success = false;
                
                return success;
            }
        }
        [TestSection(Section = "Deque")]
        private class Insertion : TestUnit
        {
            public override bool Validate()
            {
                var qq = new Deque<int>();
                qq.AddFirst(0);
                for (int i = 0; i < 10000; i++) qq.Insert(i, i);
                bool success = true;
                for (int i = 0; i < 10000; i++) if (qq.TakeFirst() != i) success = false;
                return success;
            }
        }
        [TestSection(Section = "Deque")]
        private class RangeInsertion : TestUnit
        {
            public override bool Validate()
            {
                var qq = new Deque<int>();
                qq.AddRangeFirst([0, 1, 2, 3, 4]);
                qq.AddRangeLast([5, 6, 7, 8, 9]);
                for (int i = 0; i < 10; i++) if (qq.TakeFirst() != i) return false;
                return true;
            }
        }
        [TestSection(Section = "Deque")]
        private class RangeRemoval : TestUnit
        {
            public override bool Validate()
            {
                var qq = new Deque<int>();
                qq.AddRangeFirst([0, 1, 2, 3, 4]);
                qq.AddRangeLast([5, 6, 7, 8, 9]);
                qq.RemoveRange(2, 6);
                int n = 0;
                while (qq.TryTakeFirst(out int t)) n += t;
                return n == 18;

            }
        }


    }
}
