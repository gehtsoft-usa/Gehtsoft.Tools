using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Jint;
using Jint.Native;
using NUnit.Framework;

//all code pieces causes warning made for purpose to test nuances
//of proper expression compilation
#pragma warning disable S1940, S1125, S1905, S2589, S2699, S2184, RCS1163, IDE0057, RCS1068, RCS1077, IDE1006, RCS1033

namespace Gehtsoft.ExpressionToJs.Tests
{
    [TestFixture]
    public class CompilerTest
    {
        public class SubEntity
        {
            public int subIntProp { get; set; }
            public int index { get; set; }
        }

        public class Entity
        {
            public int intProp { get; set; }
            public string stringProp { get; set; }
            public int[] arrayProp { get; set; }
            public SubEntity SubEntity { get; set; }
        }

        private readonly Regex mRe = new Regex("^a.+f$");

        private Engine SetupJint()
        {
            Engine engine = new Engine();
            engine.SetValue("intVal", 10);
            engine.SetValue("intArr", new int[] { 10, 20, 30, 40, 50 });
            engine.SetValue("doubleVal", 1234.56);
            engine.SetValue("dateTime1", new DateTime(2002, 1, 31, 12, 30, 18));
            engine.SetValue("stringVal", "abcdef");
            engine.SetValue("stringVal1", "  ABC   ");
            engine.SetValue("stringVal2", "Bb1, ");
            engine.SetValue("stringVal3", "on");
            engine.SetValue("stringVal4", "50");

            engine.SetValue("dateVal", new DateTime(2001, 11, 9, 8, 46, 0));
            engine.SetValue("entityVal", new Entity() { intProp = 20, stringProp = "eklmn", arrayProp = new int[] { 1, 2, 3 }, SubEntity = new SubEntity() { subIntProp = 515, index = 2 } });
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            return engine;
        }

        public void TestExpression<TA, TR>(Expression<Func<TA, TR>> expression, TR expectedResult)
        {
            ExpressionCompiler compiler = new ExpressionCompiler(expression);
            string jsExpression = compiler.JavaScriptExpression;
            Engine engine = SetupJint();
            if (typeof(TR) == typeof(double))
                Assert.AreEqual((double)(object)expectedResult, (double)engine.Execute(jsExpression).GetCompletionValue().ToObject(), 1e-9);
            else if (typeof(TR) == typeof(DateTime))
            {
                JsValue v = engine.Execute(jsExpression).GetCompletionValue();
                Assert.That(((DateTime)(object)expectedResult).ToUniversalTime(), Is.EqualTo(v.AsDate().ToDateTime()).Within(TimeSpan.FromMilliseconds(950)));
            }
            else
                Assert.AreEqual(expectedResult, engine.Execute(jsExpression).GetCompletionValue().ToObject());
        }

