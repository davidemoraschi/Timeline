/*
 *   Copyright 2009 Andrew Syrov <asyrovprog@live.com>
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU Library General Public License as
 *   published by the Free Software Foundation; either version 2 or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details
 *
 *   You should have received a copy of the GNU Library General Public
 *   License along with this program; if not, write to the
 *   Free Software Foundation, Inc.,
 *   51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *   
 */
using System;
using System.Net;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace TimelineLibrary
{
    public enum TimelineCalendarType
    {
        Decades,
        Years,
        Months,
        Days,
        Hours,
        Minutes10,
        Minutes
    }

    /// 
    /// <summary>
    /// Time calculator used by timeline bands</summary>
    /// 
    public class TimelineCalendar
    {
        private TimelineCalendarType                    m_type;
        private static TimeSpan                         TICK = new TimeSpan((long)1);
        private Calendar                                m_calendar;  
        private DateTime                                m_minDate;
        private DateTime                                m_maxDate;
    
        public TimelineCalendar(             
            string                                      cultureCalendar,
            TimelineCalendarType                        itemType,
            DateTime                                    minDateTime,
            DateTime                                    maxDateTime
        )
        {
            m_type = itemType;
            m_calendar = CalendarFromString(cultureCalendar);
            m_minDate = minDateTime;
            m_maxDate = maxDateTime;
        }

        public DateTime MinDateTime
        {
            get
            {
                return m_minDate;
            }
        }

        public DateTime MaxDateTime
        {
            get
            {
                return m_maxDate;
            }
        }

        public TimelineCalendarType LineType
        {
            get
            {
                return m_type;
            }
        }

        public Calendar Calendar
        {
            get
            {
                return m_calendar;
            }
        }

        public long IndexOf(
            DateTime                                    value
            )
        {
            DateTime                                    d;
            long                                        ret;
            TimeSpan                                    span;
            DateTime                                    start;

            Debug.Assert(value != null);

            d = (DateTime) value;
            start = DateTime.MinValue;
            span = d - start;

            switch (m_type)
            {
                case TimelineCalendarType.Decades:
                    ret = d.Year / 10 - start.Year / 10;
                    break;

                case TimelineCalendarType.Years:
                    ret = d.Year - start.Year;
                    break;

                case TimelineCalendarType.Months:
                    ret = d.Year * 12 + d.Month - start.Year * 12 - start.Month;
                    break;

                case TimelineCalendarType.Days:
                    ret = (long) span.TotalDays;
                    break;

                case TimelineCalendarType.Hours:
                    ret = (long) span.TotalHours;
                    break;

                case TimelineCalendarType.Minutes10:
                    ret = (long) (span.TotalMinutes / 10);
                    break;

                case TimelineCalendarType.Minutes: 
                    ret = (long) span.TotalMinutes;
                    break;

                default:
                   throw new ArgumentOutOfRangeException();
            }
            
            return ret;
        }
     
        public DateTime this[
            long                                        index
        ]
        {
            get
            {
                DateTime                                ret;
                DateTime                                start;
                DateTime                                end;

                Debug.Assert(index > -1);

                start = DateTime.MinValue;
                end = DateTime.MaxValue;

                switch (m_type)
                {
                    case TimelineCalendarType.Decades:
                        ret = start.AddYears((int) index * 10);
                        break;

                    case TimelineCalendarType.Years:
                        ret = start.AddYears((int) index);
                        break;

                    case TimelineCalendarType.Months:
                        ret = start.AddMonths((int) index);
                        break;

                    case TimelineCalendarType.Days:
                        ret = start.AddDays(index);
                        break;

                    case TimelineCalendarType.Hours:
                        ret = start.AddHours(index);
                        break;

                    case TimelineCalendarType.Minutes10:
                        ret = start.AddMinutes(index * 10);
                        break;

                    case TimelineCalendarType.Minutes: 
                        ret = start.AddMinutes(index);
                        break;

                    default:
                       throw new ArgumentOutOfRangeException();
                }
                
                return ret;
            }
        }

        public static string ItemToString(
            TimelineCalendar                            src,
            DateTime                                    value
            )
        {
            DateTime                                    d;
            string                                      ret;
            TimelineCalendarType                        type;

            Debug.Assert(src as TimelineCalendar != null);
            Debug.Assert(value != null);

            d = (DateTime) value;
            type = ((TimelineCalendar) src).LineType;

            switch (type)
            {
                case TimelineCalendarType.Decades:
                    ret = ((d.Year / 10) * 10).ToString();
                    break;

                case TimelineCalendarType.Years:
                    ret = d.Year.ToString();
                    break;

                case TimelineCalendarType.Months:
                    if (d.Month != 1)
                    {
                        ret = d.ToString("MMM");
                    }
                    else
                    {
                        ret = d.ToString("MMM yyyy");
                    }
                    break;

                case TimelineCalendarType.Days:
                    ret = d.Day.ToString();
                    if (d.Day == 1)
                    {
                        ret += d.ToString(" MMM");

                        if (d.Month == 1)
                        {
                            ret += d.ToString(" yyyy");
                        }
                    }
                    break;

                case TimelineCalendarType.Hours:
                    ret = d.ToString("hh tt");
                    break;

                case TimelineCalendarType.Minutes10:
                    if (d.Minute == 0)
                    {
                        ret = d.Hour.ToString() + ":00";
                    }
                    else 
                    {
                        ret = ((d.Minute / 10) * 10).ToString();
                    }
                    break;

                case TimelineCalendarType.Minutes: 
                    ret = d.ToString("mm");
                    break;

                default:
                   throw new ArgumentOutOfRangeException();
            }
            
            return ret;
        }

        public DateTime GetFloorTime(
            DateTime                                    dt
        )
        {
            DateTime                                    date;
            int                                         year;

            date = dt;

            switch (m_type)
            {
                case TimelineCalendarType.Minutes10:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 
                        date.Minute - date.Minute % 10, 0, m_calendar); 
                    break;

                case TimelineCalendarType.Minutes: 
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 
                        0, m_calendar);
                    break;

                case TimelineCalendarType.Hours:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, 
                        m_calendar);
                    break;

                case TimelineCalendarType.Days:
                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 
                        m_calendar);
                    break;

                case TimelineCalendarType.Months:
                    date = new DateTime(date.Year, date.Month, 1, 0, 0, 0, 
                        m_calendar);
                    break;

                case TimelineCalendarType.Years:
                    date = new DateTime(date.Year, 1, 1, 0, 0, 0, m_calendar);
                    break;

                case TimelineCalendarType.Decades:
                    year = Math.Max(DateTime.MinValue.Year, date.Year - date.Year % 10);
                    date = new DateTime(year, 1, 1, 0, 0, 0, m_calendar);
                    break;

                default:
                   throw new ArgumentOutOfRangeException();
            }

            return date;
        }

        public DateTime GetCeilingTime(
            DateTime                                    dt
        )
        {
            long                                        index;
            DateTime                                    ret;

            Debug.Assert(dt != null); 

            index = IndexOf(dt);
            if (index >= IndexOf(DateTime.MaxValue))
            {
                ret = MaxDateTime;
            }
            else
            {
                ret = this[IndexOf(dt) + 1] - TICK;
            }

            if (ret > DateTime.MaxValue)
            {
                ret = MaxDateTime;
            }

            return ret;
        }

        public static Calendar CalendarFromString(
            string                                      name
        )
        {
            Calendar                                    c;

            if (name == null)
            {
                name = String.Empty;
            }

            switch (name.ToLower())
            {
                case "hebrew":
                    c = new HebrewCalendar();
                    break;

                case "hijri":
                    c = new HijriCalendar();
                    break;

                case "japanese":
                    c = new JapaneseCalendar();
                    break;

                case "korean":
                    c = new KoreanCalendar();
                    break;

                case "taiwan":
                    c = new TaiwanCalendar();
                    break;

                case "thaibuddhist":
                    c = new ThaiBuddhistCalendar();
                    break;

                case "umalqura":
                    c = new UmAlQuraCalendar();
                    break;

                default:
                    c = new GregorianCalendar();
                    break;
            }

            return c;
        }
    }
}

