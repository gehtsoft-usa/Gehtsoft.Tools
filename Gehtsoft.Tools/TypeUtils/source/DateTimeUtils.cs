using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public static class DateUtils
    {
        private static readonly DateTime gOABase = new DateTime(1899, 12, 30);

        public static double ToOleDate(this DateTime dt)
        {
            return dt.Subtract(gOABase).TotalDays;
        }

        public static DateTime FromOleDate(double oleDate)
        {
            return gOABase.AddDays(oleDate);
        }

        public static DateTime TruncateToMilliseconds(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);
        }

        public static DateTime TruncateToSeconds(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
        }

        public static DateTime TruncateToMinutes(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, 0, dt.Kind);
        }

        public static DateTime TruncateToHours(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, 0, dt.Kind);
        }

        public static DateTime TruncateTime(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, 0, dt.Kind);
        }

        public static DateTime BeginOfMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, 0, dt.Kind);
        }

        public static DateTime BeginOfYear(this DateTime dt)
        {
            return new DateTime(dt.Year, 1, 1, 0, 0, 0, 0, dt.Kind);
        }
    }
}

