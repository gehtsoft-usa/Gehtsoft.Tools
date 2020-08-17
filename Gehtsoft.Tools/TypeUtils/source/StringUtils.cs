using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public static class StringUtils
    {
        /// <summary>
        /// Performs equality checking using behaviour similar to that of SQL's LIKE.
        /// </summary>
        /// <param name="text">The string to check for equality.</param>
        /// <param name="mask">The mask to check the string against.</param>
        /// <param name="CaseInsensitive">True if the check should be case insensitive.</param>
        /// <returns>Returns true if the string matches the mask.</returns>
        /// <remarks>
        /// All matches are case-insensitive in the invariant culture.
        /// % acts as a multi-character wildcard.
        /// * acts as a multi-character wildcard.
        /// ? acts as a single-character wildcard.
        /// _ acts as a single-character wildcard.
        /// Backslash acts as an escape character.  It needs to be doubled if you wish to
        /// check for an actual backslash.
        /// [abc] searches for multiple characters.
        /// [^abc] matches any character that is not a,b or c
        /// [a-c] matches a, b or c
        /// Published on CodeProject: http://www.codeproject.com/Articles/608266/A-Csharp-LIKE-implementation-that-mimics-SQL-LIKE
        /// </remarks>
        public static bool Like(string text, string mask, bool caseInsensitive = true)
        {
            //Nothing matches a null mask or null input string
            if (mask == null || text == null)
                return false;
            
            //Null strings are treated as empty and get checked against the mask.
            //If checking is case-insensitive we convert to uppercase to facilitate this.
            if (caseInsensitive)
            {
                text = text.ToUpperInvariant();
                mask = mask.ToUpperInvariant();
            }

            //Keeps track of our position in the primary string - s.
            int j = 0;
            //Used to keep track of multi-character wildcards.
            bool matchanymulti = false;
            //Used to keep track of multiple possibility character masks.
            string multicharmask = null;
            bool inversemulticharmask = false;
            for (int i = 0; i < mask.Length; i++)
            {
                //If this is the last character of the mask and its a % or * we are done
                if (i == mask.Length - 1 && (mask[i] == '%' || mask[i] == '*'))
                    return true;
                //A direct character match allows us to proceed.
                var charcheck = true;
                //Backslash acts as an escape character.  If we encounter it, proceed
                //to the next character.
                if (mask[i] == '\\')
                {
                    i++;
                    if (i == mask.Length)
                        i--;
                }
                else
                {
                    //If this is a wildcard mask we flag it and proceed with the next character
                    //in the mask.
                    if (mask[i] == '%' || mask[i] == '*')
                    {
                        matchanymulti = true;
                        continue;
                    }

                    //If this is a single character wildcard advance one character.
                    if (mask[i] == '?' || mask[i] == '_')
                    {
                        //If there is no character to advance we did not find a match.
                        if (j == text.Length)
                            return false;
                        j++;
                        continue;
                    }

                    if (mask[i] == '[')
                    {
                        var endbracketidx = mask.IndexOf(']', i);
                        //Get the characters to check for.
                        multicharmask = mask.Substring(i + 1, endbracketidx - i - 1);
                        //Check for inversed masks
                        inversemulticharmask = multicharmask.StartsWith("^");
                        //Remove the inversed mask character
                        if (inversemulticharmask)
                            multicharmask = multicharmask.Remove(0, 1);
                        //Unescape \^ to ^
                        multicharmask = multicharmask.Replace("\\^", "^");

                        //Prevent direct character checking of the next mask character
                        //and advance to the next mask character.
                        charcheck = false;
                        i = endbracketidx;
                        //Detect and expand character ranges
                        if (multicharmask.Length == 3 && multicharmask[1] == '-')
                        {
                            var newmask = "";
                            var first = multicharmask[0];
                            var last = multicharmask[2];
                            if (last < first)
                            {
                                first = last;
                                last = multicharmask[0];
                            }

                            var c = first;
                            while (c <= last)
                            {
                                newmask += c;
                                c++;
                            }

                            multicharmask = newmask;
                        }

                        //If the mask is invalid we cannot find a mask for it.
                        if (endbracketidx == -1)
                            return false;
                    }
                }

                //Keep track of match finding for this character of the mask.
                var matched = false;
                while (j < text.Length)
                {
                    //This character matches, move on.
                    if (charcheck && text[j] == mask[i])
                    {
                        j++;
                        matched = true;
                        break;
                    }

                    //If we need to check for multiple charaters to do.
                    if (multicharmask != null)
                    {
                        var ismatch = multicharmask.Contains(text[j]);
                        //If this was an inverted mask and we match fail the check for this string.
                        //If this was not an inverted mask check and we did not match fail for this string.
                        if (inversemulticharmask && ismatch ||
                            !inversemulticharmask && !ismatch)
                        {
                            //If we have a wildcard preceding us we ignore this failure
                            //and continue checking.
                            if (matchanymulti)
                            {
                                j++;
                                continue;
                            }

                            return false;
                        }

                        j++;
                        matched = true;
                        //Consumse our mask.
                        multicharmask = null;
                        break;
                    }

                    //We are in an multiple any-character mask, proceed to the next character.
                    if (matchanymulti)
                    {
                        j++;
                        continue;
                    }

                    break;
                }

                //We've found a match - proceed.
                if (matched)
                {
                    matchanymulti = false;
                    continue;
                }

                //If no match our mask fails
                return false;
            }

            //Some characters are left - our mask check fails.
            if (j < text.Length)
                return false;
            //We've processed everything - this is a match.
            return true;
        }

        static Regex mRe = new Regex("\\W*(\\w+(['’]\\w+)?)(.*)", RegexOptions.Multiline);
        static Regex mRe1 = new Regex("[^\\w\\%]*([\\w\\%]+(['’][\\w\\%]+)?)(.*)", RegexOptions.Multiline);

        public static string[] ParseToWords(string text)
        {
            return ParseToWords(text, false);
        }

        public static string[] ParseToWords(string text, bool mask)
        {
            List<string> words = new List<string>();
            while (true)
            {
                Match m = (mask ? mRe1 : mRe).Match(text.Replace('\n', ' '));
                if (!m.Success)
                    break;
                if (m.Groups[1].Value.Length > 0)
                    words.Add(m.Groups[1].Value);
                text = m.Groups[3].Value;
            }
            return words.ToArray();
        }

        public static int LevenshteinDistance(string source, string target)
        {
            if(String.IsNullOrEmpty(source))
            {
                if(String.IsNullOrEmpty(target)) 
                    return 0;
                return target.Length;
            }
            
            if(String.IsNullOrEmpty(target)) 
                return source.Length;

            if(source.Length > target.Length)
            {
                string temp = target;
                target = source;
                source = temp;
            }
 
            int m = target.Length;
            int n = source.Length;
            int[,] distance = new int[2, m + 1];
            
            // Initialize the distance 'matrix'
            for(int j = 1; j <= m; j++) 
                distance[0, j] = j;

            int currentRow = 0;
            for(int i = 1; i <= n; i++)
            {
                currentRow = i & 1;
                distance[currentRow, 0] = i;
                int previousRow = currentRow ^ 1;
                for(int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1] ? 0 : 1);
                    
                    distance[currentRow, j] = Math.Min(Math.Min(distance[previousRow, j] + 1, 
                                                                distance[currentRow, j - 1] + 1),
                                                       distance[previousRow, j - 1] + cost);
                }
            }
            return distance[currentRow, m];
        }

        public static string[] ToWords(this string text) => ParseToWords(text);
        public static string[] ToWords(this string text, bool mask) => ParseToWords(text, mask);
        public static int DistanceTo(this string text, string target) => LevenshteinDistance(text, target);
        public static bool IsLike(this string text, string mask, bool caseInsensitive = false) => Like(text, mask, caseInsensitive);

    }

}
