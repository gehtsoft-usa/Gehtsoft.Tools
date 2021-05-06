using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Gehtsoft.ExpressionToJs.Tests
{
    [TestFixture]
    public class Debug
    {
        public class Entity
        {
            private int mV;
            public int V => mV;

            public int this[int index] { get => 0; set => mV = value + index; }
            public int A { get; set; }
            public string B { get; set; }
            public int[] C { get; set; }
        }

        [Explicit]
        [Test]
        public void Debug1()
        {
            Expression<Func<string, bool>> function = s => Functions.ToInt(s) > 10;
            ExpressionCompiler compiler = new ValidationExpressionCompiler(function, valueParameterIndex: 0);
            string js = compiler.JavaScriptExpression;
            Assert.NotNull(js);
        }
    }
}