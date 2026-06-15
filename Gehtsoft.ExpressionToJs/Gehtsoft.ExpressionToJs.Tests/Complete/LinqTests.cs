using System;
using System.Linq;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>LINQ Where/Select/Sum/Min/Max/Distinct, including chained pipelines.</summary>
    public class LinqTests : DifferentialTestBase
    {
        private static readonly int[] Nums = { 5, 3, 8, 1, 4 };

        // ---- aggregates without selector ----
        [Fact] public void Sum() => AssertUnary<int[], int>(a => a.Sum(), Nums, 21);
        [Fact] public void Min() => AssertUnary<int[], int>(a => a.Min(), Nums, 1);
        [Fact] public void Max() => AssertUnary<int[], int>(a => a.Max(), Nums, 8);

        // ---- aggregates with selector ----
        [Fact] public void SumWithSelector() => AssertUnary<int[], int>(a => a.Sum(x => x * 2), Nums, 42);
        [Fact] public void MaxWithSelector() => AssertUnary<int[], int>(a => a.Max(x => -x), Nums, -1);

        // ---- Where / Select feeding a terminal aggregate ----
        [Fact] public void Where_Count() => AssertUnary<int[], int>(a => a.Where(x => x > 3).Count(), Nums, 3);
        [Fact] public void Where_Sum() => AssertUnary<int[], int>(a => a.Where(x => x % 2 == 0).Sum(), Nums, 12);
        [Fact] public void Select_Sum() => AssertUnary<int[], int>(a => a.Select(x => x + 1).Sum(), Nums, 26);
        [Fact] public void Select_Max() => AssertUnary<int[], int>(a => a.Select(x => x * x).Max(), Nums, 64);

        // ---- chained Where -> Select -> aggregate ----
        [Fact]
        public void Where_Select_Sum()
            => AssertUnary<int[], int>(a => a.Where(x => x > 2).Select(x => x * 10).Sum(), Nums, 200);

        // ---- Distinct ----
        [Fact]
        public void Distinct_Count()
            => AssertUnary<int[], int>(a => a.Distinct().Count(), new[] { 1, 1, 2, 3, 3, 3 }, 3);

        [Fact]
        public void Distinct_Sum()
            => AssertUnary<int[], int>(a => a.Distinct().Sum(), new[] { 1, 1, 2, 3, 3 }, 6);

        // ---- Where with an existing predicate-aware terminal ----
        [Fact]
        public void Where_Any()
            => AssertUnary<int[], bool>(a => a.Where(x => x > 3).Any(x => x > 7), Nums, true);
    }
}
