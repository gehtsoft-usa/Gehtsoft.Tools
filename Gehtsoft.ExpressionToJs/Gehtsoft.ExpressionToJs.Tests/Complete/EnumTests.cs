using System;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Generic enum support: enums emit as their underlying numeric value.</summary>
    public class EnumTests
    {
        public enum Status { Inactive = 0, Active = 1, Pending = 2 }

        [Flags]
        public enum Perm { None = 0, Read = 1, Write = 2, Exec = 4 }

        public class Holder { public Status Status { get; set; } }

        public static bool Check(int v, Status s) => true;

        [Fact]
        public void EnumConstant_EmitsUnderlyingValue()
        {
            Assert.Equal("1", ExpressionCompiler.AddConstant(Status.Active));
            Assert.Equal("2", ExpressionCompiler.AddConstant(Status.Pending));
        }

        [Fact]
        public void FlagsEnumConstant_EmitsCombinedValue()
        {
            Assert.Equal("3", ExpressionCompiler.AddConstant(Perm.Read | Perm.Write));
        }

        [Fact]
        public void EnumComparison_EmitsNumeric()
        {
            Expression<Func<Holder, bool>> expr = h => h.Status == Status.Active;
            string js = new ExpressionCompiler(expr).JavaScriptExpression;
            Assert.StartsWith("jsv_equal(", js);
            Assert.Contains("h.Status", js);
            Assert.EndsWith(", 1)", js); // Active == 1
        }

        [Fact]
        public void EnumConstant_AsMethodArgument()
        {
            Expression<Func<int, bool>> expr = x => Check(x, Status.Pending);
            var compiler = new ExpressionCompiler(expr);
            compiler.Methods.MapMethod(typeof(EnumTests), nameof(Check), "jsv_check($0, $1)");
            Assert.Equal("jsv_check(x, 2)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void EnumConstant_NameOverride_ViaRegistry()
        {
            Expression<Func<int, bool>> expr = x => Check(x, Status.Active);
            var compiler = new ExpressionCompiler(expr);
            compiler.Methods.MapMethod(typeof(EnumTests), nameof(Check), "jsv_check($0, $1)");
            compiler.Constants.MapConstant<Status>(s => "'" + s + "'");
            Assert.Equal("jsv_check(x, 'Active')", compiler.JavaScriptExpression);
        }
    }
}
