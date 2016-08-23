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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace TimelineLibrary
{
    /// 
    /// <summary>
    /// Alignment of initial date on the screen</summary>
    /// 
    public enum DateAlignment
    {
        Left    = 0, 
        Right   = 1,
        Center  = 2
    }


    /// 
    /// <summary>
    /// This class privides mapping between screen coordinates/sizes and time and 
    /// timeline event placement</summary>
    /// 
    public class TimelineBuilder
    {
        private const double                            TOP_MARGIN = 5;

        // we need this margin so that events do not run over date/time column titles
        private const double                            BOTTOM_MARGIN = 5; 

        #region Private Fields

        private int                                     m_columnCount;
        private DateTime                                m_currDate;
        private TimelineCalendar                        m_timeline;
        private Dictionary<object, long>                m_columnIndexes;
        private long                                    m_minIndex;
        private long                                    m_maxIndex;
        private List<TimelineEvent>                     m_events;
        private Dictionary<TimelineEvent, object>       m_visibleEvents;
        private double                                  m_maxEventHeight;
        private Dictionary<TimelineEvent, TimelineDisplayEvent>
                                                        m_dispEvents;
        private bool                                    m_assignRows;
        private Canvas                                  m_canvas;            
        private DataTemplate                            m_template;
        private DataTemplate                            m_eventTemplate;
        private TimelineBand                            m_parent;
        private long                                    m_zindex = 1;


        #endregion

        public TimelineBuilder(
            TimelineBand                                band,
            Canvas                                      canvas,
            DataTemplate                                template,
            int                                         columnCount,
            TimelineCalendar                            timeline,
            DataTemplate                                eventTemplate,
            double                                      maxEventHeight, 
            bool                                        assignRows,
            DateTime                                    currDateTime
        )
        {
            Debug.Assert(template != null);
            Debug.Assert(canvas != null);
            Debug.Assert(eventTemplate != null);
            Debug.Assert(band != null);
            Debug.Assert(columnCount > 0);
            Debug.Assert(timeline != null);
            Debug.Assert(maxEventHeight > 0);

            m_parent = band;
            m_eventTemplate = eventTemplate;
            m_canvas = canvas;
            m_template = template;;
            m_columnCount = columnCount;
            m_timeline = timeline;
            m_assignRows = assignRows;
            
            m_visibleEvents = new Dictionary<TimelineEvent, object>();
            m_dispEvents = new Dictionary<TimelineEvent, TimelineDisplayEvent>();
            m_maxEventHeight = maxEventHeight;

            CurrentDateTime = currDateTime;

            Utilities.Trace(this);
        }

        public TimelineCalendar Calendar
        {
            get
            {
                return m_timeline;
            }
        }

        public TimeSpan VisibleWindowSize
        {
            get;
            set;
        }

        /// 
        /// <summary>
        /// This method used by Utilities.Trace</summary>
        /// 
        public override string ToString(
        )
        {
            return (m_timeline == null ? String.Empty : m_timeline.LineType.ToString());
        }


        public int ColumnCount
        {
            get
            {
                return m_columnCount;
            }
            set
            {
                m_columnCount = value;
            }
        }

        /// 
        /// <summary>
        /// This calculates row for each of event (makes sence only for main band).
        /// We try to place evens so that the do not overlap on the screen.</summary>
        /// 
        public void CalculateEventRows(
        )
        {
            int                                         count;
            int                                         rowCount;
            int                                         minIndex;
            DateTime[]                                  dates;
            DateTime                                    minDate;
            DateTime                                    eventDate;
            TimelineEvent                               e;

            Debug.Assert(m_assignRows);
            Debug.Assert(m_events != null);

            Utilities.Trace(this);

            rowCount = (int) ((PixelHeight - TOP_MARGIN - BOTTOM_MARGIN) / m_maxEventHeight);
            Debug.Assert(rowCount > 0);

            dates = new DateTime[rowCount];

            for (int i = 0; i < rowCount; ++i)
            {
                dates[i] = new DateTime();
            }

            count = m_events.Count;

            for (int i = 0; i < count; ++i)
            {
                minDate = dates[0];    
                minIndex = 0;
                e = m_events[i];

                if (e.RowOverride == -1)
                {
                    eventDate = e.EndDate;

                    for (int k = 0; k < rowCount; ++k)
                    {
                        if (minDate > dates[k])
                        {
                            minIndex = k;
                            minDate = dates[k];
                        }
                    }

                    dates[minIndex] = e.EndDate;
                    e.Row = minIndex;
                }
                else
                {
                    e.Row = e.RowOverride;
                    dates[e.Row] = e.EndDate;
                }
            }
        }


        /// 
        /// <summary>
        /// Calculate top position and width of each event</summary>
        /// 
        public void CalculateEventPositions(
        )
        {
            TimelineDisplayEvent                        pos;

            Debug.Assert(m_events != null);
            Debug.Assert(m_dispEvents != null && m_dispEvents.Count == 0);

            Utilities.Trace(this);
            
            foreach (TimelineEvent e in m_events)
            {
                if (!m_dispEvents.ContainsKey(e))
                {
                    pos = new TimelineDisplayEvent(e);
                    m_dispEvents.Add(e, pos);
                }
                else
                {
                    pos = m_dispEvents[e];
                }

                Debug.Assert(e.Row >= 0, "Need first call CalculateEventRows for main band");

                pos.Top = e.Row * m_maxEventHeight + TOP_MARGIN;

                pos.EventPixelWidth = TimeSpanToPixels(e.EndDate - e.StartDate); 
                pos.EventPixelWidth = Math.Max(3.0, pos.EventPixelWidth);
            }
        }

        /// 
        /// <summary>
        /// Display all visible events and remove all invisible for current visible time window</summary>
        /// 
        public void DisplayEvents(
        )
        {
            DateTime                                    begin;
            DateTime                                    end;
            double                                      distance;
            double                                      x;

            begin = m_timeline.GetFloorTime(m_timeline[m_minIndex]);
            end = m_timeline.GetCeilingTime(m_timeline[m_maxIndex]);

            if (m_events != null)
            {
                foreach (TimelineEvent e in m_events)
                {
                    if (!((e.StartDate < begin && e.EndDate < begin) || 
                          (e.StartDate > end && e.EndDate > end)))
                    {
                        distance = TimeSpanToPixels(CurrentDateTime - e.StartDate);
                        x = PixelWidth / 2 - distance;

                        if (!m_visibleEvents.ContainsKey(e))
                        {
                            m_visibleEvents.Add(e, CreateEvent(e, m_dispEvents[e]));
                        }

                        MoveEvent(m_visibleEvents[e], 
                                  m_dispEvents[e].Top, 
                                  x, 
                                  m_dispEvents[e].EventPixelWidth);
                    }
                    else
                    {
                        if (m_visibleEvents.ContainsKey(e))
                        {
                            RemoveEvent(m_visibleEvents[e]);
                            m_visibleEvents.Remove(e);
                        }
                    }
                }
            }                    
        }

        /// 
        /// <summary>
        /// Remove all events from timeline screen</summary>
        /// 
        public void ClearEvents(
        )
        {
            m_zindex = 1;

            if (m_visibleEvents != null)
            {
                foreach (object e in m_visibleEvents.Values)
                {
                    RemoveEvent(e);
                }
            }

            m_visibleEvents = new Dictionary<TimelineEvent, object>();
            m_dispEvents = new Dictionary<TimelineEvent,TimelineDisplayEvent>();
            m_events = new List<TimelineEvent>();
        }

        /// 
        /// <summary>
        /// Removes all columns from timelineband screen</summary>
        /// 
        public void ClearColumns(
        )
        {
            if (Columns != null)
            {
                foreach (object obj in Columns.Keys)
                {
                    RemoveColumn(obj);
                }
            }
        }

        #region Properties

        public double PixelWidth
        {
            get
            {
                return m_canvas.ActualWidth;
            }
        }

        public double PixelHeight
        {
            get
            {
                return m_canvas.ActualHeight;
            }
        }

        public DateTime CurrentDateTime
        {
            get
            {
                return m_currDate;
            }
            set
            {
                if (value < m_timeline.MinDateTime)
                {
                    m_currDate = m_timeline.MinDateTime;
                }
                else if (value > m_timeline.MaxDateTime)
                {
                    m_currDate = m_timeline.MaxDateTime;
                }
                else
                {
                    m_currDate = value;
                }
            }
        }

        public double ColumnPixelWidth
        {
            get
            {
                return PixelWidth / m_columnCount;
            }
        }

        public TimeSpan ColumnTimeWidth
        {
            get
            {
                DateTime                                begin;
                DateTime                                end;
                DateTime                                date;
                TimeSpan                                tick;

                date = CurrentDateTime;
                tick = new TimeSpan(1L);

                if (date == m_timeline.MaxDateTime)
                {
                    date = m_timeline.GetFloorTime(date) - tick;
                }
                else if (date == m_timeline.MinDateTime)
                {
                    date = m_timeline.GetCeilingTime(date) + tick;
                }

                end = m_timeline.GetFloorTime(date);
                begin = m_timeline.GetCeilingTime(date);

                return begin - end;
            }
        }

        public Dictionary<object, long> Columns
        {
            get
            {
                return m_columnIndexes;
            }
        }

        public List<TimelineEvent> TimelineEvents
        {
            get
            {
                return m_events;
            }
            set
            {
                Debug.Assert(value != null);
                m_events = value;
            }
        }

        #endregion

        public object CreateColumn(
            double                                      left, 
            double                                      top, 
            double                                      width, 
            double                                      height, 
            string                                      content
        )
        {
            FrameworkElement                            el;

            Debug.Assert(top > -1);
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);
            Debug.Assert(ColumnPixelWidth > 0);

            el = m_template.LoadContent() as FrameworkElement;

            el.DataContext = content;
            m_canvas.Children.Add(el);

            el.SetValue(Canvas.LeftProperty, left);
            el.SetValue(Canvas.TopProperty, 0.0);
            el.Width = ColumnPixelWidth + 1;
            el.Height = height;

            return el;
        }

        public void MoveColumn(
            object                                      element,
            double                                      left,
            string                                      content
        )
        {
            FrameworkElement                            el;

            Debug.Assert(element != null);
            Debug.Assert(content != null);

            el = (FrameworkElement) element;

            Debug.Assert(content.Length != 0);

            if (((string) el.DataContext) != content)
            {
                el.DataContext = content;
            }
            el.SetValue(Canvas.LeftProperty, left);
        }

        public object CreateEvent(
            TimelineEvent                               e,
            TimelineDisplayEvent                        de
        )
        {
            FrameworkElement                            element;
            DependencyObject                            obj;
            HyperlinkButton                             link;

            Debug.Assert(e != null);

            obj = m_eventTemplate.LoadContent();
            element = (FrameworkElement) obj;
            element.MouseEnter += OnMouseEnter;
            element.MouseLeave += OnMouseLeave;
            element.DataContext = de;
            element.SetValue(Canvas.ZIndexProperty, 1);
            
            link = (HyperlinkButton) element.FindName("EventLinkTextBlock");

            if (link != null)
            {
                link.Click += m_parent.OnMoreInfoClick;
            }

            m_canvas.Children.Add(element);
            return element;
        }



        void OnMouseLeave(
            object                                      sender, 
            System.Windows.Input.MouseEventArgs         e
        )
        {
            FrameworkElement                            el;

            el = (FrameworkElement) sender;
            el.SetValue(Canvas.ZIndexProperty, 1);
        }

        private void OnMouseEnter(
            object                                      sender, 
            System.Windows.Input.MouseEventArgs         e
        )
        {
            FrameworkElement                            el;

            el = (FrameworkElement) sender;
            el.SetValue(Canvas.ZIndexProperty, 2);
        }

        public void MoveEvent(
            object                                      eo,
            double                                      top,
            double                                      left,
            double                                      width
        )
        {
            FrameworkElement                            el;

            el = eo as FrameworkElement;

            Debug.Assert(el != null);

            el.SetValue(Canvas.TopProperty, top);
            el.SetValue(Canvas.LeftProperty, left);

            el.Width = width;
        }

        public void RemoveEvent(
            object                                      e
        )
        {
            RemoveElement(e);
        }

        public void RemoveColumn(
            object                                      e
        )
        {
            RemoveElement(e);
        }

        private void RemoveElement(
            object                                      e
        )
        {
            bool                                        removed;
            UIElement                                   element;

            element = e as UIElement;
            Debug.Assert(element != null);

            removed = m_canvas.Children.Remove(e as UIElement);
            element.MouseEnter -= OnMouseEnter;
            element.MouseLeave -= OnMouseLeave;

            Debug.Assert(removed);
        }
    
        private string GetDataContext(
            long                                        index
        )
        {
            return TimelineCalendar.ItemToString(m_timeline, m_timeline[index]);
        }

        /// 
        /// <summary>
        /// Build columns (one for each years, dates, etc.)</summary>
        /// 
        public void BuildColumns(
        )
        {
            double                                      step;
            double                                      width;
            double                                      height;
            object                                      el;
            double                                      x;
            FrameworkElement                            fe;

            Utilities.Trace(this);

            width = PixelWidth;
            height = PixelHeight;
            step = ColumnPixelWidth;


            if (m_columnIndexes != null)
            {
                foreach (object o in m_columnIndexes.Keys)
                {
                    fe = (FrameworkElement) o;
                    m_canvas.Children.Remove(fe);
                }
            }

            m_columnIndexes = new Dictionary<object, long>();
            
            for (int i = 0; i < m_columnCount + 2; ++i)
            {
                x = ColumnPixelWidth * (i - 1);
                el = CreateColumn(x, 0.0, step + 1, height, String.Empty);

                m_columnIndexes.Add(el, 0);
            }

            FixPositions();
        }

        /// 
        /// <summary>
        /// Convert pixels to TimeSpan according to current timeline band type</summary>
        /// 
        public TimeSpan PixelsToTimeSpan(
            double                                      pixels
        )
        {
            return new TimeSpan(0, 0, (int) ((pixels / ColumnPixelWidth) * 
                ColumnTimeWidth.TotalSeconds));
        }

        /// 
        /// <summary>
        /// Convert TimeSpan to pixels according to current timeline band type</summary>
        /// 
        public double TimeSpanToPixels(
            TimeSpan                                    span
            )
        {
            TimeSpan                                    totalVisible;

            switch (m_timeline.LineType)
            {
                case TimelineCalendarType.Decades:
                    totalVisible = new TimeSpan(365 * 10 * m_columnCount, 0, 0, 0);
                    break;

                case TimelineCalendarType.Years:
                    totalVisible = new TimeSpan(365 * m_columnCount, 0, 0, 0);
                    break;

                case TimelineCalendarType.Months:
                    totalVisible = new TimeSpan(31 * m_columnCount, 0, 0, 0);
                    break;

                case TimelineCalendarType.Days:
                    totalVisible = new TimeSpan(m_columnCount, 0, 0, 0);
                    break;

                case TimelineCalendarType.Hours:
                    totalVisible = new TimeSpan(0, m_columnCount, 0, 0, 0);
                    break;

                case TimelineCalendarType.Minutes10:
                    totalVisible = new TimeSpan(0, 0, m_columnCount * 10, 0, 0);
                    break;
                
                default: // TimelineType.Minutes
                    totalVisible = new TimeSpan(0, 0, m_columnCount, 0, 0);
                    break;
            }

            return (PixelWidth * span.TotalSeconds) / totalVisible.TotalSeconds;
        }

        /// 
        /// <summary>
        /// Moves current timeline band for the passed timespan</summary>
        /// 
        public void TimeMove(
            TimeSpan                                    span
            )
        {
            CurrentDateTime -= span;
            FixPositions();
        }

        /// 
        /// <summary>
        /// After current data is changed this function fixes positions of all columns 
        /// and all events</summary>
        /// 
        private void FixPositions(
        )
        {
            double                                      width;
            double                                      height;
            object                                      el;
            double                                      x;
            double                                      ox;
            long                                        index;
            double                                      posOffset;
            object[]                                    elements;
            int                                         middle;
            DateTime                                    floor;
            DateTime                                    currDate;
            long                                        maxIndex;

            width = PixelWidth;
            height = PixelHeight;

            m_minIndex = 0;
            m_maxIndex = 0;

            currDate = CurrentDateTime;

            floor = m_timeline.GetFloorTime(currDate);
            posOffset = TimeSpanToPixels(currDate - floor);

            elements = new object[m_columnCount + 2];
            m_columnIndexes.Keys.CopyTo(elements, 0);

            middle = m_columnCount / 2 + 1;
            ox = width / 2 - posOffset;
            index = m_timeline.IndexOf(currDate);
            x = ox;
            maxIndex = m_timeline.IndexOf(m_timeline.MaxDateTime);

            for (int i = middle; i < m_columnCount + 2; ++i)
            {
                el = elements[i];
                MoveColumn(el, x, GetDataContext(index));

                m_columnIndexes[el] = Math.Min(maxIndex, index);

                x += ColumnPixelWidth;

                if (index != int.MaxValue)
                {
                    ++index;
                }
            }

            m_maxIndex = Math.Min(index, maxIndex);
            index = m_timeline.IndexOf(currDate) - 1;
            x = ox - ColumnPixelWidth;

            for (int i = middle - 1; i >= 0; --i, --index)
            {
                el = elements[i];

                MoveColumn(el, x, GetDataContext(index));
                m_columnIndexes[el] = Math.Max(-1, index);

                x -= ColumnPixelWidth;
            }
            m_minIndex = Math.Max(index, 0);
            DisplayEvents();
        }

        public long MinVisibleIndex
        {
            get
            {
                return m_minIndex + 2;
            }
        }

        public DateTime MinVisibleDateTime
        {
            get
            {
                return (DateTime) m_timeline[MinVisibleIndex];
            }
        }

        public long MaxVisibleIndex
        {
            get
            {
                return m_maxIndex - 2;
            }
        }

        public DateTime MaxVisibleDateTime
        {
            get
            {
                return (DateTime) m_timeline[MaxVisibleIndex];
            }
        }
    }
}