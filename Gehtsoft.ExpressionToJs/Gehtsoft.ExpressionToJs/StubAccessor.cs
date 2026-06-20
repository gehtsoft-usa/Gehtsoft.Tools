using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.ExpressionToJs
{
    /// <summary>
    /// Gives you the JavaScript runtime that every compiled expression depends on.
    ///
    /// The strings produced by <see cref="ExpressionCompiler.JavaScriptExpression"/> call into a set
    /// of [c]jsv_*[/c] helper functions (for null-safe arithmetic, string handling, date math, LINQ,
    /// and so on). Those helpers live in this stub, which is embedded in the assembly. Emit the stub
    /// into the page (or otherwise load it into the JS engine) [i]once[/i], before any compiled
    /// expression is evaluated; the emitted expressions are useless without it. Typical use is to
    /// drop the returned text inside a [c]&lt;script&gt;[/c] block in your page layout.
    /// </summary>
    public static class ExpressionToJsStubAccessor
    {
        private static byte[] gJsIncludesAsBytes = null;
        private static string gJsIncludesAsString = null;

        private static byte[] GetJsIncludesAsBytes()
        {
            if (gJsIncludesAsBytes != null)
                return gJsIncludesAsBytes;

            using (Stream stream = typeof(ExpressionToJsStubAccessor).Assembly.GetManifestResourceStream("Gehtsoft.ExpressionToJs.stub.js"))
            {
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

        /// <summary>
        /// Returns the full jsv_* JavaScript runtime stub to embed in your page.
        ///
        /// Call it once during page composition and place the returned text inside a script block so
        /// the browser has it loaded before any compiled expression runs. The content is fixed and
        /// cached after the first call, so repeated calls are cheap and you may call it freely from
        /// view code.
        /// </summary>
        /// <returns>The complete JavaScript runtime stub as a single string of source.</returns>
        public static string GetJsIncludesAsString() => gJsIncludesAsString ?? (gJsIncludesAsString = BytesToString(GetJsIncludesAsBytes(), Encoding.UTF8));
    }
}
