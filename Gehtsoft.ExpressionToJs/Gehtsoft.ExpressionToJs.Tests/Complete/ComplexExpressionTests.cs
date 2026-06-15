using System;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Whole-tree expressions that mix operators, brackets, ternaries, coalesce and method chains.
    /// Each is checked C# == JS == expected, so any precedence/associativity or bracketing error in
    /// the emitted JavaScript is caught.
    /// </summary>
    public class ComplexExpressionTests : DifferentialTestBase
    {
        // ---- integer arithmetic: bracketing must change the result, and the | 0 wrapping at each
        // int-typed node must not disturb operator precedence ----

        [Fact] public void Brackets_ChangeGrouping_A() => AssertUnary<int, int>(x => (x + 2) * 3, 4, 18);
        [Fact] public void Brackets_ChangeGrouping_B() => AssertUnary<int, int>(x => x + 2 * 3, 4, 10);

        [Fact] public void MixedMulDivAddSub() => AssertUnary<int, int>(x => x * 3 + 2 * x - 4 / 2, 4, 18);
        [Fact] public void ProductOfSums() => AssertUnary<int, int>(x => (x - 1) * (x + 1), 5, 24);
        [Fact] public void ModuloDivideMultiplyPrecedence() => AssertUnary<int, int>(x => x % 3 + x / 2 * 4, 7, 13);
        [Fact] public void UnaryMinusBinding() => AssertUnary<int, int>(x => -(x + 1) * 2, 3, -8);
        [Fact] public void DoubleNegate() => AssertUnary<int, int>(x => -x * -x, 3, 9);

        // ---- doubles (no | 0 wrap), nested ----
        [Fact] public void DoubleNested() => AssertUnary<double, double>(d => (d + 1.5) * 2.0 - d / 4.0, 4.0, 10.0);

        // ---- comparison + logical precedence (&& binds tighter than ||) ----
        [Fact] public void AndOrPrecedence_True() => AssertUnary<int, bool>(x => (x > 5 && x < 100) || x == 0, 7, true);
        [Fact] public void AndOrPrecedence_ZeroBranch() => AssertUnary<int, bool>(x => x > 5 && x < 100 || x == 0, 0, true);
        [Fact] public void NotAndOr() => AssertUnary<bool, bool>(b => !(b && false) || (b || true), false, true);

        // ---- ternary nesting and combination with arithmetic ----
        [Fact] public void TernaryPlusArithmetic() => AssertUnary<int, int>(x => (x > 0 ? x * 2 : x - 1) + 3, 5, 13);
        [Fact] public void NestedTernary() => AssertUnary<int, int>(x => x > 10 ? (x > 20 ? 1 : 2) : 3, 15, 2);

        // ---- coalesce woven into a member/arithmetic chain ----
        [Fact] public void CoalesceThenLengthPlus() => AssertUnary<string, int>(s => (s ?? "xy").Length + 1, null, 3);

        // ---- string method chain feeding a comparison ----
        [Fact] public void StringChainComparison() => AssertUnary<string, bool>(s => s.Trim().ToUpper().Length > 2, " abc ", true);

        // ---- the whole tree: int arithmetic + brackets, pinned to an EXACT value ----
        // For x=4 the left side is ((5*2)-3)/2 % 5 = 7/2 % 5 = 3 (note 7/2 truncates to 3). Asserting
        // the exact 3 catches both a precedence/bracketing error and a broken integer truncation,
        // neither of which a loose "> 0" would reveal.
        [Fact]
        public void WholeTree_IntArithmetic_ExactValue()
            => AssertUnary<int, int>(x => ((x + 1) * 2 - 3) / 2 % 5, 4, 3);

        // The left operand up to and including "== 3", in isolation (no && tree around it), so the
        // comparison itself is verified independently of any short-circuit combination.
        [Fact]
        public void WholeTree_IntArithmetic_EqualsExact()
            => AssertUnary<int, bool>(x => ((x + 1) * 2 - 3) / 2 % 5 == 3, 4, true);

        // Same arithmetic compared to its exact value, woven into a boolean tree with && / ||.
        [Fact]
        public void WholeTree_IntAndBoolean()
            => AssertUnary<int, bool>(x => ((x + 1) * 2 - 3) / 2 % 5 == 3 && (x < 100 || x == -1), 4, true);

        // ---- two-parameter trees ----
        [Fact] public void TwoParam_ProductOfSumAndDifference() => AssertBinary<int, int, int>((a, b) => (a + b) * (a - b), 5, 3, 16);
        [Fact] public void TwoParam_MixedArithmetic() => AssertBinary<int, int, int>((a, b) => a * b + a / b - a % b, 10, 3, 32);
        [Fact] public void TwoParam_BooleanTree() => AssertBinary<int, int, bool>((a, b) => a > b && a + b > 10 || a == b, 7, 4, true);
    }
}
