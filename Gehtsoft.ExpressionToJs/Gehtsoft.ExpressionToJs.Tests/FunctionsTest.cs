using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests
{
    public class FunctionsTest
    {
        [Fact]
        public void TestCcnValidator()
        {
            Assert.True(Functions.IsCreditCardNumberCorrect("4444333322221111"));
            Assert.True(Functions.IsCreditCardNumberCorrect("4444 3333 2222 1111"));
            Assert.False(Functions.IsCreditCardNumberCorrect("4444 3333 2222 1112"));
        }
    }
}
