using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// Extensions to the date/time class
    /// </summary>
    public static class DateExtensions
    {
        /// <summary>
        /// Truncates date to whole seconds
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime TruncateToSeconds(this DateTime dateTime)
            => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);

        /// <summary>
        /// Truncates date to whole minutes
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime TruncateToMinutes(this DateTime dateTime)
            => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);

        /// <summary>
        /// Truncates date to whole hours
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>

        public static DateTime TruncateToHours(this DateTime dateTime)
            => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);

        /// <summary>
        /// Truncates date to days
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>

        public static DateTime TruncateTime(this DateTime dateTime)
            => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
    }
}
