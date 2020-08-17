using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Crypto
{
    public class MDFourHash : HashAlgorithm
    {
        private uint mA;
        private uint mB;
        private uint mC;
        private uint mD;
        private uint[] mKey;
        private int mProcessed;

        public MDFourHash()
        {
            mKey = new uint[16];
            InitializeInternal();
        }

        public override void Initialize()
        {
            InitializeInternal();
        }

        private void InitializeInternal()
        {
            mA = 0x67452301;
            mB = 0xefcdab89;
            mC = 0x98badcfe;
            mD = 0x10325476;
            mProcessed = 0;
        }

        protected override void HashCore(byte[] array, int offset, int length)
        {
            ProcessMessage(Bytes(array, offset, length));
        }

        protected override byte[] HashFinal()
        {
            try
            {
                ProcessMessage(Padding());
                byte[] hash = new byte[16];
                ByteSaver.WriteUInt32(hash, 0, mA, true);
                ByteSaver.WriteUInt32(hash, 4, mB, true);
                ByteSaver.WriteUInt32(hash, 8, mC, true);
                ByteSaver.WriteUInt32(hash, 12, mD, true);
                return hash;
            }
            finally
            {
                Initialize();
            }
        }

        private void ProcessMessage(IEnumerable<byte> bytes)
        {
            foreach (byte b in bytes)
            {
                int c = mProcessed & 63;
                int i = c >> 2;
                int s = (c & 3) << 3;

                mKey[i] = (mKey[i] & ~((uint) 255 << s)) | ((uint) b << s);

                if (c == 63)
                {
                    Process16WordBlock();
                }

                mProcessed++;
            }
        }

        private static IEnumerable<byte> Bytes(byte[] bytes, int offset, int length)
        {
            for (int i = offset; i < length; i++)
            {
                yield return bytes[i];
            }
        }

        private IEnumerable<byte> Bytes(uint word)
        {
            yield return (byte) (word & 255);
            yield return (byte) ((word >> 8) & 255);
            yield return (byte) ((word >> 16) & 255);
            yield return (byte) ((word >> 24) & 255);
        }

        private IEnumerable<byte> Repeat(byte value, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return value;
            }
        }

        private IEnumerable<byte> Padding()
        {
            return Repeat(128, 1)
                .Concat(Repeat(0, ((mProcessed + 8) & 0x7fffffc0) + 55 - mProcessed))
                .Concat(Bytes((uint) mProcessed << 3))
                .Concat(Repeat(0, 4));
        }

        private void Process16WordBlock()
        {
            uint aa = mA;
            uint bb = mB;
            uint cc = mC;
            uint dd = mD;

            foreach (int k in new[] {0, 4, 8, 12})
            {
                aa = Round1Operation(aa, bb, cc, dd, mKey[k], 3);
                dd = Round1Operation(dd, aa, bb, cc, mKey[k + 1], 7);
                cc = Round1Operation(cc, dd, aa, bb, mKey[k + 2], 11);
                bb = Round1Operation(bb, cc, dd, aa, mKey[k + 3], 19);
            }

            foreach (int k in new[] {0, 1, 2, 3})
            {
                aa = Round2Operation(aa, bb, cc, dd, mKey[k], 3);
                dd = Round2Operation(dd, aa, bb, cc, mKey[k + 4], 5);
                cc = Round2Operation(cc, dd, aa, bb, mKey[k + 8], 9);
                bb = Round2Operation(bb, cc, dd, aa, mKey[k + 12], 13);
            }

            foreach (int k in new[] {0, 2, 1, 3})
            {
                aa = Round3Operation(aa, bb, cc, dd, mKey[k], 3);
                dd = Round3Operation(dd, aa, bb, cc, mKey[k + 8], 9);
                cc = Round3Operation(cc, dd, aa, bb, mKey[k + 4], 11);
                bb = Round3Operation(bb, cc, dd, aa, mKey[k + 12], 15);
            }

            unchecked
            {
                mA += aa;
                mB += bb;
                mC += cc;
                mD += dd;
            }
        }

        private static uint ROL(uint value, int numberOfBits)
        {
            return (value << numberOfBits) | (value >> (32 - numberOfBits));
        }

        private static uint Round1Operation(uint a, uint b, uint c, uint d, uint xk, int s)
        {
            unchecked
            {
                return ROL(a + ((b & c) | (~b & d)) + xk, s);
            }
        }

        private static uint Round2Operation(uint a, uint b, uint c, uint d, uint xk, int s)
        {
            unchecked
            {
                return ROL(a + ((b & c) | (b & d) | (c & d)) + xk + 0x5a827999, s);
            }
        }

        private static uint Round3Operation(uint a, uint b, uint c, uint d, uint xk, int s)
        {
            unchecked
            {
                return ROL(a + (b ^ c ^ d) + xk + 0x6ed9eba1, s);
            }
        }

        public static byte[] GetHash(byte[] message)
        {
            MDFourHash hash = new MDFourHash();
            return hash.ComputeHash(message);
        }

        public static byte[] GetHash(string message) => GetHash(Encoding.UTF8.GetBytes(message));
    }
}
