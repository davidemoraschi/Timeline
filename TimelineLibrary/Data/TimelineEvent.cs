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
using System.Reflection;
using System.Windows.Media;

namespace TimelineLibrary
{

    /// 
    /// <summary>
    /// we need this so that every timeline band can have it's own instance and 
    /// therefore different brush and event top coordinates and width</summary>
    /// 
    public class TimelineDisplayEvent: TimelineEvent
    {
        private Brush                                   m_rectBrush;
        
        public static string                            MoreLinkText;
        public static int                               TeaserSize;

        public TimelineDisplayEvent(
            TimelineEvent                               e
        )
        {
            Title = e.Title;
            IsDuration = e.IsDuration;
            StartDate = e.StartDate;
            EndDate = e.EndDate;
            Description = FixDescription(e.Description);
            EventColor = e.EventColor;
            Row = e.Row;
            Link = e.Link;
            EventImage = e.EventImage;
            TeaserEventImage = e.TeaserEventImage;
            RowOverride = e.RowOverride;

            if (TeaserSize > 0 && Description.Length > TeaserSize)
            {
                Teaser = Description.Substring(0, TeaserSize) + "...";
                LinkText = MoreLinkText;
            }
            else
            {
                Teaser = Description;
                LinkText = String.Empty;
            }
        }

        public Brush EventColorBrush
        {
            get
            {
                Type                                    colorType;
                object                                  o;

                if (m_rectBrush == null)
                {
                    m_rectBrush = new SolidColorBrush(Colors.Gray);

                    if (!String.IsNullOrEmpty(EventColor))
                    {
                        colorType = typeof(System.Windows.Media.Colors); 

                        if (colorType.GetProperty(EventColor) != null)
                        {
                            o = colorType.InvokeMember(EventColor, BindingFlags.GetProperty, null, null, null);
                            if (o != null)
                            {
                                m_rectBrush = new SolidColorBrush((Color) o);
                            }
                        }
                    }
                }

                return m_rectBrush;
            }
            set
            {
                m_rectBrush = value;
            }
        }

        public string Teaser
        {
            get;
            set;
        }

        public string LinkText
        {
            get;
            set;
        }

        public double Top
        {
            get;
            set;
        }
        
        public double EventPixelWidth
        {
            get;
            set;
        }

        public string StartEndDateTime
        {
            get
            {
                string                                  ret;

                if (IsDuration)
                {
                    ret = String.Format(Resource.DateFromToFormat, StartDate, EndDate);
                }
                else
                {
                    ret = StartDate.ToString();
                }

                return ret;
            }
        }

        public string FixDescription(
            string                                      d
        )
        {
            return d.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ").Trim();
        }
    }


    /// 
    /// <summary>
    /// Class that represents event element read from xml file</summary>
    /// 
    public class TimelineEvent
    {
        private int                                     m_row = -1;

        public string Id
        {
            get;
            set;
        }
        
        public string Title
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }
        
        public DateTime StartDate
        {
            get;
            set;
        }

        public DateTime EndDate
        {
            get;
            set;
        }

        public string Link
        {
            get;
            set;
        }

        public bool IsDuration
        {
            get;
            set;
        }

        public string EventImage
        {
            get;
            set;
        }

        public string TeaserEventImage
        {
            get;
            set;
        }

        public string EventColor
        {
            get;
            set;
        }

        public int RowOverride
        {
            get;
            set;
        }

        public int Row
        {
            get
            {
                return m_row;
            }
            set
            {
                m_row = value;
            }
        }
    }
}
