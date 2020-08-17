using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.ExpressionToJs
{
    public class ExpressionToJsStubAccessor
    {
        private static byte[] gJsIncludesAsBytes = null;
        private static string gJsIncludesAsString = null;

        private static byte[] GetJsIncludesAsBytes()
        {
            if (gJsIncludesAsBytes != null)
                return gJsIncludesAsBytes;

            using (Stream stream = typeof(ExpressionToJsStubAccessor).Assembly.GetManifestResourceStream("Gehtsoft.ExpressionToJs.stub.js"))
            {
                BinaryReader br = new BinaryReader(stream);
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                gJsIncludesAsBytes = ba;
                return ba;
            }
        }

        private static string BytesToString(byte[] ba, Encoding encoding)
        {
            if (ba.Length > 3 && ba[0] == 0xef && ba[1] == 0xbb && ba[2] == 0xbf)
                return Encoding.UTF8.GetString(ba, 3, ba.Length - 3);
            else
                return encoding.GetString(ba);

        }

        public static string GetJsIncludesAsString() => gJsIncludesAsString ?? (gJsIncludesAsString = BytesToString(GetJsIncludesAsBytes(), Encoding.UTF8));
    }
}
