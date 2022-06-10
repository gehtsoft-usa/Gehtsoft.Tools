using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Gehtsoft.Tools2.Algorithm;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class Crc32Test
    {
        [Theory]
        [InlineData("", 0x0)]
        [InlineData("The quick brown fox jumps over the lazy dog", 0x414fa339)]
        [InlineData("Test vector from febooti.com", 0x0c877f61)]
        [InlineData("123456789", 0xcbf43926)]
        [InlineData("@Gehtsoft.Tools2.UnitTest.Resources.longtext.txt", 0xc3ac27a2)]
        public void IEEE(string testString, uint crc32)
        {
            if (testString.StartsWith("@"))
            {
                using (var s = typeof(Crc32Test).Assembly.GetManifestResourceStream(testString.Substring(1)))
                {
                    s.Should().NotBeNull();
                    byte[] b = new byte[s.Length];
                    s.Read(b, 0, b.Length);
                    testString = Encoding.UTF8.GetString(b);
                }
            }
            Crc32.GetHash(testString).Should().Be(crc32);
        }

        [Theory]
        [InlineData("a", 0xc1d04330)]
        [InlineData("Discard medicine more than two years old.", 0xb2cc01fe)]
        public void iSCSI(string testString, uint crc32)
        {
            if (testString.StartsWith("@"))
            {
                using (var s = typeof(Crc32Test).Assembly.GetManifestResourceStream(testString.Substring(1)))
                {
                    s.Should().NotBeNull();
                    byte[] b = new byte[s.Length];
                    s.Read(b, 0, b.Length);
                    testString = Encoding.UTF8.GetString(b);
                }
            }
            Crc32 g = new Crc32(0x82f63b78);
            g.UpdateHash(Encoding.UTF8.GetBytes(testString));
            g.Checksum.Should().Be(crc32);
            



        }

        [Theory]
        [InlineData("The quick brown fox jumps over the lazy dog", true, 0x39, 0xa3, 0x4f, 0x41)]
        [InlineData("The quick brown fox jumps over the lazy dog", false, 0x41, 0x4f, 0xa3, 0x39)]
        public void Endianess(string testString, bool littleIndian, byte b1, byte b2, byte b3, byte b4)
        {
            Crc32 crc = new Crc32() { IsLittleEndian = littleIndian };
            crc.Initialize();
            crc.UpdateHash(Encoding.ASCII.GetBytes(testString));
            var hash = crc.Hash;

            hash.Should().HaveCount(4);
            hash.Should().BeEquivalentTo(new byte[] { b1, b2, b3, b4 });
        }
    }
}
