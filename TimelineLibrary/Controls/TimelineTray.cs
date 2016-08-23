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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using TimelineLibrary.Data;

namespace TimelineLibrary
{
    /// 
    /// <summary>
    /// Main container class for TimelineBands. It is inherited from Grid, so that timeline band 
    /// can be places one under another and main band can be maximized.</summary>
    /// 
    public class TimelineTray: Grid, ITimelineToolboxTarget
    {
        #region Constants

        // Used for tracing in debug version
        private const string                            FMT_TRACE = "Name: {0}, Size:{1},{2}";
        
        // Default 'more' link text displayed in events of main band
        private const string                            MORE_LINK_TEXT = " More...";

        // Default teaser length of events in main band
        private const int                               DEFAULT_TEASER_SIZE = 80;

        // These default constants are used for scripting
        private const int                               DEFAULT_MAIN_EVENT_SIZE = 70;
        private const int                               DEFAULT_SECONDARY_EVENT_SIZE = 4;

        #endregion

        #region Private Fields

        private List<TimelineBand>                      m_bands;
        private TimelineBand                            m_mainBand;
        private List<TimelineEvent>                     m_events;
        private TimelineUrlCollection                   m_dataUrls;

        private DataControlNotifier                     m_notifier;   
        private string                                  m_cultureId = DateTimeConverter.DEFAULT_CULTUREID;

        // We recognize 2 modes in which control is working: silverlight and javascript.
        // In javascript mode bands are created from javascript, so sequence of control 
        // load events is different from silverlight mode. If no timeline bands defined 
        // in xaml we assume javascript mode otherwize assume silverlight mode.
        private bool                                    m_isJavascriptMode;     
        private DateTime                                m_currentDateTime;
        private bool                                    m_initialized;

        #endregion

        #region Ctor

        public TimelineTray(
        )
        {
            DoubleClickTrigger                          dblClick;

            m_dataUrls = new TimelineUrlCollection();
            m_events = new List<TimelineEvent>();
            m_bands = new List<TimelineBand>();

            MoreLinkText = MORE_LINK_TEXT;

            this.Loaded += OnControlLoaded;
            this.SizeChanged += OnSizeChanged;
           
            dblClick = new DoubleClickTrigger();
            dblClick.Attach(this);
            dblClick.OnDoubleClick += OnDoubleClick;

            Application.Current.Host.Content.FullScreenChanged += OnFullScreenChanged;
        }

        #endregion

        #region Dependency Properties

        #region InitialDateTime
        
        /// 
        /// <summary>
        /// InitialDateTime, MinDateTime and MaxDateTime cannot be changed once
        /// timeline is initialized (see Initialized property).</summary>
        /// 
        private void VerifyNotInitialized(
        )
        {
            if (m_initialized)
            {
                throw new InvalidProgramException("Value cannot be changed after control is initialized");
            }
        }

        public static readonly DependencyProperty InitialDateTimeProperty =
            DependencyProperty.Register("InitialDateTime", typeof(DateTime), 
            typeof(TimelineTray), new PropertyMetadata(DateTime.Now));

        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime InitialDateTime
        {
            get
            {
                return (DateTime) GetValue(InitialDateTimeProperty);
            }
            set
            {
                VerifyNotInitialized();
                SetValue(InitialDateTimeProperty, value);
            }
        }

        #endregion

        #region MinDateTime

        public static readonly DependencyProperty MinDateTimeProperty =
            DependencyProperty.Register("MinDateTime", typeof(DateTime), 
            typeof(TimelineTray), new PropertyMetadata(DateTime.MinValue));

        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime MinDateTime
        {
            get
            {
                return (DateTime) GetValue(MinDateTimeProperty);
            }
            set
            {
                VerifyNotInitialized();
                SetValue(MinDateTimeProperty, value);
            }
        }

        #endregion

        #region MaxDateTime

        public static readonly DependencyProperty MaxDateTimeProperty =
            DependencyProperty.Register("MaxDateTime", typeof(DateTime), 
            typeof(TimelineTray), new PropertyMetadata(DateTime.MaxValue));

        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime MaxDateTime
        {
            get
            {
                return (DateTime) GetValue(MaxDateTimeProperty);
            }
            set
            {
                VerifyNotInitialized();
                SetValue(MaxDateTimeProperty, value);
            }
        }

