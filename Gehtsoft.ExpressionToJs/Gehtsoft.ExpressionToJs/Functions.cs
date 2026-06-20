using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Gehtsoft.ExpressionToJs
{
    /// <summary>
    /// Validation helpers you call from inside the lambdas you compile to JavaScript.
    ///
    /// Reach for these instead of an arbitrary BCL call whenever the operation you need has no
    /// direct, translatable JavaScript form (a Luhn check, a calendar-aware "months since", a
    /// lenient string-to-bool coercion, ...). Every method here is guaranteed to have a matching
    /// [c]jsv_*[/c] implementation in the embedded runtime stub, so an expression that uses them
    /// compiles to JavaScript [i]and[/i] runs unchanged as a server-side delegate - the single
    /// source of truth the library exists to provide. A plain BCL method with no such mapping
    /// throws at compile time, so prefer the helper here when one fits.
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// Gets the part of a number after the decimal point, for whole-number or decimal-place checks.
        ///
        /// Use it to reject values that must be whole ([c]Functions.Fractional(x) == 0[/c]) or to
        /// validate that a money amount has at most two decimal places.
        /// </summary>
        /// <param name="value">
        /// The number to inspect. The sign is ignored: the magnitude is taken first, so
        /// [c]-3.25[/c] yields [c]0.25[/c] exactly as [c]3.25[/c] does. An integral value yields
        /// [c]0[/c].
        /// </param>
        /// <returns>The non-negative fractional part, always in the range [0, 1).</returns>
        public static double Fractional(double value) => Math.Abs(value) - Math.Floor(Math.Abs(value));

        /// <summary>
        /// Counts whole calendar days between two dates, ignoring the time of day.
        ///
        /// Use it for "within N days" rules - expiry windows, cool-down periods, recent-activity
        /// checks - where only the calendar day matters and the time of day must not skew the count.
        /// </summary>
        /// <param name="args0">The reference (usually later) date; its time component is discarded.</param>
        /// <param name="args1">
        /// The baseline (usually earlier) date; its time component is discarded. The argument order
        /// sets the sign of the result: [c]args0[/c] after [c]args1[/c] gives a positive count,
        /// before it gives a negative count.
        /// </param>
        /// <returns>The signed whole number of calendar days from <paramref name="args1"/> to <paramref name="args0"/>.</returns>
        public static double DaysSince(DateTime args0, DateTime args1)
        {
            return new TimeSpan(args0.Date.Subtract(args1.Date).Ticks).Days;
        }

        /// <summary>
        /// Counts completed calendar months between two dates, ignoring partial months.
        ///
        /// Use it for age-in-months or tenure rules where a partial month should not count - the
        /// result advances only when a full calendar month has elapsed, not on a raw 30-day division.
        /// </summary>
        /// <param name="args0">The reference (usually later) date; its time component is discarded.</param>
        /// <param name="args1">
        /// The baseline (usually earlier) date; its time component is discarded. As with
        /// <see cref="DaysSince"/>, the argument order sets the sign of the result.
        /// </param>
        /// <returns>The signed whole number of completed calendar months between the two dates.</returns>
        public static double MonthsSince(DateTime args0, DateTime args1)
        {
            var diff = new DateTime(Math.Abs(args0.Date.Subtract(args1.Date).Ticks), DateTimeKind.Unspecified);
            return args0 > args1 ? (diff.Year - 1) * 12 + (diff.Month - 1) : -((diff.Year - 1) * 12 + (diff.Month - 1));
        }

        /// <summary>
        /// Counts completed calendar years between two dates - the usual way to check age.
        ///
        /// Use it for the canonical "must be at least N years old" rule
        /// ([c]Functions.YearsSince(DateTime.Today, birthDate) &gt;= 18[/c]): the count advances only
        /// on a completed calendar year, so it matches how age is reckoned in practice.
        /// </summary>
        /// <param name="args0">The reference (usually later) date, e.g. today; its time component is discarded.</param>
        /// <param name="args1">
        /// The baseline (usually earlier) date, e.g. a birth date; its time component is discarded.
        /// The argument order sets the sign of the result.
        /// </param>
        /// <returns>The signed whole number of completed calendar years between the two dates.</returns>
        public static double YearsSince(DateTime args0, DateTime args1)
        {
            var diff = new DateTime(Math.Abs(args0.Date.Subtract(args1.Date).Ticks), DateTimeKind.Unspecified);
            return args0 > args1 ? diff.Year - 1 : -(diff.Year - 1);
        }

        /// <summary>
        /// Validates a payment card number with the Luhn checksum, client and server alike.
        ///
        /// Use it to catch typos in a typed-in card number before submission. It confirms the number
        /// passes the Luhn check only; it does not verify the card exists or is active.
        /// </summary>
        /// <param name="value">
        /// The card number as entered. Embedded spaces and dashes are tolerated (so grouped input
        /// like [c]"4111 1111 1111 1111"[/c] validates); [c]null[/c] or any other non-digit
        /// character makes the result [c]false[/c].
        /// </param>
        /// <returns>[c]true[/c] if the digits satisfy the Luhn check, otherwise [c]false[/c].</returns>
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

        /// <summary>
        /// Coerces a loosely-typed field to a bool, accepting the common yes/no spellings.
        ///
        /// Use it to give a checkbox, text input, or query-string value definite boolean meaning
        /// inside a rule, rather than matching only the literal [c]"true"[/c]/[c]"false"[/c].
        /// </summary>
        /// <param name="v">
        /// The value to coerce. [c]null[/c] becomes [c]false[/c]; an actual <see cref="bool"/>
        /// passes through; a string is matched case-insensitively - [c]"true"/"yes"/"1"/"on"[/c]
        /// give [c]true[/c] and [c]"false"/"no"/"0"/"off"[/c] give [c]false[/c]. Any other string,
        /// or any other type, is rejected with an exception, so use it only where the input is known
        /// to be one of these shapes.
        /// </param>
        /// <returns>The boolean interpretation of <paramref name="v"/>.</returns>
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

        /// <summary>
        /// Converts a possibly-textual field to an int for use in numeric rules.
        ///
        /// Use it to bring a value that may arrive as text into integer arithmetic within a rule
        /// (e.g. comparing a typed-in quantity against a limit) without a separate parse step.
        /// </summary>
        /// <param name="v">
        /// The value to convert. [c]null[/c] becomes [c]0[/c]; an actual <see cref="int"/> passes
        /// through; a string is parsed as an integer and throws if it is not a valid one, so guard
        /// or pre-validate free text before relying on it. Other types are rejected.
        /// </param>
        /// <returns>The integer value of <paramref name="v"/>.</returns>
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

        /// <summary>
        /// Fluent, translatable stand-in for a null check.
        ///
        /// Use it ([c]model.Field.IsNull()[/c]) when you want the null check to read as part of the
        /// member chain and to survive translation to JavaScript.
        /// </summary>
        /// <param name="v">The value to test; the receiver of the extension call.</param>
        /// <returns>[c]true[/c] when <paramref name="v"/> is [c]null[/c].</returns>
        public static bool IsNull(this object v) => v == null;

        /// <summary>
        /// Fluent "required text" check - true when the string is missing or empty.
        ///
        /// Use it where a form treats both a missing value and an empty string as "not provided",
        /// which is usually what you want.
        /// </summary>
        /// <param name="v">The string to test. Both [c]null[/c] and [c]""[/c] yield [c]true[/c]; whitespace-only text does not.</param>
        /// <returns>[c]true[/c] when <paramref name="v"/> is [c]null[/c] or empty.</returns>
        public static bool IsNullOrEmpty(this string v) => string.IsNullOrEmpty(v);

        /// <summary>
        /// Fluent, translatable not-null check.
        ///
        /// Use it - the inverse of <see cref="IsNull"/> - typically to guard a further check, e.g.
        /// [c]model.Child.IsNotNull() &amp;&amp; model.Child.Age &gt; 0[/c].
        /// </summary>
        /// <param name="v">The value to test; the receiver of the extension call.</param>
        /// <returns>[c]true[/c] when <paramref name="v"/> is not [c]null[/c].</returns>
        public static bool IsNotNull(this object v) => v != null;

        /// <summary>
        /// Fluent "text was supplied" check - true when the string is non-empty.
        ///
        /// Use it - the inverse of <see cref="IsNullOrEmpty(string)"/> - to require a non-empty value.
        /// </summary>
        /// <param name="v">The string to test. Returns [c]false[/c] for both [c]null[/c] and [c]""[/c].</param>
        /// <returns>[c]true[/c] when <paramref name="v"/> is neither [c]null[/c] nor empty.</returns>
        public static bool IsNotNullOrEmpty(this string v) => !string.IsNullOrEmpty(v);
    }
}