using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class EnumUtilTest
    {
        public enum Enum1 : int
        {
            [EnumDescription("A1D")]
            A1 = 1,
            [EnumDescription("A2D")]
            A2 = 2,
            [EnumDescription("A3D")]
            A3 = 3,
            [EnumDescription("A4D")]
            A4 = 4,
        }

        [Flags]
        public enum Enum2 : int
        {
            [EnumDescription("B1D")]
            B1 = 1,
            [EnumDescription("B1D")]
            B2 = 2,
            [EnumDescription("B1D")]
            B3 = 4,
            [EnumDescription("A4D")]
            B4 = 8,
        }

        [Test]
        public void Test()
        {
            EnumValue<Enum1, int>[] v1 = EnumUtils.GetEnumValues<Enum1, int>();
            Assert.AreEqual(4, v1?.Length ?? 0);
            Assert.AreEqual(1, v1[0].Value);
            Assert.AreEqual(Enum1.A1, v1[0].EnumerationValue);
            Assert.IsNotNull(v1[0].FieldInfo);
            Assert.AreEqual(1, v1[0].FieldInfo.GetRawConstantValue());
            Assert.AreEqual("A1D", v1[0].Description);
            Assert.AreEqual(4, v1[3].Value);
            Assert.AreEqual(Enum1.A4, v1[3].EnumerationValue);
            Assert.AreEqual("A4D", v1[3].Description);
        }

    }
}