        [Test]
        public void TestMath()
        {
            //primitives
            TestExpression<int, int>(intVal => intVal + 5, 15);
            TestExpression<double, double>(doubleVal => doubleVal + 5.0, 1239.56);
            TestExpression<int, int>(intVal => intVal - 5, 5);
            TestExpression<double, double>(doubleVal => doubleVal - 5.0, 1229.56);
            TestExpression<int, int>(intVal => -intVal, -10);
            TestExpression<double, double>(doubleVal => -doubleVal, -1234.56);
            TestExpression<int, int>(intVal => intVal * 5, 50);
            TestExpression<int, int>(intVal => intVal / 2, 5);
            TestExpression<int, int>(intVal => intVal % 3, 1);
            TestExpression<double, double>(doubleVal => -(doubleVal * 2.5 + 1) / -3, (1234.56 * 2.5 + 1) / 3);

            //functions
            TestExpression<double, double>(doubleVal => Math.Round(doubleVal), 1235);
            TestExpression<double, double>(doubleVal => Math.Floor(doubleVal), 1234);
            TestExpression<double, double>(doubleVal => Math.Ceiling(doubleVal), 1235);
            TestExpression<double, double>(doubleVal => Math.Sqrt(doubleVal), Math.Sqrt(1234.56));
            TestExpression<double, double>(doubleVal => Math.Log(doubleVal), Math.Log(1234.56));
            TestExpression<double, double>(doubleVal => Math.Exp(doubleVal), Math.Exp(1234.56));
            TestExpression<double, double>(doubleVal => Math.Pow(doubleVal, 0.3), Math.Pow(1234.56, 0.3));
            TestExpression<double, double>(doubleVal => Math.Truncate(doubleVal), 1234);
            TestExpression<double, double>(doubleVal => Math.Truncate(-doubleVal), -1234);

            TestExpression<double, double>(doubleVal => Math.Max(doubleVal, 10000), 10000);
            TestExpression<double, double>(doubleVal => Math.Min(Math.Floor(doubleVal) / 2, doubleVal), 1234.0 / 2);

            TestExpression<double, double>(doubleVal => Math.Asin(Math.Sin(Math.PI / 4)), Math.PI / 4);
            TestExpression<double, double>(doubleVal => Math.Acos(Math.Cos(Math.PI / 4)), Math.PI / 4);
            TestExpression<double, double>(doubleVal => Math.Tan(Math.Atan(Math.PI / 4)), Math.PI / 4);

            TestExpression<double, double>(doubleVal => Functions.Fractional(1.234), 0.234);
            TestExpression<double, double>(doubleVal => Functions.Fractional(-1.234), 0.234);
            TestExpression<double, double>(doubleVal => Math.Sign(12.234), 1);
            TestExpression<double, double>(doubleVal => Math.Sign(-12.234), -1);

            const int j = 50;
            TestExpression<int, int>(intVal => intVal * j / (j + 50), 5);
        }

        [Test]
        public void TestString()
        {
            TestExpression<string, int>(stringVal => stringVal.Length, 6);
            TestExpression<Entity, int>(entityVal => entityVal.stringProp.Length, 5);
            TestExpression<string, bool>(stringVal => string.IsNullOrEmpty(stringVal), false);
            TestExpression<string, bool>(stringVal => string.IsNullOrEmpty(null), true);

            TestExpression<string, bool>(stringVal => stringVal.IsNotNull(), true);
            TestExpression<string, bool>(stringVal => ((object)null).IsNull(), true);
            TestExpression<string, bool>(stringVal => stringVal.IsNull(), false);
            TestExpression<string, bool>(stringVal => stringVal.IsNullOrEmpty(), false);
            TestExpression<string, bool>(stringVal => "".IsNullOrEmpty(), true);
            TestExpression<string, bool>(stringVal => ((string)"").IsNullOrEmpty(), true);

            TestExpression<string, string>(stringVal => stringVal.ToUpper(), "ABCDEF");
            TestExpression<string, int>(stringVal => stringVal[0], 0x61);
            TestExpression<string, bool>(stringVal => stringVal[0] == 'a', true);
            TestExpression<string, bool>(stringVal => stringVal[0] == 0x61, true);

            TestExpression<string, string>(stringVal1 => stringVal1.Trim().ToLower(), "abc");
            TestExpression<string, bool>(stringVal => stringVal.StartsWith("abc"), true);
            TestExpression<string, bool>(stringVal => stringVal.StartsWith("ABC"), false);
            TestExpression<string, bool>(stringVal => stringVal.StartsWith("ABC", StringComparison.Ordinal), false);
            TestExpression<string, bool>(stringVal => stringVal.StartsWith("ABC", StringComparison.OrdinalIgnoreCase), true);
            TestExpression<string, bool>(stringVal => stringVal.Contains("bc"), true);
            TestExpression<string, bool>(stringVal => stringVal.Contains("BC"), false);
            TestExpression<string, string>(stringVal => stringVal.Substring(1), "bcdef");
            TestExpression<string, string>(stringVal => stringVal.Substring(1, 2), "bc");
            TestExpression<string, int>(stringVal => stringVal.IndexOf("bc"), 1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("bc", StringComparison.Ordinal), 1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("BC"), -1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("BC", StringComparison.Ordinal), -1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("BC", StringComparison.OrdinalIgnoreCase), 1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("BC", 1, StringComparison.Ordinal), -1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("BC", 1, StringComparison.OrdinalIgnoreCase), 1);
            TestExpression<string, int>(stringVal => stringVal.IndexOf("BC", 2, StringComparison.OrdinalIgnoreCase), -1);
        }

