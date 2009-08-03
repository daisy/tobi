﻿// (c) Copyright Microsoft Corporation.
// This source is subject to [###LICENSE_NAME###].
// Please see [###LICENSE_LINK###] for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Windows.Controls
{
    // NOTICE: This date time helper assumes it is working in a Gregorian calendar
    //         If we ever support non Gregorian calendars this class would need to be redesigned
    internal static class DateTimeHelper
    {
        private static System.Globalization.Calendar cal = new GregorianCalendar();

        public static DateTime? AddDays(DateTime time, int days)
        {
            try
            {
                return cal.AddDays(time, days);
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        public static DateTime? AddMonths(DateTime time, int months)
        {
            try
            {
                return cal.AddMonths(time, months);
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        public static DateTime? AddYears(DateTime time, int years)
        {
            try
            {
                return cal.AddYears(time, years);
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        public static DateTime? SetYear(DateTime date, int year)
        {
            return DateTimeHelper.AddYears(date, year - date.Year);                        
        }

        public static DateTime? SetYearMonth(DateTime date, DateTime yearMonth)
        {
            DateTime? target = SetYear(date, yearMonth.Year);
            if (target.HasValue)
            {
                target = DateTimeHelper.AddMonths(target.Value, yearMonth.Month - date.Month);
            }

            return target;
        }

        public static int CompareDays(DateTime dt1, DateTime dt2)
        {
            return DateTime.Compare(DiscardTime(dt1).Value, DiscardTime(dt2).Value);
        }

        public static int CompareYearMonth(DateTime dt1, DateTime dt2)
        {
            return ((dt1.Year - dt2.Year) * 12) + (dt1.Month - dt2.Month);
        }

        public static int DecadeOfDate(DateTime date)
        {
            return date.Year - (date.Year % 10);
        }

        public static DateTime DiscardDayTime(DateTime d)
        {
            return new DateTime(d.Year, d.Month, 1, 0, 0, 0);
        }

        public static DateTime? DiscardTime(DateTime? d)
        {
            if (d == null)
            {
                return null;
            }

            return d.Value.Date;
        }

        public static int EndOfDecade(DateTime date)
        {
            return DecadeOfDate(date) + 9;
        }

        public static DateTimeFormatInfo GetCurrentDateFormat()
        {
            if (CultureInfo.CurrentCulture.Calendar is GregorianCalendar)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat;
            }
            else
            {
                foreach (System.Globalization.Calendar cal in CultureInfo.CurrentCulture.OptionalCalendars)
                {
                    if (cal is GregorianCalendar)
                    {
                        // if the default calendar is not Gregorian, return the first supported GregorianCalendar dtfi
                        DateTimeFormatInfo dtfi = new CultureInfo(CultureInfo.CurrentCulture.Name).DateTimeFormat;
                        dtfi.Calendar = cal;
                        return dtfi;
                    }
                }

                // if there are no GregorianCalendars in the OptionalCalendars list, use the invariant dtfi
                DateTimeFormatInfo dt = new CultureInfo(CultureInfo.InvariantCulture.Name).DateTimeFormat;
                dt.Calendar = new GregorianCalendar();
                return dt;
            }
        }

        // returns if the date is included in the range
        public static bool InRange(DateTime date, CalendarDateRange range)
        {
            return InRange(date, range.Start, range.End);
        }

        // returns if the date is included in the range
        public static bool InRange(DateTime date, DateTime start, DateTime end)
        {
            Debug.Assert(DateTime.Compare(start, end) < 1);

            if (CompareDays(date, start) > -1 && CompareDays(date, end) < 1)
            {
                return true;
            }

            return false;
        }

        public static string ToDayString(DateTime? date)
        {
            string result = string.Empty;
            DateTimeFormatInfo format = GetCurrentDateFormat();

            if (date.HasValue && format != null)
            {
                result = date.Value.Day.ToString(format);
            }

            return result;
        }

        public static string ToDecadeRangeString(int decade)
        {
            string result = string.Empty;
            DateTimeFormatInfo format = GetCurrentDateFormat();

            if (format != null)
            {
                int decadeEnd = decade + 9;
                result = decade.ToString(format) + "-" + decadeEnd.ToString(format);
            }

            return result;
        }

        public static string ToYearMonthPatternString(DateTime? date)
        {
            string result = string.Empty;
            DateTimeFormatInfo format = GetCurrentDateFormat();

            if (date.HasValue && format != null)
            {
                result = date.Value.ToString(format.YearMonthPattern, format);
            }

            return result;
        }

        public static string ToYearString(DateTime? date)
        {
            string result = string.Empty;
            DateTimeFormatInfo format = GetCurrentDateFormat();

            if (date.HasValue && format != null)
            {
                result = date.Value.Year.ToString(format);
            }

            return result;
        }

        public static string ToAbbreviatedMonthString(DateTime? date)
        {
            string result = string.Empty;
            DateTimeFormatInfo format = GetCurrentDateFormat();

            if (date.HasValue && format != null)
            {
                string[] monthNames = format.AbbreviatedMonthNames;
                if (monthNames != null && monthNames.Length > 0)
                {
                    result = monthNames[(date.Value.Month - 1) % monthNames.Length];
                }
            }

            return result;
        }

        public static string ToLongDateString(DateTime? date)
        {
            string result = string.Empty;
            DateTimeFormatInfo format = GetCurrentDateFormat();

            if (date.HasValue && format != null)
            {
                result = date.Value.Date.ToString(format.LongDatePattern, format);
            }

            return result;
        }
    }
}