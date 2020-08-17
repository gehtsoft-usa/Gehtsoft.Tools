using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Gehtsoft.ExpressionToJs.Tests
{
    [TestFixture]
    public class FunctionsTest
    {
        [Test]
        public void TestCcnValidator()
        {
            Assert.IsTrue(Functions.IsCreditCardNumberCorrect("4444333322221111"));
            Assert.IsTrue(Functions.IsCreditCardNumberCorrect("4444 3333 2222 1111"));
            Assert.IsFalse(Functions.IsCreditCardNumberCorrect("4444 3333 2222 1112"));
        }
    }
}
