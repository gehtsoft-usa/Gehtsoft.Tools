using System;
using System.Linq.Expressions;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Verifies the differential helper and the three example rules shown in the "Unit-testing your
    /// expressions" guide actually agree across C# and the emitted JavaScript.
    /// </summary>
    public class UnitTestingGuideTests
    {
        // The exact helper printed in the guide.
        private static void AssertRule<T>(Expression<Func<T, bool>> rule, T input)
        {
            bool expected = rule.Compile()(input);

            string js = new ExpressionCompiler(rule).JavaScriptExpression;

            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            engine.SetValue(rule.Parameters[0].Name, input);
            bool actual = engine.Evaluate(js).AsBoolean();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RangeCheck_Boundary() => AssertRule<int>(age => age >= 18 && age <= 120, 18);

        [Fact]
        public void Required_RejectsWhitespace() => AssertRule<string>(s => !string.IsNullOrWhiteSpace(s), "\t");

        [Fact]
        public void IntegerDivision_StaysInParity() => AssertRule<int>(n => n / 2 == 5, 11);
    }
}
