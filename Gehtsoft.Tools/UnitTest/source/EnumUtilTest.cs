using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
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

        [Fact]
        public void Test()
        {
            EnumValue<Enum1, int>[] v1 = EnumUtils.GetEnumValues<Enum1, int>();
            Assert.Equal(4, v1?.Length ?? 0);
            Assert.Equal(1, v1[0].Value);
            Assert.Equal(Enum1.A1, v1[0].EnumerationValue);
            Assert.NotNull(v1[0].FieldInfo);
            Assert.Equal(1, v1[0].FieldInfo.GetRawConstantValue());
            Assert.Equal("A1D", v1[0].Description);
            Assert.Equal(4, v1[3].Value);
            Assert.Equal(Enum1.A4, v1[3].EnumerationValue);
            Assert.Equal("A4D", v1[3].Description);
        }

    }
}