        #endregion

        #region TeaserSize

        public static readonly DependencyProperty TeaserSizeProperty =
            DependencyProperty.Register("TeaserSize", typeof(int), 
            typeof(TimelineTray), new PropertyMetadata(DEFAULT_TEASER_SIZE));

        [TypeConverter(typeof(DateTimeConverter))]
        public int TeaserSize
        {
            get
            {
                return (int) GetValue(TeaserSizeProperty);
            }
            set
            {
                VerifyNotInitialized();
                SetValue(TeaserSizeProperty, value);
            }
        }

        #endregion

        #endregion

        #region Public Methods and Properties

        /// 
        /// <summary>
        /// ResetEvents method adds events specified in xml to all timeline bands.
        /// If you need to refresh events call ClearEvents and then ResetEvents, this 
        /// will removed all events from the screen and then draw up-to-date events.</summary>
        /// 
        public void ResetEvents(
            string                                      xml
        )
        {
            ResetEvents(XDocument.Parse(xml));
        }

        public void ResetEvents(
            XDocument                                   doc
        )
        {
            m_events.Clear();
            LoadEventDocument(doc);
            RefreshEvents();
        }

        /// 
        /// <summary>
        /// Removes all events from all timeline bands</summary>
        /// 
        public void ClearEvents(
        )
        {
            m_events.Clear();
            RefreshEvents();
        }

        /// 
        /// <summary>
        /// This link text is displayed if description on the event is more then specified by
        /// TeaserSize property</summary>
        /// 
        public string MoreLinkText
        {
            get;
            set;
        }

        /// 
        /// <summary>
        /// Refresh (delete and recreate and redisplay) all events on all timeline bands.</summary>
        /// 
        public void RefreshEvents(
        )
        {
            Debug.Assert(m_mainBand != null);

            Utilities.Trace(this);

            foreach (TimelineBand b in m_bands)
            {
                Debug.Assert(b.Calculator != null);

                b.ClearEvents();
                b.TimelineEvents = m_events;
            }

            m_mainBand.CalculateEventRows();
            
            foreach (TimelineBand b in m_bands)
            {
                b.CalculateEventPositions();
            }

            foreach (TimelineBand b in m_bands)
            {
                b.DisplayEvents();
            }
        }

        /// 
        /// <summary>
        /// This method is used for debugging (from Utilities.Trace)</summary>
        /// 
        public override string ToString(
        )
        {
            return String.Format(FMT_TRACE, Name, ActualHeight, ActualWidth);
        }

        /// 
        /// <summary>
        /// Gets or sets current date of all timeline bands. Current date is in the middle of 
        /// each timeline band. This property (in opposite to InitialDateTime) can be changed
        /// from code behind to programmatically move timelines back and forth</summary>
        /// 
        public DateTime CurrentDateTime
        {
            get
            {
                return m_currentDateTime;
            }
            set
            {
                m_currentDateTime = value;
                if (m_mainBand != null)
                {
                    m_mainBand.CurrentDateTime = value;
                }
            }
        }

        /// 
        /// <summary>
        /// Specifies that control is initialized, which means that all bands are provided,
        /// all controls resized and xml data are read. Some of the properties and methods
        /// cannot be executed once control is initialized.</summary>
        /// 
        public bool Initialized
        {
            get
            {
                return m_initialized;
            }
        }

        /// 
        /// <summary>
        /// Calendar type (see TimelineCalendar.CalendarFromString property for the list of 
        /// values). This value cannot be changed after control is initialized.
        /// </summary>
        /// 
        public string CalendarType
        {
            get;
            set;
        }

        /// 
        /// <summary>
        /// Culture ID specified what Date format is used, by default we use 'en-US'</summary>
        /// 
        public string CultureID
        {
            get
            {
                return m_cultureId;
            }
            set
            {
                Debug.Assert(value != null && value.Length > 0);
                
                m_cultureId = value;
                DateTimeConverter.CultureId = m_cultureId;
            }
        }

        /// 
        /// <summary>
        /// This property is used for DateTime calculations and is based on CultureID</summary>
        /// 
        public CultureInfo CultureInfo
        {
            get
            {
                return DateTimeConverter.CultureInfo;
            }
        }


