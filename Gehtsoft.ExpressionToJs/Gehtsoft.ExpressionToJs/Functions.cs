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
            return new TimeSpan(((DateTime)args0).Date.Subtract(((DateTime)args1).Date).Ticks).Days;
        }

        public static double MonthsSince(DateTime args0, DateTime args1)
        {
            var diff = new DateTime(Math.Abs(((DateTime)args0).Date.Subtract(((DateTime)args1).Date).Ticks), DateTimeKind.Unspecified);
            return (DateTime)args0 > (DateTime)args1 ? (diff.Year - 1) * 12 + (diff.Month - 1) : -((diff.Year - 1) * 12 + (diff.Month - 1));
        }

        public static double YearsSince(DateTime args0, DateTime args1)
        {
            var diff = new DateTime(Math.Abs(((DateTime)args0).Date.Subtract(((DateTime)args1).Date).Ticks), DateTimeKind.Unspecified);
            return (DateTime)args0 > (DateTime)args1 ? diff.Year - 1 : -(diff.Year - 1);
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
                    digitValue = digitValue * 2;

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
                if (string.Compare(sv, "true", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(sv, "yes", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(sv, "1", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(sv, "on", StringComparison.OrdinalIgnoreCase) == 0)
                    return true;

                if (string.Compare(sv, "false", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(sv, "no", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(sv, "0", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(sv, "off", StringComparison.OrdinalIgnoreCase) == 0)
                    return false;

                throw new ArgumentException($"Wrong value {v.ToString()}", nameof(v));
            }

            throw new ArgumentException($"Wrong value type {v.GetType().ToString()}", nameof(v));
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

            throw new ArgumentException($"Wrong value type {v.GetType().ToString()}", nameof(v));
        }

        public static bool IsNull(this object v) => v == null;

        public static bool IsNullOrEmpty(this string v) => v == null || v.Length == 0;

        public static bool IsNotNull(this object v) => v != null;

        public static bool IsNotNullOrEmpty(this string v) => !(v == null || v.Length == 0);
    }
}