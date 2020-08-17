using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class ExpressionUtilTest
    {
        public enum TestEnum
        {
            E1 = 1,
            E2 = 2, 
            E3 = 3,
        }

        public class TestTarget1
        {
            public int ID { get; set; }
            public string[] Names { get; set; }
            public IEnumerable<string> ENames => Names;
        }

        public class TestTarget2
        {
            public int ID { get; set; }
            public TestTarget2 SelfReference { get; set; }
            public TestTarget1 Reference { get; set; }
            public TestTarget1[] MultiReference { get; set; }
        }


        [Test]
        public void TestGetName()
        {
            Expression<Func<TestTarget2, object>> expression1;
            expression1 = target2 => target2;
            Assert.AreEqual("", ExpressionUtils.ExpressionToName(expression1));
            expression1 = target2 => target2.ID;
            Assert.AreEqual("ID", ExpressionUtils.ExpressionToName(expression1));
            expression1 = target2 => target2.SelfReference.ID;
            Assert.AreEqual("SelfReference.ID", ExpressionUtils.ExpressionToName(expression1));
            expression1 = target2 => target2.MultiReference[1];
            Assert.AreEqual("MultiReference[1]", ExpressionUtils.ExpressionToName(expression1));
            expression1 = target2 => target2.MultiReference[1].Names[2];
            Assert.AreEqual("MultiReference[1].Names[2]", ExpressionUtils.ExpressionToName(expression1));
        }

        [Test]
        public void TestGetMemberInfo()
        {
            Expression<Func<TestTarget2, object>> expression1;
            expression1 = target2 => target2.ID;
            Assert.AreEqual(typeof(TestTarget2).GetTypeInfo().GetProperty(nameof(TestTarget2.ID)), ExpressionUtils.ExpressionToMemberInfo(expression1));
            expression1 = target2 => target2.SelfReference.ID;
            Assert.AreEqual(typeof(TestTarget2).GetTypeInfo().GetProperty(nameof(TestTarget2.ID)), ExpressionUtils.ExpressionToMemberInfo(expression1));
            expression1 = target2 => target2.SelfReference.MultiReference;
            Assert.AreEqual(typeof(TestTarget2).GetTypeInfo().GetProperty(nameof(TestTarget2.MultiReference)), ExpressionUtils.ExpressionToMemberInfo(expression1));


        }


    }
}
