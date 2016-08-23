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
using System.ComponentModel;
using System.Globalization;

namespace TimelineLibrary.Data
{
    public class DateTimeConverter: TypeConverter
    {
        public const string                            DEFAULT_CULTUREID = "en-US";

        public static CultureInfo                      CultureInfo = new CultureInfo(DEFAULT_CULTUREID);
        private static string                          m_cultId = DEFAULT_CULTUREID;

        public static string CultureId
        {
            get
            {
                return m_cultId;
            }
            set
            {
                CultureInfo = new CultureInfo(value);
                m_cultId = value;
            }
        }

        public override bool CanConvertFrom(
            ITypeDescriptorContext                      context, 
            Type                                        sourceType
        )
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(
            ITypeDescriptorContext                      context, 
            Type                                        destinationType
        )
        {   
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(
            ITypeDescriptorContext                      context, 
            CultureInfo                                 culture, 
            object                                      value, 
            Type                                        destinationType
        )
        {
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext                      context, 
            CultureInfo                                 culture, 
            object                                      value
        )
        {
            if (value.GetType() == typeof(string))
            {
                return ParseDateTime((string) value);
            }
            return base.ConvertFrom(context, culture, value);
        }


        /// 
        /// <summary>
        /// Parse initial date passed from xaml, could be datetime string or 
        /// 'today', 'now', 'yesterday', etc. constants.</summary>
        /// 
        /// 
        public static DateTime ParseDateTime(
            string                                      initialTime
        )
        {
            DateTime                                    initTime;

            switch(initialTime.ToLower())
            {
                case "tomorrow":
                    initTime = DateTime.Today.AddDays(1);
                    break;

                case "today":
                    initTime = DateTime.Today;
                    break;

                case "yesterday":
                    initTime = DateTime.Today.AddDays(-1);
                    break;

                case "now":
                    initTime = DateTime.Now;
                    break;

                default:
                    initTime = DateTime.Parse(initialTime, DateTimeConverter.CultureInfo);
                    break;
            }

            return initTime;
        }

    }
}