        [Test]
        public void TestChar()
        {
            TestExpression<string, bool>(stringVal2 => char.IsUpper(stringVal2[0]), true);
            TestExpression<string, bool>(stringVal2 => char.IsUpper(stringVal2[1]), false);
            TestExpression<string, bool>(stringVal2 => char.IsLower(stringVal2[0]), false);
            TestExpression<string, bool>(stringVal2 => char.IsLower(stringVal2[1]), true);
            TestExpression<string, bool>(stringVal2 => char.IsLetter(stringVal2[0]), true);
            TestExpression<string, bool>(stringVal2 => char.IsLetter(stringVal2[1]), true);
            TestExpression<string, bool>(stringVal2 => char.IsLetter(stringVal2[2]), false);
            TestExpression<string, bool>(stringVal2 => char.IsDigit(stringVal2[2]), true);
            TestExpression<string, bool>(stringVal2 => char.IsLetterOrDigit(stringVal2[1]), true);
            TestExpression<string, bool>(stringVal2 => char.IsLetterOrDigit(stringVal2[2]), true);
            TestExpression<string, bool>(stringVal2 => char.IsLetterOrDigit(stringVal2[3]), false);
            TestExpression<string, bool>(stringVal2 => char.IsPunctuation(stringVal2[3]), true);
            TestExpression<string, bool>(stringVal2 => char.IsWhiteSpace(stringVal2[4]), true);
        }

        [Test]
        public void TestLinq()
        {
            TestExpression<string, bool>(stringVal2 => stringVal2.Any(c => char.IsLower(c)), true);
            TestExpression<string, bool>(stringVal2 => stringVal2.Any(c => char.IsUpper(c)), true);
            TestExpression<string, bool>(stringVal2 => stringVal2.All(c => char.IsLower(c)), false);
            TestExpression<string, bool>(stringVal2 => stringVal2.All(c => char.IsUpper(c)), false);
            TestExpression<string, int>(stringVal2 => stringVal2.Count(c => char.IsUpper(c)), 1);
            TestExpression<string, int>(stringVal2 => stringVal2.Count(c => !char.IsUpper(c)), 4);
            TestExpression<string, int>(stringVal2 => stringVal2.Count(), 5);

            TestExpression<string, bool>(stringVal2 => stringVal2.Substring(0, 1).All(c => char.IsUpper(c)), true);
            TestExpression<string, bool>(stringVal2 => stringVal2.Substring(0, 2).All(c => char.IsUpper(c) || char.IsLower(c)), true);

            TestExpression<int[], bool>(intArr => intArr.All(v => v > 0), true);
            TestExpression<int[], bool>(intArr => intArr.Any(v => v == 10), true);
            TestExpression<int[], bool>(intArr => intArr.Any(v => v == 10), true);
        }

        [Test]
        public void TestRegexp()
        {
            TestExpression<string, bool>(stringVal => Regex.IsMatch(stringVal, "^a.+"), true);
            TestExpression<string, bool>(stringVal => Regex.IsMatch(stringVal, "^A.+"), false);
            TestExpression<string, bool>(stringVal => Regex.IsMatch(stringVal, "^A.+", RegexOptions.IgnoreCase), true);
            TestExpression<string, bool>(stringVal => mRe.IsMatch(stringVal), true);
            TestExpression<string, bool>(stringVal => Regex.IsMatch(stringVal, "^e.+"), false);
            TestExpression<Entity, bool>(entityVal => Regex.IsMatch(entityVal.stringProp, "^e.+"), true);
            TestExpression<string, bool>(entityVal => Functions.IsCreditCardNumberCorrect("4444333322221111"), true);
            TestExpression<string, bool>(entityVal => Functions.IsCreditCardNumberCorrect("4444 3333 2222-1111"), true);
            TestExpression<string, bool>(entityVal => Functions.IsCreditCardNumberCorrect("378282246310005"), true);
        }