        /// 
        /// <summary>
        /// Zooms in/out all timeline bands by one column</summary>
        /// 
        public void Zoom(
            bool                                         zoomIn
        )
        {
            int                                          inc;
            TimeSpan                                     visibleWindow;

            inc = zoomIn ? -1 : 1;

            if (m_initialized)
            {
                foreach (TimelineBand band in m_bands)
                {
                    if (band.TimelineWindowSize + inc >= 2)
                    {
                        band.TimelineWindowSize += inc;
                    }
                    band.Calculator.BuildColumns();
                }

                visibleWindow = m_mainBand.Calculator.MaxVisibleDateTime - 
                    m_mainBand.Calculator.MinVisibleDateTime;

                foreach (TimelineBand band in m_bands)
                {
                    band.VisibleTimeSpan = visibleWindow;
                    band.ResetVisibleDaysHighlight();
                }
                RefreshEvents();
            }
        }

        /// 
        /// <summary>
        /// Collection of data file locations to load events from</summary>
        /// 
        public TimelineUrlCollection Urls
        {
            get
            {
                return m_dataUrls;
            }
        }

        #endregion

        #region Methods and Properties for Scripting from HTML

        /// 
        /// <summary>
        /// Loads data and displays them in the control</summary>
        /// 
        public void Run(
        )
        {
            Utilities.Trace(this);

            if (!m_isJavascriptMode)
            {
                throw new Exception("Run method should not be called if you specify timeline in xaml.");
            }

            m_notifier.AddUrls(m_dataUrls);

            m_notifier.Start();
            m_notifier.CheckCompleted();
        }

        /// 
        /// <summary>
        /// Add new timeline band</summary>
        /// 
        public void AddTimelineBand(
            int                                         height,
            bool                                        isMain, 
            string                                      srcType, 
            int                                         columnsCount
        )
        {
            AddTimelineBand(height, isMain, srcType, columnsCount, 
                isMain ? DEFAULT_MAIN_EVENT_SIZE : DEFAULT_SECONDARY_EVENT_SIZE);
        }

        public void AddTimelineToolbox(
        )
        {
            TimelineToolbox                             toolbox;
            RowDefinition                               rd;

            toolbox = new TimelineToolbox();
            toolbox.SetSite(this as ITimelineToolboxTarget);

            rd = new RowDefinition();

            if (toolbox.DesiredSize.Height > 0)
            {
                rd.Height = new GridLength(toolbox.DesiredSize.Height);
            }
            else
            {
                rd.Height = new GridLength(0, GridUnitType.Auto);
            }

            this.RowDefinitions.Add(rd);
            toolbox.SetValue(Grid.RowProperty, this.RowDefinitions.Count() - 1);

            this.Children.Add(toolbox);

        }

        public void AddTimelineBand(
            int                                         height,
            bool                                        isMain, 
            string                                      srcType, 
            int                                         columnsCount, 
            int                                         eventSize
        )
        {
            TimelineBand                                band;
            RowDefinition                               rd;

            band = new TimelineBand();
            m_notifier.AddElement(band);

            band.IsMainBand = isMain;
            band.ItemSourceType = srcType;

            rd = new RowDefinition();
            
            if (height > 0)
            {
                rd.Height = new GridLength((double) height);
                band.Height = height;
            }
            else
            {
                rd.Height = new GridLength(1.0, GridUnitType.Star);
            }

            this.RowDefinitions.Add(rd);

            band.SetValue(Grid.RowProperty, this.RowDefinitions.Count() - 1);
            band.Margin = new Thickness(0.0);
            band.TimelineWindowSize = columnsCount;
            band.MaxEventHeight = eventSize;
            band.TimelineTray = this;

            if (band.IsMainBand)
            {
                m_mainBand = band;
            }

            m_bands.Add(band);
            this.Children.Add(band);
        }

        #endregion

        #region Protected and Private methods

        private void OnDoubleClick(
            object                                      sender, 
            MouseButtonEventArgs                        e
        )
        {
            Utilities.Trace(this);

            Zoom(Keyboard.Modifiers != ModifierKeys.Alt);
        }

