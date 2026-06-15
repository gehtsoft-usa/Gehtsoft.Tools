using System;
using System.Linq;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Probes that start from the assumption the library is wrong: each asserts the C#-faithful
    /// result for a construct that is either silently divergent or unsupported. Expected RED until
    /// the gap is closed.
    /// </summary>
    public class FeatureGapProbeTests : DifferentialTestBase
    {
        // Integer '+' wraps in C# (unchecked 32-bit) but JS numbers are doubles and never wrap.
        [Fact]
        public void IntegerAddition_Overflows_LikeCSharp()
            => AssertBinary<int, int, int>((x, y) => x + y, int.MaxValue, 1, int.MinValue);

        // Math.Round(value, digits) is a common overload that AddCall does not translate.
        [Fact]
        public void Round_WithDigits_IsSupported()
            => AssertUnary<double, double>(x => Math.Round(x, 2), 1.2345, 1.23);

        // Null-coalescing is extremely common in validation but ExpressionType.Coalesce is unhandled.
        [Fact]
        public void NullCoalescing_IsSupported()
            => AssertUnary<string, string>(x => x ?? "default", null, "default");

        // Enumerable.Contains(value) (no predicate) is not among the handled LINQ methods.
        [Fact]
        public void EnumerableContains_IsSupported()
            => AssertUnary<int[], bool>(a => a.Contains(2), new[] { 1, 2, 3 }, true);
    }
}
