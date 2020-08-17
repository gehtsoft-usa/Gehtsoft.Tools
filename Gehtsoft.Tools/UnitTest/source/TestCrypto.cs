using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.Crypto;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class TestCrypto
    {
        [Test]
        public void TestCrc32()
        {
            Assert.AreEqual(0x0, Crc32.GetHash(""));
            Assert.AreEqual(0x414fa339, Crc32.GetHash("The quick brown fox jumps over the lazy dog"));
            Assert.AreEqual(0x0c877f61, Crc32.GetHash("Test vector from febooti.com"));
            Assert.AreEqual(0xcbf43926, Crc32.GetHash("123456789"));
        }


        [Test]
        public void RC4Test1()
        {
            string source1 = "source text is here", source2 = "текст для кодирования";
            string key1 = "", key2 = "the key for encoding", key3 = "wrongkey";
            Assert.Throws<ArgumentNullException>(() => Rc4Decode(Rc4Encode(source1, key1), key1));
            Assert.AreEqual(source1, Rc4Decode(Rc4Encode(source1, key2), key2));
            Assert.AreEqual(source2, Rc4Decode(Rc4Encode(source2, key2), key2));
            Assert.AreEqual(source2, Rc4Decode(Rc4Encode(source2, key2), key2));
            Assert.AreNotEqual(source1, Rc4Decode(Rc4Encode(source1, key2), key3));
            Assert.AreNotEqual(source2, Rc4Decode(Rc4Encode(source1, key3), key2));
            Assert.AreNotEqual(source1, Rc4Decode(Rc4Encode(source2, key3), key3));
            Assert.AreNotEqual(source1, Rc4Decode(Rc4Encode(source2, key2), key2));

            byte[] key = new byte[] { 0x61, 0x8A, 0x63, 0xD2, 0xFB };
            byte[] plaintext = new byte[] { 0xDC, 0xEE, 0x4C, 0xF9, 0x2C };
            byte[] ciphertext = new byte[] { 0xF1, 0x38, 0x29, 0xC9, 0xDE };
            byte[] result = DecodeVector(plaintext, key);

            Assert.AreEqual(ciphertext, result);
        }

        private byte[] DecodeVector(byte[] vector, byte[] key)
        {
            byte[] r = new byte[vector.Length];
            RCFourTransform rc4 = new RCFourTransform(key);
            rc4.TransformBlock(vector, 0, vector.Length, r, 0);
            return r;
        }

        private static string Rc4Encode(string src, string key)
        {
            RCFourTransform r = new RCFourTransform(key);
            byte[] value = Encoding.UTF8.GetBytes(src);
            r.Update(value);
            return Convert.ToBase64String(value);
        }

        private static string Rc4Decode(string src, string key)
        {
            RCFourTransform r = new RCFourTransform(key);
            byte[] value = Convert.FromBase64String(src);
            r.Update(value);
            return Encoding.UTF8.GetString(value);
        }

        private string GetHash(HashAlgorithm hash, string testString)
        {
            hash.Initialize();
            byte[] hashValue = hash.ComputeHash(Encoding.ASCII.GetBytes(testString));
            StringBuilder ret = new StringBuilder();
            foreach (byte b in hashValue)
                ret.Append($"{b:x2}");
            return ret.ToString();
        }

        [Test]
        public void TestMD4()
        {
            MDFourHash hash = new MDFourHash();
            Assert.AreEqual("31d6cfe0d16ae931b73c59d7e0c089c0", GetHash(hash, ""));
            Assert.AreEqual("d9130a8164549fe818874806e1c7014b", GetHash(hash, "message digest"));
            Assert.AreEqual("043f8582f241db351ce627e153e7f0e4", GetHash(hash, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"));
        }

        [Test]
        public void WriteUInt32()
        {
            Random r = new Random((int)(DateTime.Now.Ticks % 65536));

            byte[] arr = new byte[4];
            for (int i = 0; i < 16; i++)
            {
                uint v = (uint) r.Next();
                uint r1, r2, r3, r4;

                ByteSaver.WriteUInt32(arr, 0, v, true);
                r1 = ByteSaver.ReadUInt32(arr, 0, true);
                r3 = ByteSaver.ReadUInt32(arr, 0, false);
                ByteSaver.WriteUInt32(arr, 0, v, false);
                r2 = ByteSaver.ReadUInt32(arr, 0, false);
                r4 = ByteSaver.ReadUInt32(arr, 0, true);
                Assert.AreEqual(v, r1);
                Assert.AreEqual(v, r2);
                Assert.AreNotEqual(v, r3);
                Assert.AreNotEqual(v, r4);
            }
        }


    }

}