        [Test]
        public void TestAccess()
        {
            TestExpression<Entity, int>(entityVal => entityVal.arrayProp[1], 2);
            TestExpression<Entity, int>(entityVal => entityVal.arrayProp.Length, 3);
            TestExpression<int[], int>(intArr => intArr.Length, 5);
            TestExpression<int[], int>(intArr => intArr[2], 30);
            TestExpression<int[], double>(intArr => intArr.Length / 2, 2.5);
            TestExpression<int[], int>(intArr => intArr[intArr.Length / 2], 30);
            TestExpression<Entity, int>(entityVal => entityVal.SubEntity.subIntProp, 515);
            TestExpression<Entity, int>(entityVal => entityVal.arrayProp[entityVal.SubEntity.index], 3);
        }

        [Test]
        public void TestDateTime()
        {
            TestExpression<DateTime, DateTime>(d => DateTime.Now, DateTime.UtcNow);
            TestExpression<DateTime, int>(dateTime1 => dateTime1.Year, 2002);
            TestExpression<DateTime, int>(dateTime1 => dateTime1.Month, 1);
            TestExpression<DateTime, int>(dateTime1 => dateTime1.Day, 31);
            TestExpression<DateTime, int>(dateTime1 => dateTime1.Hour, 12);
            TestExpression<DateTime, int>(dateTime1 => dateTime1.Minute, 30);
            TestExpression<DateTime, bool>(dateTime1 => dateTime1.DayOfWeek == DayOfWeek.Thursday, true);
            TestExpression<DateTime, DateTime>(dateTime1 => dateTime1.AddDays(30), new DateTime(2002, 3, 2, 12, 30, 18));
            TestExpression<DateTime, DateTime>(dateTime1 => dateTime1.AddHours(-1.5), new DateTime(2002, 1, 31, 11, 0, 18));
            DateTime dateTime2 = new DateTime(2008, 1, 31, 13, 35, 18);

            TestExpression<DateTime, double>(dateTime1 => Math.Floor(Math.Abs(Functions.YearsSince(dateTime2, dateTime1))), 6);
            TestExpression<DateTime, double>(dateTime1 => Math.Floor(Math.Abs(Functions.MonthsSince(dateTime2, dateTime1))), 72);
            dateTime2 = new DateTime(2002, 3, 1, 0, 0, 0);
            TestExpression<DateTime, double>(dateTime1 => Math.Floor(Functions.DaysSince(dateTime2, dateTime1)), 28);
        }

        public class Constants
        {
            public int Five { get; set; } = 5;
        }

        private readonly Constants mConstants = new Constants();

        [Test]
        public void TestConvertors()
        {
            TestExpression<string, bool>(stringVal3 => Functions.ToBool(stringVal3), true);
            TestExpression<string, bool?>(stringVal => Functions.ToBool(stringVal), null);
            TestExpression<string, int>(stringVal4 => Functions.ToInt(stringVal4), 50);
            TestExpression<string, bool>(stringVal4 => Functions.ToInt(stringVal4) > 70, false);
            TestExpression<string, bool>(stringVal4 => Functions.ToInt(stringVal4) + 25 > 70, true);
            TestExpression<string, bool>(stringVal4 => Functions.ToInt(stringVal4) + mConstants.Five * 5 > 70, true);
        }

