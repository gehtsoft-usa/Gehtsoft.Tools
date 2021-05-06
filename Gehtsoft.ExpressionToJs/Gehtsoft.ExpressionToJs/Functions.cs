using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Gehtsoft.ExpressionToJs
{
    public static class Functions
    {
        public static double Fractional(double value) => Math.Abs(value) - Math.Floor(Math.Abs(value));

        public static double DaysSince(DateTime args0, DateTime args1)
        {
            return new TimeSpan(args0.Date.Subtract(args1.Date).Ticks).Days;
        }

        public static double MonthsSince(DateTime args0, DateTime args1)
        {
            var diff = new DateTime(Math.Abs(args0.Date.Subtract(args1.Date).Ticks), DateTimeKind.Unspecified);
            return args0 > args1 ? (diff.Year - 1) * 12 + (diff.Month - 1) : -((diff.Year - 1) * 12 + (diff.Month - 1));
        }

        public static double YearsSince(DateTime args0, DateTime args1)
        {
            var diff = new DateTime(Math.Abs(args0.Date.Subtract(args1.Date).Ticks), DateTimeKind.Unspecified);
            return args0 > args1 ? diff.Year - 1 : -(diff.Year - 1);
        }

        public static bool IsCreditCardNumberCorrect(string value)
        {
            if (value == null)
                return false;

            int checksum = 0;
            bool evenDigit = false;
            char[] digits = value.ToCharArray();

            for (int j = digits.Length - 1; j >= 0; j--)
            {
                char digit = digits[j];
                if (!char.IsDigit(digit))
                {
                    if (digit == ' ' || digit == '-')
                        continue;
                    else
                        return false;
                }

                int digitValue = (digit - '0');

                if (evenDigit)
                    digitValue *= 2;

                evenDigit = !evenDigit;

                while (digitValue > 0)
                {
                    checksum += digitValue % 10;
                    digitValue /= 10;
                }
            }
            return (checksum % 10) == 0;
        }

        public static bool ToBool(object v)
        {
            if (v == null)
                return false;

            if (v is bool bv)
                return bv;

            if (v is string sv)
            {
                if (string.Equals(sv, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sv, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sv, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sv, "on", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(sv, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sv, "no", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sv, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sv, "off", StringComparison.OrdinalIgnoreCase))
                    return false;

                throw new ArgumentException($"Wrong value {v}", nameof(v));
            }

            throw new ArgumentException($"Wrong value type {v.GetType()}", nameof(v));
        }

        public static int ToInt(object v)
        {
            if (v == null)
                return 0;

            if (v is int iv)
                return iv;

            if (v is string sv)
            {
                return Int32.Parse(sv);
            }

            throw new ArgumentException($"Wrong value type {v.GetType()}", nameof(v));
        }

        public static bool IsNull(this object v) => v == null;

        public static bool IsNullOrEmpty(this string v) => string.IsNullOrEmpty(v);

        public static bool IsNotNull(this object v) => v != null;

        public static bool IsNotNullOrEmpty(this string v) => !string.IsNullOrEmpty(v);
    }
}