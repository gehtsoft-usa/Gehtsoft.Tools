using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.Crypto
{
    public class RCFourTransform : ICryptoTransform
    {
        private byte[] mState = new byte[256];
        private int mIndex1 = 0, mIndex2 = 0;

        public RCFourTransform() : this(DEFAULTKEY)
        {
            
        }

        private static byte[] DEFAULTKEY = {0xe6, 0x20, 0xf1, 0x39,  0x0d, 0x19, 0xbd, 0x84,   0xe2, 0xe0, 0xfd, 0x75,  0x20, 0x31, 0xaf, 0xc1};

        public RCFourTransform(string key, bool useHash = false) : this(useHash ? MDFourHash.GetHash(key) : Encoding.UTF8.GetBytes(key))
        {
            
        }

        public RCFourTransform(byte[] key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentNullException(nameof(key));

            int i, j = 0, len = key.Length;
            byte t;
            byte[] state = mState;

            for (i = 0; i < 256; i++)
                mState[i] = (byte)i;

            for (i = 0; i < 256; i++)
            {
                j = (j + state[i] + key[i % len]) & 255;
                t = state[i];
                state[i] = state[j];
                state[j] = t;
            }
        }

        public byte Update(byte src)
        {
            mIndex1 = (mIndex1 + 1) & 255;
            mIndex2 = (mIndex2 + mState[mIndex1]) & 255;

            byte t;

            t = mState[mIndex1];
            mState[mIndex1] = mState[mIndex2];
            mState[mIndex2] = t;

            t = (byte)((mState[mIndex1] + mState[mIndex2]) & 255);

            return (byte)(src ^ mState[((int)t) & 255]);

        }

        public void Update(byte[] source, byte[] output, int srcfrom, int outputfrom, int lenght)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (srcfrom < 0 || srcfrom >= source.Length)
                throw new ArgumentOutOfRangeException(nameof(srcfrom));
            if (outputfrom < 0 || outputfrom >= output.Length)
                throw new ArgumentOutOfRangeException(nameof(outputfrom));
            int srcto = srcfrom + lenght - 1, outputto = outputfrom + lenght - 1;
            if (srcto <= srcfrom || outputto <= outputfrom || srcto >= source.Length || outputto >= output.Length)
                throw new ArgumentOutOfRangeException(nameof(lenght));

            for (; srcfrom <= srcto; outputfrom++, srcfrom++)
            {
                mIndex1 = (mIndex1 + 1) & 255;
                mIndex2 = (mIndex2 + mState[mIndex1]) & 255;

                byte t;

                t = mState[mIndex1];
                mState[mIndex1] = mState[mIndex2];
                mState[mIndex2] = t;

                t = (byte)((mState[mIndex1] + mState[mIndex2]) & 255);

                output[outputfrom] = (byte)(source[srcfrom] ^ mState[((int)t) & 255]);
            }
        }

        public void Update(byte[] array, int from, int length)
        {
            Update(array, array, from, from, length);
        }

        public void Update(byte[] array)
        {
            Update(array, 0, array.Length);
        }

        public void Clear()
        {
            mIndex1 = mIndex2 = 0;
            for (int i = 0; i < mState.Length; i++)
                mState[i] = 0;
        }

        public void Dispose()
        {
            Clear();
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Array.Copy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
            Update(outputBuffer, outputOffset, inputCount);
            return inputCount;

        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] outputBuffer = new byte[inputCount];
            Array.Copy(inputBuffer, inputOffset, outputBuffer, 0, inputCount);
            Update(outputBuffer, 0, inputCount);
            return outputBuffer;
        }

        public int InputBlockSize => 1;
        public int OutputBlockSize => 1;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => false;
    }

    public class RCFourAlgorithm : SymmetricAlgorithm
    {
        private static Random gRandom = new Random((int) (DateTime.Now.Ticks & 0xffff_ffff));

        public override int BlockSize => 8;
        
        public override int FeedbackSize => 8;

        public override KeySizes[] LegalKeySizes { get; } = new KeySizes[] { new KeySizes(8, Int32.MaxValue, 8) };

        public override KeySizes[] LegalBlockSizes { get; } = new KeySizes[] { new KeySizes(8, Int32.MaxValue, 8) };

        public override PaddingMode Padding { get; set; } = PaddingMode.None;
       
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return new RCFourTransform(rgbKey);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return new RCFourTransform(rgbKey);
        }

        public override void GenerateKey()
        {
            Key = new byte[] {32};
            gRandom.NextBytes(Key);
        }

        public override void GenerateIV()
        {
            IV = new byte[1];
            gRandom.NextBytes(IV);
        }

        
        public static string Encode(string text, string key)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            RCFourTransform encoder = new RCFourTransform(key);
            encoder.Update(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string Decode(string encoded, string key, string defaultValue)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(encoded);
                RCFourTransform encoder = new RCFourTransform(key);
                encoder.Update(bytes);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception )
            {
                return defaultValue;
            }
        }
    }
}