        private void OnFullScreenChanged(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            Utilities.Trace(this);

            if (m_initialized)
            {
                RefreshEvents();
            }
        }

        private void OnSizeChanged(
            object                                      sender, 
            SizeChangedEventArgs                        e
        )
        {
            Utilities.Trace(this);

            if (m_initialized)
            {
                RefreshEvents();
            }
        }

        /// 
        /// <summary>
        /// The user moved current datetime on one of the timeline bands, so we 
        /// sync all other bands with it.</summary>
        /// 
        private void OnCurrentDateChanged(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            TimelineBand                                band;

            band = (TimelineBand) sender;
            m_currentDateTime = band.CurrentDateTime;

            m_bands.ForEach(b => 
            {
                if (sender != b) 
                { 
                    b.CurrentDateTime = band.CurrentDateTime; 
                }
            });
        }

        /// 
        /// <summary>
        /// Adds events located in passed document to list of all events</summary>
        /// 
        private void LoadEventDocument(
            XDocument                               doc
            )
        {
            List<TimelineEvent>                     allEvents;
            DateTime                                start;
            DateTime                                end;
            TimelineEvent                           ev;
            bool                                    isDuration;

            Debug.Assert(doc != null);
            Utilities.Trace(this);

            var events = from row in doc.Descendants("data").Descendants("event")
                select new {
                    Start = GetAttribute(row.Attribute("start")),
                    End = GetAttribute(row.Attribute("end")),
                    Title = GetAttribute(row.Attribute("title")),
                    EventColor = GetAttribute(row.Attribute("color")),
                    EventLink = GetAttribute(row.Attribute("link")),
                    EventImage = GetAttribute(row.Attribute("image")),
                    TeaserEventImage = GetAttribute(row.Attribute("teaserimage")),
                    Description = GetContent(row),
                    RowOverride = GetAttribute(row.Attribute("rowOverride")),
                    IsDuration = GetAttribute(row.Attribute("isDuration")),
                };

            allEvents = new List<TimelineEvent>();
            
            foreach (var e in events)
            {
                if (e.Start.Length == 4)
                {
                    start = new DateTime(int.Parse(e.Start), 1, 1);
                }
                else
                {
                    start = DateTime.Parse(e.Start, DateTimeConverter.CultureInfo);
                }


                if (String.Compare(e.IsDuration, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    isDuration = true;
                    if (e.End.Length == 4)
                    {
                        end = new DateTime(int.Parse(e.End), 1, 1);
                    }
                    else if (e.End.Length < 4)
                    {
                        end = start;
                    }
                    else
                    {
                        end = DateTime.Parse(e.End, DateTimeConverter.CultureInfo);
                    }
                }
                else
                {
                    isDuration = false;
                    end = start;
                }
               
                ev = new TimelineEvent();
                ev.StartDate = start;
                ev.EndDate = end;
                ev.Title = e.Title;
                ev.Description = e.Description;
                ev.IsDuration = isDuration;
                ev.EventColor = e.EventColor;
                ev.EventImage = e.EventImage;
                ev.Link = e.EventLink;
                ev.TeaserEventImage = e.TeaserEventImage;
                ev.RowOverride = e.RowOverride.Length == 0 ? -1 : int.Parse(e.RowOverride);

                m_events.Add(ev);
            }
            m_events.Sort(CompareEvents);
        }


        private void OnControlLoaded(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {

            Utilities.Trace(this);
            
            HookChildElements(this.Children);
    
            if (m_bands.Count > 0 && m_mainBand != null)
            {
                // we are in silverlight mode, so all bands and urls are specified in 
                // xaml and we can start data load immediately
                m_isJavascriptMode = false;

                m_notifier = new DataControlNotifier(m_dataUrls, m_bands);
                m_notifier.LoadComplete += OnControlAndDataComlete; 
                m_notifier.Start();
                m_notifier.CheckCompleted();
            }
            else
            {
                // we will need to wait till Run method is called from javascript
                m_isJavascriptMode = true;
                m_notifier = new DataControlNotifier();
                m_notifier.LoadComplete += OnControlAndDataComlete; 
            }
        }

        public void HookChildElements(
            UIElementCollection                         col
        )
        {
            TimelineBand                                b;

            foreach (UIElement  el in Children)
            {
                if (el as TimelineBand != null)
                {
                    b = (TimelineBand) el;
                    b.TimelineTray = this;

                    if (b.IsMainBand)
                    {
                        m_mainBand = b;
                    }

                    m_bands.Add(b);
                }
                else if (el as TimelineToolbox != null)
                {
                    ((TimelineToolbox) el).SetSite(this);
                }
                else if (el as Panel != null)
                {
                    HookChildElements(((el) as Panel).Children);
                }
            }        

        }

        /// 
        /// <summary>
        /// This happens when we have all data and all timeline band controls have 
        /// completed resizing, so we ready to show content</summary>
        /// 
        private void OnControlAndDataComlete(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            TimeSpan                                    visibleWindow;

            Debug.Assert(m_notifier != null);

            TimelineDisplayEvent.MoreLinkText = MoreLinkText;
            TimelineDisplayEvent.TeaserSize = TeaserSize;

            m_currentDateTime = InitialDateTime;
            m_notifier.StreamList.ForEach(s => LoadEventDocument(XDocument.Load(s, LoadOptions.None)));

            if (m_mainBand == null)
            {
                throw new Exception("At least one main timeline band should be specified");
            }

            m_mainBand.CreateTimelineCalculator(CalendarType, CurrentDateTime, 
                MinDateTime, MaxDateTime);

            m_bands.ForEach(b => b.CreateTimelineCalculator(CalendarType, CurrentDateTime,
                MinDateTime, MaxDateTime));

            //
            // now we need to calculate visible timeline window and 
            // assign it to all timelineband controls
            //
            visibleWindow = m_mainBand.Calculator.MaxVisibleDateTime - 
                m_mainBand.Calculator.MinVisibleDateTime;

            foreach (TimelineBand band in m_bands)
            {
                band.VisibleTimeSpan = visibleWindow;
                band.ResetVisibleDaysHighlight();

                band.Calculator.BuildColumns();

                band.OnCurrentDateChanged += OnCurrentDateChanged;
            }
            RefreshEvents();

            m_notifier = null;
            m_initialized = true;
        }


        /// 
        /// <summary>
        /// Attribute value reader helper.</summary>
        /// 
        private static string GetAttribute(
            XAttribute                                  a
        )
        {
            return a == null ? String.Empty : a.Value;
        }

        ///
        /// <summary>
        /// Returns content of element as xml</summary>
        ///
        private static string GetContent(
            XElement                                    e
            )
        {
            return (e.FirstNode == null ? String.Empty : e.FirstNode.ToString());
        }

        /// 
        /// <summary>
        /// Sort function for events by startdate</summary>
        /// 
        private static int CompareEvents(
            TimelineEvent                               a,
            TimelineEvent                               b
        )
        {
            int                                         ret;

            Debug.Assert(a != null);
            Debug.Assert(b != null);

            if (a.StartDate == b.StartDate)
            {
                ret = 0;
            }
            else if (a.StartDate < b.StartDate)
            {
                ret = -1;
            }
            else
            {
                ret = 1;
            }

            return ret;
        }
        #endregion 
    
        #region ITimelineToolboxTarget Members

        void ITimelineToolboxTarget.FindMinDate(
        )
        {
            this.CurrentDateTime = this.MinDateTime;
        }

        void ITimelineToolboxTarget.FindMaxDate(
        )
        {
            this.CurrentDateTime = this.MaxDateTime;
        }

        void ITimelineToolboxTarget.FindDate(
            DateTime                                    date
        )
        {
            this.CurrentDateTime = date;
        }

        void ITimelineToolboxTarget.MoveLeft(
        )
        {
            this.CurrentDateTime -= m_mainBand.Calculator.ColumnTimeWidth;
        }

        void ITimelineToolboxTarget.MoveRight(
        )
        {
            this.CurrentDateTime += m_mainBand.Calculator.ColumnTimeWidth;
        }

        void ITimelineToolboxTarget.ZoomIn(
        )
        {
            this.Zoom(true);
        }

        void ITimelineToolboxTarget.ZoomOut(
        )
        {
            this.Zoom(false);
        }

        #endregion
    }
}
