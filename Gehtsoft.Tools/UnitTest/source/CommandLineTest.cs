using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.CommandLine;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    public class CommandLineTest
    {
        private void DoSingleLineParserTest(SingleLineParser parser, string commandLine, params string[]expectedResult)
        {
            string[] result = parser.Parse(commandLine);
            Assert.NotNull(result);
            Assert.Equal(expectedResult?.Length ?? 0, result.Length);
            Assert.Equal(expectedResult ?? new string[] {}, result);
        }
       

        [Fact]
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

        [Fact]
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
            Assert.True(parser.Errors.Count == 0);
            Assert.Equal(6, parser.Arguments.Count);

            StoringCommandLineParser.Argument argument;

            argument = parser.Arguments[0];
            Assert.Null(argument.Key.KeyName);
            Assert.Equal(3, argument.Arguments.Length);
            Assert.Equal(typeof(int), argument.Arguments[0].GetType());
            Assert.Equal(typeof(string), argument.Arguments[1].GetType());
            Assert.Equal(typeof(string), argument.Arguments[2].GetType());
            Assert.Equal(321, argument.Arguments[0]);
            Assert.Equal("file1", argument.Arguments[1]);
            Assert.Equal("file2", argument.Arguments[2]);

            argument = parser.Arguments["--help"];
            Assert.NotNull(argument);
            Assert.Equal("--help", argument.Key.KeyName);
            Assert.Equal(0, argument.ParametersCount);

            argument = parser.Arguments["--help", 2];
            Assert.Null(argument);

            Assert.Throws<ArgumentException>(() =>
            {
                Assert.Null(parser.Arguments["--help", 0]); 
            });

            argument = parser.Arguments[helpKey];
            Assert.NotNull(argument);
            Assert.Equal("--help", argument.Key.KeyName);
            Assert.Equal(helpKey, argument.Key);
            Assert.Equal(0, argument.ParametersCount);

            argument = parser.Arguments[helpKey, 2];
            Assert.Null(argument);

            Assert.Throws<ArgumentException>(() =>
            {
                Assert.Null(parser.Arguments[helpKey, 0]); 
            });


            argument = parser.Arguments["--daterange"];
            Assert.NotNull(argument);
            Assert.Equal("--daterange", argument.Key.KeyName);
            Assert.Equal(2, argument.Arguments.Length);
            Assert.Equal(typeof(DateTime), argument.Arguments[0].GetType());
            Assert.Equal(typeof(DateTime), argument.Arguments[1].GetType());
            Assert.Equal(new DateTime(2010, 3, 1), argument.Arguments[0]);
            Assert.Equal(new DateTime(2010, 3, 5), argument.Arguments[1]);
            Assert.Equal(new DateTime(2010, 3, 1), argument.GetParameter<DateTime>(0));
            Assert.Equal(new DateTime(2010, 3, 5), argument.GetParameter<DateTime>(1));

            argument = parser.Arguments["--dtz-insensitive"];
            Assert.NotNull(argument);
            Assert.Equal("--dtz-insensitive", argument.Key.KeyName);
            Assert.Single(argument.Arguments);
            Assert.Equal(typeof(bool), argument.Arguments[0].GetType());
            Assert.Equal(true, argument.Arguments[0]);

            argument = parser.Arguments["--seconds"];
            Assert.NotNull(argument);
            Assert.Equal("--seconds", argument.Key.KeyName);
            Assert.Single(argument.Arguments);
            Assert.Equal(typeof(double), argument.Arguments[0].GetType());
            Assert.Equal(0.5, argument.Arguments[0]);

            argument = parser.Arguments["--message"];
            Assert.NotNull(argument);
            Assert.Equal("--message", argument.Key.KeyName);
            Assert.Single(argument.Arguments);
            Assert.Equal(typeof(string), argument.Arguments[0].GetType());
            Assert.Equal("my message", argument.Arguments[0]);

            parser.Reset();
            parser.Parse("321 file1 file2 --help --daterange 3/1/2010 --message \"my message\" --dtz-insensitive true --seconds 0.5");
            Assert.Equal(1, parser.Errors.Count);
            Assert.Equal("--daterange", parser.Errors[0].Key.KeyName);
            Assert.Equal(CommandLineParser.CommandLineError.WrongNumberOfValues, parser.Errors[0].ErrorCode);
            parser.Reset();
            parser.Parse("321 file1 file2 --help --daterange 3/1/2010 3/5/2010 --message \"my message\" --dtz-insensitive true --seconds");
            Assert.Equal(1, parser.Errors.Count);
            Assert.Equal("--seconds", parser.Errors[0].Key.KeyName);
            Assert.Equal(CommandLineParser.CommandLineError.WrongNumberOfValues, parser.Errors[0].ErrorCode);
            parser.Reset();
            parser.Parse("321 file1 file2 --help hello --daterange 3/1/2010 3/5/2010 --message \"my message\" --dtz-insensitive true --seconds");
            Assert.Equal(1, parser.Errors.Count);
            Assert.Null(parser.Errors[0].Key);
            Assert.Equal(CommandLineParser.CommandLineError.UnknownKey, parser.Errors[0].ErrorCode);
            Assert.Equal("hello", parser.Errors[0].Value);
            parser.Reset();

            parser.Parse("e321");
            Assert.Equal(1, parser.Errors.Count);
            Assert.Null(parser.Errors[0].Key.KeyName);
            Assert.Equal(CommandLineParser.CommandLineError.CantParseValue, parser.Errors[0].ErrorCode);
            Assert.Equal("e321", parser.Errors[0].Value);

        }
    }
}
