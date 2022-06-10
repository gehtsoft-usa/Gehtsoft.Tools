using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Gehtsoft.Tools2.Extensions;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class StringTest
    {
        [Theory]
        [InlineData("a", "a", true, true)]
        [InlineData("aa", "aa", true, true)]
        [InlineData("aa", "ab", true, false)]
        [InlineData("a", "A", true, true)]
        [InlineData("a", "A", false, false)]

        [InlineData("a", "a%", true, true)]
        [InlineData("abc", "a%", true, true)]
        [InlineData("abc", "a%c", true, true)]
        [InlineData("abc", "a%d", true, false)]
        [InlineData("abc", "a_c", true, true)]
        [InlineData("abbc", "a_c", true, false)]

        [InlineData("a", "a*", true, true)]
        [InlineData("a", "a?", true, false)]
        [InlineData("abc", "a*", true, true)]
        [InlineData("abc", "a\\*", true, false)]
        [InlineData("a*c", "a\\*c", true, true)]
        [InlineData("abbc", "a*c", true, true)]
        [InlineData("abc", "a*d", true, false)]
        //[InlineData("abc", "a*[a-c]", true, true)]        TBD: need to fix
        [InlineData("abc", "a?*", true, true)]
        [InlineData("ac", "a?*", true, true)]
        [InlineData("a", "a?*", true, false)]
        [InlineData("abc", "a*[^a-c]", true, false)]
        [InlineData("abc", "a?c", true, true)]

        [InlineData("abcd", "a%c%d", true, true)]

        [InlineData("ab", "a[abc]", true, true)]
        [InlineData("ab", "a[a-c]", true, true)]
        [InlineData("ab", "a[^d-z]", true, true)]

        [InlineData("ad", "a[abc]", true, false)]
        [InlineData("ad", "a[a-c]", true, false)]
        [InlineData("ad", "a[c-a]", true, false)]
        [InlineData("ad", "a[^d-z]", true, false)]

        [InlineData("abc", "a[abc]c", true, true)]
        public void Like(string text, string mask, bool caseInsensitive, bool result)
        {
            text.Like(mask, caseInsensitive).Should().Be(result);
        }

        [Theory]
        [InlineData("This is text", "This", "is", "text")]
        [InlineData("I've don't, and... couldn’t", "I've", "don't", "and", "couldn’t")]
        public void SplitWords(string text, params string[] result)
        {
            text.ParseToWords().Should().BeEquivalentTo(result);
        }

        [Theory]
        [InlineData("done", "dune", 1)]
        [InlineData("", "one", 3)]
        [InlineData("", "", 0)]
        [InlineData("one", "done", 1)]
        [InlineData("done", "one", 1)]
        [InlineData("kitten", "sitting", 3)]
        public void LevenshteinDistanceTo(string word1, string word2, int distance)
        {
            word1.LevenshteinDistanceTo(word2).Should().Be(distance);
        }

        [Theory]
        [InlineData("c:/path1", "c:/path2", '\\', @"..\path1")]
        [InlineData("c:/path1/subfolder1/subfolder2", "c:/path1/subfolder3/subfolder4", '/', @"../../subfolder1/subfolder2")]
        [InlineData("c:/path1/subfolder1/subfolder2", "c:/path1/subfolder1/subfolder3", '/', @"../subfolder2")]
        public void RelativePathTo(string path, string @base, char separator, string result)
        {
            path.RelativePathTo(@base, separator).Should().Be(result);
        }

        [Theory]
        [InlineData("c:/path1", "d:/path1")]
        [InlineData(@"c:\path1", @"d:\path1")]
        public void RelativePathTo_Fail(string path, string @base)
        {
            ((Action)(() => path.RelativePathTo(@base))).Should().Throw<ArgumentException>();
        }
    }
}