        [Test]
        public void TestEquality()
        {
            Expression<Func<string, int>> f1 = s => s.Length;
            Expression<Func<string, int>> f2 = s => s.Length;

            ExpressionCompiler c11 = new ExpressionCompiler(f1);
            ExpressionCompiler c12 = new ExpressionCompiler(f1);

            Assert.IsTrue(c12.Equals(c11));
            Assert.IsTrue(c12.Equals(f1));
            Assert.IsFalse(c12.Equals(f2));

            ExpressionCompiler c2 = new ExpressionCompiler(f2);
            Assert.IsTrue(c2.Equals(f2));
            Assert.IsFalse(c2.Equals(f1));
            Assert.IsFalse(c2.Equals(c11));
        }

        [Test]
        public void TestComparison()
        {
            TestExpression<int, bool>(intVal => intVal == 10, true);
            TestExpression<int, bool>(intVal => intVal != 10, false);
            TestExpression<int, bool>(intVal => intVal >= 10, true);
            TestExpression<int, bool>(intVal => intVal > 10, false);
            TestExpression<int, bool>(intVal => intVal <= 10, true);
            TestExpression<int, bool>(intVal => intVal < 10, false);

            TestExpression<int, bool>(intVal => !(intVal == 10), false);

            TestExpression<int, bool>(intVal => intVal == 10 && true, true);
            TestExpression<int, bool>(intVal => intVal != 10 && true, false);
            TestExpression<int, bool>(intVal => intVal == 10 || true, true);
            TestExpression<int, bool>(intVal => intVal != 10 || true, true);

            TestExpression<int, int>(intVal => intVal == 10 ? intVal * 30 : intVal / 2, 300);
            TestExpression<int, int>(intVal => intVal != 10 ? intVal * 30 : intVal / 2, 5);
        }

        [Test]
        public void TestCustomValidation()
        {
            ValidationExpressionCompiler compiler;

            Expression<Func<Entity, bool>> entityExpression = e => e.arrayProp.Length > 1;
            compiler = new ValidationExpressionCompiler(entityExpression, entityParameterIndex: 0);
            Assert.AreEqual("jsv_greater(jsv_length(reference('arrayProp')), 1)", compiler.JavaScriptExpression);

            const int i = 1;
            entityExpression = e => e.SubEntity.index < 4 + i;
            compiler = new ValidationExpressionCompiler(entityExpression, entityParameterIndex: 0);
            Assert.AreEqual("jsv_less(reference('SubEntity.index'), 5)", compiler.JavaScriptExpression);

            entityExpression = e => (2 + 2 + (int)Math.PI) == 7;
            compiler = new ValidationExpressionCompiler(entityExpression, entityParameterIndex: 0);
            Assert.AreEqual("true", compiler.JavaScriptExpression);
        }

        [Test]
        public void TestTimeSpan()
        {
            DateTime dt = new DateTime(2002, 1, 31, 12, 30, 19), dt1;
            TestExpression<DateTime, bool>(dateTime1 => Math.Abs(1 - (dt - dateTime1).TotalSeconds) <= 0.001, true);
            dt = new DateTime(2002, 1, 31, 13, 30, 19);
            TestExpression<DateTime, bool>(dateTime1 => (dt - dateTime1).TotalHours >= 1, true);
            TestExpression<DateTime, bool>(dateTime1 => (dt - dateTime1).TotalDays >= 1, false);
            TestExpression<DateTime, bool>(dateTime1 => (dt - dateTime1).TotalDays >= (1.0 / 24.0), true);
            dt = new DateTime(2002, 2, 1, 12, 30, 19);
            TestExpression<DateTime, bool>(dateTime1 => (dt - dateTime1).TotalDays >= 1, true);

            dt = new DateTime(2002, 1, 30, 12, 30, 18);
            dt1 = new DateTime(2005, 1, 1, 0, 0, 0);

            TestExpression<DateTime, int>(dateTime1 => (int)(dt - dt1).TotalMilliseconds, (int)(dt - dt1).TotalMilliseconds);
            TestExpression<DateTime, DateTime>(dateTime1 => dt1 + (dt - dateTime1), new DateTime(2004, 12, 31, 0, 0, 0));
        }
    }
}