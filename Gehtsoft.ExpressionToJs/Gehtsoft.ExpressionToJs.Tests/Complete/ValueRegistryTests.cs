using System;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Tests for the constant (compiler.Constants) and member (compiler.Members) extension points.</summary>
    public class ValueRegistryTests
    {
        public class Player
        {
            public int Score { get; set; }
        }

        public class Holder
        {
            public int Count { get; set; }
        }

        // ---------- constants ----------

        [Fact]
        public void UnknownConstantType_ThrowsWithoutRegistration()
        {
            Guid id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Expression<Func<Guid, bool>> expr = x => x == id;
            Assert.Throws<ArgumentException>(() => { _ = new ExpressionCompiler(expr).JavaScriptExpression; });
        }

        [Fact]
        public void MapConstant_EmitsCustomType()
        {
            Guid id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Expression<Func<Guid, bool>> expr = x => x == id;
            var compiler = new ExpressionCompiler(expr);
            compiler.Constants.MapConstant<Guid>(g => "'" + g + "'");
            Assert.Contains("'00000000-0000-0000-0000-000000000001'", compiler.JavaScriptExpression);
        }

        // ---------- members ----------

        [Fact]
        public void Member_DefaultsToParameterAccess()
        {
            Expression<Func<Player, int>> expr = p => p.Score;
            Assert.Equal("p.Score", new ExpressionCompiler(expr).JavaScriptExpression);
        }

        [Fact]
        public void MapMember_EmitsCustomTemplate()
        {
            Expression<Func<Player, int>> expr = p => p.Score;
            var compiler = new ExpressionCompiler(expr);
            compiler.Members.MapMember(typeof(Player), nameof(Player.Score), "$obj.score");
            Assert.Equal("p.score", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MapMember_ShadowsBuiltin()
        {
            // Count is matched by the built-in Length/Count rule (-> jsv_length); a user mapping wins.
            Expression<Func<Holder, int>> expr = h => h.Count;
            Assert.Equal("jsv_length(h)", new ExpressionCompiler(expr).JavaScriptExpression);

            var compiler = new ExpressionCompiler(expr);
            compiler.Members.MapMember(typeof(Holder), nameof(Holder.Count), "$obj.size");
            Assert.Equal("h.size", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Registration_IsPerInstance()
        {
            Expression<Func<Player, int>> expr = p => p.Score;
            var customized = new ExpressionCompiler(expr);
            customized.Members.MapMember(typeof(Player), nameof(Player.Score), "$obj.score");
            _ = customized.JavaScriptExpression;

            Assert.Equal("p.Score", new ExpressionCompiler(expr).JavaScriptExpression);
        }
    }
}
