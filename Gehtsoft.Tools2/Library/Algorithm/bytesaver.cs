using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools2.Algorithm
{
    /// <summary>
    /// The class that saves integers to a byte array
    /// </summary>
    internal static class ByteSaver
    {
        /// <summary>
        /// Writes a 32-bit value
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        /// <param name="littleEndian"></param>
        public static void WriteUInt32(byte[] array, int offset, uint value, bool littleEndian)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0 || offset > array.Length - 4)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (littleEndian)
            {
                array[offset + 3] = (byte)((value >> 24) & 255);
                array[offset + 2] = (byte)((value >> 16) & 255);
                array[offset + 1] = (byte)((value >> 8) & 255);
                array[offset + 0] = (byte)((value) & 255);
            }
            else
            {
                array[offset + 0] = (byte)((value >> 24) & 255);
                array[offset + 1] = (byte)((value >> 16) & 255);
                array[offset + 2] = (byte)((value >> 8) & 255);
                array[offset + 3] = (byte)((value) & 255);
            }
        }

        public static uint ReadUInt32(byte[] array, int offset, bool littleEndian)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0 || offset > array.Length - 4)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (littleEndian)
            {
                return (uint)array[offset + 0] & 0xff |
                       (((uint)array[offset + 1] & 0xff) << 8) |
                       (((uint)array[offset + 2] & 0xff) << 16) |
                       (((uint)array[offset + 3] & 0xff) << 24);
            }
            else
            {
                return (uint)array[offset + 3] & 0xff |
                       (((uint)array[offset + 2] & 0xff) << 8) |
                       (((uint)array[offset + 1] & 0xff) << 16) |
                       (((uint)array[offset + 0] & 0xff) << 24);
            }
        }

        public static void WriteUInt16(byte[] array, int offset, ushort value, bool littleEndian)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0 || offset > array.Length - 2)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (littleEndian)
            {
                array[offset + 1] = (byte)((value >> 8) & 255);
                array[offset + 0] = (byte)((value) & 255);
            }
            else
            {
                array[offset + 0] = (byte)((value >> 8) & 255);
                array[offset + 1] = (byte)((value) & 255);
            }
        }

        public static ushort ReadUInt16(byte[] array, int offset, bool littleEndian)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0 || offset > array.Length - 2)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (littleEndian)
            {
                return (ushort)((ushort)(array[offset + 0] & 0xff) | (((ushort)array[offset + 1] & 0xff) << 8));
            }
            else
            {
                return (ushort)((ushort)(array[offset + 1] & 0xff) | (((ushort)array[offset + 0] & 0xff) << 8));
            }
        }       
    }
}