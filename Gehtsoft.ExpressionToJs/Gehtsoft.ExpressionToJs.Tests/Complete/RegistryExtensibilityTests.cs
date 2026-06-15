using System;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Coverage for the lower-level extensibility surface that <see cref="ValueRegistryTests"/> exercises
    /// only through the <c>MapConstant</c>/<c>MapMember</c> sugar: the raw
    /// <c>compiler.Constants.AddTranslator</c> / <c>compiler.Members.AddTranslator</c> registration paths,
    /// their argument-null guards, and the <see cref="DelegateConstantTranslator"/> directly - including
    /// its constructor guards and the "value does not match my type, fall through" branch.
    /// </summary>
    public class RegistryExtensibilityTests
    {
        public class Box { public int Value { get; set; } }

        private static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // ---------------------------------------------------------- ConstantRegistry.AddTranslator

        [Fact]
        public void Constants_AddTranslator_CustomTranslatorIsUsed()
        {
            Expression<Func<Guid, bool>> expr = x => x == Id;
            var compiler = new ExpressionCompiler(expr);
            compiler.Constants.AddTranslator(new DelegateConstantTranslator(typeof(Guid), g => "'GID'"));
            Assert.Equal("jsv_equal(x, 'GID')", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Constants_AddTranslator_NullThrows()
        {
            var compiler = new ExpressionCompiler((Expression<Func<int, int>>)(x => x));
            Assert.Throws<ArgumentNullException>(() => compiler.Constants.AddTranslator(null));
        }

        [Fact]
        public void Constants_MapConstant_NullEmitThrows()
        {
            var compiler = new ExpressionCompiler((Expression<Func<int, int>>)(x => x));
            Assert.Throws<ArgumentNullException>(() => compiler.Constants.MapConstant<Guid>(null));
        }

        // ---------------------------------------------------------- MemberRegistry.AddTranslator

        [Fact]
        public void Members_AddTranslator_CustomTranslatorIsUsed()
        {
            Expression<Func<Box, int>> expr = b => b.Value;
            var compiler = new ExpressionCompiler(expr);
            compiler.Members.AddTranslator(new FixedMemberTranslator(nameof(Box.Value), "$obj.v"));
            Assert.Equal("b.v", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Members_AddTranslator_NullThrows()
        {
            var compiler = new ExpressionCompiler((Expression<Func<Box, int>>)(b => b.Value));
            Assert.Throws<ArgumentNullException>(() => compiler.Members.AddTranslator(null));
        }

        // ---------------------------------------------------------- DelegateConstantTranslator

        [Fact]
        public void DelegateConstantTranslator_NullArgs_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new DelegateConstantTranslator(null, o => ""));
            Assert.Throws<ArgumentNullException>(() => new DelegateConstantTranslator(typeof(int), null));
        }

        [Fact]
        public void DelegateConstantTranslator_NonMatchingValue_FallsThroughToBuiltin()
        {
            // A Guid-only custom translator is registered, but the only constant is an int. The
            // translator is consulted, its type test fails (returns false), and the built-in int
            // emitter takes over - exercising the "no match" branch.
            Expression<Func<int, bool>> expr = x => x == 5;
            var compiler = new ExpressionCompiler(expr);
            compiler.Constants.MapConstant<Guid>(g => "'GID'");
            Assert.Equal("jsv_equal(x, 5)", compiler.JavaScriptExpression);
        }

        /// <summary>A minimal custom member translator used to exercise Members.AddTranslator.</summary>
        private sealed class FixedMemberTranslator : IMemberTranslator
        {
            private readonly string mName;
            private readonly string mTemplate;

            public FixedMemberTranslator(string name, string template)
            {
                mName = name;
                mTemplate = template;
            }

            public bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js)
            {
                if (member.Member.Name == mName)
                {
                    js = mTemplate.Replace("$obj", context.Emit(member.Expression));
                    return true;
                }
                js = null;
                return false;
            }
        }
    }
}
