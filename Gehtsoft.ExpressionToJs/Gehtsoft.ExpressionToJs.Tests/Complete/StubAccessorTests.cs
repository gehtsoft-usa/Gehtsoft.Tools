using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Tests for the embedded stub.js resource accessor.</summary>
    public class StubAccessorTests
    {
        [Fact]
        public void GetJsIncludesAsString_NotNullOrEmpty()
        {
            string js = ExpressionToJsStubAccessor.GetJsIncludesAsString();
            Assert.False(string.IsNullOrWhiteSpace(js));
        }

        [Fact]
        public void GetJsIncludesAsString_IsCached()
        {
            string first = ExpressionToJsStubAccessor.GetJsIncludesAsString();
            string second = ExpressionToJsStubAccessor.GetJsIncludesAsString();
            Assert.Same(first, second);
        }

        [Fact]
        public void Stub_IsValidJavaScript()
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString()); // must not throw
        }

        [Theory]
        [InlineData("jsv_plus")]
        [InlineData("jsv_minus")]
        [InlineData("jsv_multiply")]
        [InlineData("jsv_divide")]
        [InlineData("jsv_equal")]
        [InlineData("jsv_notequal")]
        [InlineData("jsv_and")]
        [InlineData("jsv_or")]
        [InlineData("jsv_not")]
        [InlineData("jsv_match")]
        [InlineData("jsv_length")]
        [InlineData("jsv_index")]
        [InlineData("jsv_isempty")]
        [InlineData("jsv_any")]
        [InlineData("jsv_all")]
        [InlineData("jsv_count")]
        [InlineData("jsv_dayssince")]
        [InlineData("jsv_ccn_valid")]
        [InlineData("jsv_trunc")]
        [InlineData("jsv_sign")]
        [InlineData("jsv_fractional")]
        public void CoreFunction_IsDefined(string name)
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            object isFunction = engine.Evaluate($"typeof {name} === 'function'").ToObject();
            Assert.True((bool)isFunction, $"stub.js should define function {name}");
        }
    }
}
