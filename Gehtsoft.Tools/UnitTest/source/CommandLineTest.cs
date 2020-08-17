using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.CommandLine;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class CommandLineTest
    {
        private void DoSingleLineParserTest(SingleLineParser parser, string commandLine, params string[]expectedResult)
        {
            string[] result = parser.Parse(commandLine);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult?.Length ?? 0, result.Length);
            Assert.AreEqual(expectedResult ?? new string[] {}, result);
        }
       

        [Test]
        public void SingleLineParserTest()
        {
            SingleLineParser parser = new SingleLineParser();
            Assert.Throws<ArgumentNullException>(() => parser.Parse(null));

            DoSingleLineParserTest(parser, "");
            DoSingleLineParserTest(parser, "aaa", "aaa");
            DoSingleLineParserTest(parser, "aaa bbb", "aaa",  "bbb");
            DoSingleLineParserTest(parser, "aaa\tbbb ", "aaa",  "bbb");
            DoSingleLineParserTest(parser, "aaa \"bbb ccc\"", "aaa",  "bbb ccc");
            DoSingleLineParserTest(parser, "aaa\"bbb ccc\"", "aaa",  "bbb ccc");
            DoSingleLineParserTest(parser, "aaa \"b\" ccc", "aaa", "b",  "ccc");
            DoSingleLineParserTest(parser, "aaa \"\" ccc", "aaa", "",  "ccc");
            Assert.Throws<ArgumentException>(() => parser.Parse("args1 \"args2"));
        }

        [Test]
        public void CommandLineParserTest()
        {
            StoringCommandLineParser parser = new StoringCommandLineParser();
            parser.AddKey(null, CommandLineParser.ParameterType.Integer, CommandLineParser.ParameterType.Vararg);
            CommandLineParser.KeyDescription helpKey = parser.AddKey("--help");
            parser.AddKey("--daterange", false, CommandLineParser.ParameterType.Date, CommandLineParser.ParameterType.Date);
            parser.AddKey("--message", CommandLineParser.ParameterType.String);
            parser.AddKey("--dtz-insensitive", CommandLineParser.ParameterType.Boolean);
            parser.AddKey("--seconds", CommandLineParser.ParameterType.Number);

            parser.Reset();

            parser.Parse("321 file1 file2 --help --daterange 3/1/2010 3/5/2010 --message \"my message\" --dtz-insensitive true --seconds 0.5");
            Assert.IsTrue(parser.Errors.Count == 0);
            Assert.AreEqual(6, parser.Arguments.Count);

            StoringCommandLineParser.Argument argument;

            argument = parser.Arguments[0];
            Assert.IsNull(argument.Key.KeyName);
            Assert.AreEqual(3, argument.Arguments.Length);
            Assert.AreEqual(typeof(int), argument.Arguments[0].GetType());
            Assert.AreEqual(typeof(string), argument.Arguments[1].GetType());
            Assert.AreEqual(typeof(string), argument.Arguments[2].GetType());
            Assert.AreEqual(321, argument.Arguments[0]);
            Assert.AreEqual("file1", argument.Arguments[1]);
            Assert.AreEqual("file2", argument.Arguments[2]);

            argument = parser.Arguments["--help"];
            Assert.IsNotNull(argument);
            Assert.AreEqual("--help", argument.Key.KeyName);
            Assert.AreEqual(0, argument.ParametersCount);

            argument = parser.Arguments["--help", 2];
            Assert.IsNull(argument);

            Assert.Throws<ArgumentException>(() =>
            {
                Assert.IsNull(parser.Arguments["--help", 0]); 
            });

            argument = parser.Arguments[helpKey];
            Assert.IsNotNull(argument);
            Assert.AreEqual("--help", argument.Key.KeyName);
            Assert.AreEqual(helpKey, argument.Key);
            Assert.AreEqual(0, argument.ParametersCount);

            argument = parser.Arguments[helpKey, 2];
            Assert.IsNull(argument);

            Assert.Throws<ArgumentException>(() =>
            {
                Assert.IsNull(parser.Arguments[helpKey, 0]); 
            });


            argument = parser.Arguments["--daterange"];
            Assert.IsNotNull(argument);
            Assert.AreEqual("--daterange", argument.Key.KeyName);
            Assert.AreEqual(2, argument.Arguments.Length);
            Assert.AreEqual(typeof(DateTime), argument.Arguments[0].GetType());
            Assert.AreEqual(typeof(DateTime), argument.Arguments[1].GetType());
            Assert.AreEqual(new DateTime(2010, 3, 1), argument.Arguments[0]);
            Assert.AreEqual(new DateTime(2010, 3, 5), argument.Arguments[1]);
            Assert.AreEqual(new DateTime(2010, 3, 1), argument.GetParameter<DateTime>(0));
            Assert.AreEqual(new DateTime(2010, 3, 5), argument.GetParameter<DateTime>(1));

            argument = parser.Arguments["--dtz-insensitive"];
            Assert.IsNotNull(argument);
            Assert.AreEqual("--dtz-insensitive", argument.Key.KeyName);
            Assert.AreEqual(1, argument.Arguments.Length);
            Assert.AreEqual(typeof(bool), argument.Arguments[0].GetType());
            Assert.AreEqual(true, argument.Arguments[0]);

            argument = parser.Arguments["--seconds"];
            Assert.IsNotNull(argument);
            Assert.AreEqual("--seconds", argument.Key.KeyName);
            Assert.AreEqual(1, argument.Arguments.Length);
            Assert.AreEqual(typeof(double), argument.Arguments[0].GetType());
            Assert.AreEqual(0.5, argument.Arguments[0]);

            argument = parser.Arguments["--message"];
            Assert.IsNotNull(argument);
            Assert.AreEqual("--message", argument.Key.KeyName);
            Assert.AreEqual(1, argument.Arguments.Length);
            Assert.AreEqual(typeof(string), argument.Arguments[0].GetType());
            Assert.AreEqual("my message", argument.Arguments[0]);

            parser.Reset();
            parser.Parse("321 file1 file2 --help --daterange 3/1/2010 --message \"my message\" --dtz-insensitive true --seconds 0.5");
            Assert.AreEqual(1, parser.Errors.Count);
            Assert.AreEqual("--daterange", parser.Errors[0].Key.KeyName);
            Assert.AreEqual(CommandLineParser.CommandLineError.WrongNumberOfValues, parser.Errors[0].ErrorCode);
            parser.Reset();
            parser.Parse("321 file1 file2 --help --daterange 3/1/2010 3/5/2010 --message \"my message\" --dtz-insensitive true --seconds");
            Assert.AreEqual(1, parser.Errors.Count);
            Assert.AreEqual("--seconds", parser.Errors[0].Key.KeyName);
            Assert.AreEqual(CommandLineParser.CommandLineError.WrongNumberOfValues, parser.Errors[0].ErrorCode);
            parser.Reset();
            parser.Parse("321 file1 file2 --help hello --daterange 3/1/2010 3/5/2010 --message \"my message\" --dtz-insensitive true --seconds");
            Assert.AreEqual(1, parser.Errors.Count);
            Assert.AreEqual(null, parser.Errors[0].Key);
            Assert.AreEqual(CommandLineParser.CommandLineError.UnknownKey, parser.Errors[0].ErrorCode);
            Assert.AreEqual("hello", parser.Errors[0].Value);
            parser.Reset();

            parser.Parse("e321");
            Assert.AreEqual(1, parser.Errors.Count);
            Assert.AreEqual(null, parser.Errors[0].Key.KeyName);
            Assert.AreEqual(CommandLineParser.CommandLineError.CantParseValue, parser.Errors[0].ErrorCode);
            Assert.AreEqual("e321", parser.Errors[0].Value);

        }
    }
}
