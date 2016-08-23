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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimelineLibrary
{
    /// 
    /// <summary>
    /// Timeline (which is an instance of TimelineTray class) may have several TimelineBand
    /// objects in it. Each TimelineBand may represent years, months, and so on. Events 
    /// are displayed on each timelineband</summary>
    /// 
    [TemplatePart(Name = TimelineBand.TP_CANVAS_PART, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = TimelineBand.TP_NAVIGATE_LEFT_PART, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = TimelineBand.TP_NAVIGATE_LEFT_BUTTON_PART, Type = typeof(Button))]
    [TemplatePart(Name = TimelineBand.TP_NAVIGATE_RIGHT_PART, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = TimelineBand.TP_NAVIGATE_RIGHT_BUTTON_PART, Type = typeof(Button))]
    [TemplatePart(Name = TimelineBand.TP_VISIBLE_DATES_PART, Type = typeof(Rectangle))]
    [TemplatePart(Name = TimelineBand.TP_MAIN_GRID_PART, Type = typeof(Grid))]
    public class TimelineBand: Control
    {
        #region Events

        public delegate void TimelineBandEvent(object sender, RoutedEventArgs e);

        public event TimelineBandEvent                  OnCurrentDateChanged;
        
        #endregion

        #region Private Fields and Constants

        #region Types of Scale

        private const string                            TL_TYPE_DECADES = "decades";
        private const string                            TL_TYPE_YEARS = "years";
        private const string                            TL_TYPE_MONTHS = "months";
        private const string                            TL_TYPE_DAYS = "days";
        private const string                            TL_TYPE_HOURS = "hours";
        private const string                            TL_TYPE_MINUTES = "minutes";
        private const string                            TL_TYPE_MINUTES10 = "minutes10";

        #endregion

        #region Control Part constants

        private const string                            TP_CANVAS_PART = "CanvasPart";
        private const string                            TP_NAVIGATE_LEFT_PART = "NavigateLeft";
        private const string                            TP_NAVIGATE_LEFT_BUTTON_PART = "NavigateLeftButton";
        private const string                            TP_NAVIGATE_RIGHT_PART = "NavigateRight";
        private const string                            TP_NAVIGATE_RIGHT_BUTTON_PART = "NavigateRightButton";
        private const string                            TP_MAIN_GRID_PART = "MainGrid";
        private const string                            TP_VISIBLE_DATES_PART = "VisibleDatesPart";

        #endregion

        private const string                            FMT_TRACE = "Name: {0}, Type:{1}, Size:{2},{3},{4},{5}";


        // user drags timeline (and therefore changes current datetime)
        private bool                                    m_dragging;

        // need move timeline in dragdrop mode (change current time)
        private bool                                    m_needMove;

        // canval where we draw column lines, column titles and events
        private Canvas                                  m_canvasPart;  

        // previous mouse point where we started dragging from
        private Point                                   m_prevMousePoint;
        
        // timeline columns clipping area
        private RectangleGeometry                       m_clipRect;

        private TimelineBuilder                         m_calc;

        // specifies if all columns already initialized
        private bool                                    m_calcInitialized;
        
        private string                                  m_sourceType = TL_TYPE_YEARS;
        private TimelineCalendarType                    m_timelineType;
        private string                                  m_calendarType;

        #endregion

        #region Dependency Properties

        #region DefaultItemTemplate
        public static readonly DependencyProperty DefaultItemTemplateProperty =
            DependencyProperty.Register("DefaultItemTemplate", typeof(DataTemplate), 
            typeof(TimelineBand), new PropertyMetadata(DefaultItemTemplateChanged));


        public static void DefaultItemTemplateChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;
            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnDefaultItemTemplateChanged(e); 
            }
        }

        protected virtual void OnDefaultItemTemplateChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            DefaultItemTemplate = (DataTemplate) e.NewValue;
        }

        public DataTemplate DefaultItemTemplate
        {
            get
            {
                return (DataTemplate) GetValue(DefaultItemTemplateProperty);
            }
            set
            {
                SetValue(DefaultItemTemplateProperty, value);
            }
        }
        #endregion

        #region DefaultShortEventTemplate
        
        public static readonly DependencyProperty DefaultShortEventTemplateProperty =
            DependencyProperty.Register("DefaultShortEventTemplate", 
                typeof(DataTemplate), typeof(TimelineBand), 
                new PropertyMetadata(DefaultShortEventTemplateChanged));


        public static void DefaultShortEventTemplateChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;
            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnDefaultShortEventTemplateChanged(e); 
            }
        }

        protected virtual void OnDefaultShortEventTemplateChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            DefaultShortEventTemplate = (DataTemplate) e.NewValue;
        }
        
        public DataTemplate DefaultShortEventTemplate
        {
            get
            {
                return (DataTemplate) GetValue(DefaultShortEventTemplateProperty);
            }

            set
            {
                SetValue(DefaultShortEventTemplateProperty, value);
            }
        }
        #endregion

        #region DefaultEventTemplate
        
        public static readonly DependencyProperty DefaultEventTemplateProperty =
            DependencyProperty.Register("DefaultEventTemplate", 
                typeof(DataTemplate), 
                typeof(TimelineBand), 
                new PropertyMetadata(DefaultEventTemplateChanged));


        public static void DefaultEventTemplateChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;
            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnDefaultEventTemplateChanged(e); 
            }
        }

        protected virtual void OnDefaultEventTemplateChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            DefaultEventTemplate = (DataTemplate) e.NewValue;
        }

        public DataTemplate DefaultEventTemplate
        {
            get
            {
                return (DataTemplate)GetValue(DefaultEventTemplateProperty);
            }

            set
            {
                SetValue(DefaultEventTemplateProperty, value);
            }
        }
        #endregion

        #region EventTemplate

        public static readonly DependencyProperty EventTemplateProperty =
            DependencyProperty.Register("EventTemplate", 
                typeof(DataTemplate), 
                typeof(TimelineBand), 
                new PropertyMetadata(EventTemplateChanged));

        public static void EventTemplateChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;
            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnEventTemplateChanged(e); 
            }
        }

        protected virtual void OnEventTemplateChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            EventTemplate = (DataTemplate) e.NewValue;
        }

        public DataTemplate EventTemplate
        {
            get
            {
                return (DataTemplate) GetValue(EventTemplateProperty);
            }

            set
            {
                SetValue(EventTemplateProperty, value);
            }
        } 
        
        #endregion

        #region ItemTemplate

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", 
                typeof(DataTemplate), 
                typeof(TimelineBand), 
                new PropertyMetadata(ItemTemplateChanged));

        public static void ItemTemplateChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;
            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnItemTemplateChanged(e); 
            }
        }

        protected virtual void OnItemTemplateChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            ItemTemplate = (DataTemplate) e.NewValue;
        }

        public DataTemplate ItemTemplate
        {
            get
            {
                return (DataTemplate)GetValue(ItemTemplateProperty);
            }

            set
            {
                SetValue(ItemTemplateProperty, value);
            }
        } 
        
        #endregion

        #region ShortEventTemplate

        public static readonly DependencyProperty ShortEventTemplateProperty =
            DependencyProperty.Register("ShortEventTemplate", 
                typeof(DataTemplate), 
                typeof(TimelineBand), 
                new PropertyMetadata(ShortEventTemplateChanged));

        public static void ShortEventTemplateChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;
            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnShortEventTemplateChanged(e); 
            }
        }

        protected virtual void OnShortEventTemplateChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            ShortEventTemplate = (DataTemplate) e.NewValue;
        }

        public DataTemplate ShortEventTemplate
        {
            get
            {
                return (DataTemplate)GetValue(ShortEventTemplateProperty);
            }

            set
            {
                SetValue(ShortEventTemplateProperty, value);
            }
        } 
        
        #endregion

        #region TimelineWindowSize

        public static readonly DependencyProperty TimelineWindowSizeProperty =
            DependencyProperty.Register("TimelineWindowSize", typeof(int), 
                typeof(TimelineBand), new PropertyMetadata(
                10, TimelineWindowSizeChanged));

        public static void TimelineWindowSizeChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            TimelineBand                        band;

            band = (TimelineBand) d;

            if (band != null)
            {
                band.OnTimelineWindowSizeChanged(e); 
            }
        }

        protected virtual void OnTimelineWindowSizeChanged(
            DependencyPropertyChangedEventArgs          e
        )
        {
            int                                         newVal;

            newVal = (int) e.NewValue;

            if (newVal < 2)
            {
                throw new ArgumentException("TimelineWindowSize should be greater then 2");
            }
            if (Calculator != null)
            {
                Calculator.ColumnCount = newVal;
            }

            UpdateControlSize();
        }

        public int TimelineWindowSize
        {
            get
            {
                return (int) GetValue(TimelineWindowSizeProperty);
            }
            set
            {
                SetValue(TimelineWindowSizeProperty, value);
            }
        }

        #endregion

        #region MaxEventHeight

        public static readonly DependencyProperty MaxEventHeightProperty =
            DependencyProperty.Register("MaxEventHeight", 
                typeof(double), typeof(TimelineBand), 
                new PropertyMetadata(50.0, MaxEventHeightChanged));

        public static void MaxEventHeightChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            ((TimelineBand) d).MaxEventHeight = (double) e.NewValue;
        }

        public double MaxEventHeight
        { 
            get
            {
                return (double) GetValue(MaxEventHeightProperty);
            }

            set
            {
                Debug.Assert(value > 1);
                SetValue(MaxEventHeightProperty, value);
            }
        }

        #endregion

        #region IsMainBand

        public static readonly DependencyProperty IsMainBandProperty =
            DependencyProperty.Register("IsMainBand", 
                typeof(bool), typeof(TimelineBand), 
                new PropertyMetadata(false, IsMainBandChanged));

        public static void IsMainBandChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            ((TimelineBand) d).IsMainBand = (bool) e.NewValue;
        }

        public bool IsMainBand
        { 
            get
            {
                return (bool) GetValue(IsMainBandProperty);
            }

            set
            {
                SetValue(IsMainBandProperty, value);
            }
        }

        #endregion

        #endregion

        #region Template Parts

        public Grid MainGridPart
        {
            get;
            set;
        }

        public Canvas CanvasPart
        {
            get
            {
                return m_canvasPart;
            }

            set
            {
                if (m_canvasPart != null)
                {
                    m_canvasPart.MouseMove -= OnCanvasMouseMove;
                    m_canvasPart.MouseLeftButtonDown -= OnCanvasMouseLeftButtonDown;
                    m_canvasPart.MouseLeftButtonUp -= OnCanvasMouseLeftButtonUp;
                    m_canvasPart.MouseWheel -= OnCanvasMouseWheel;
                    m_canvasPart.SizeChanged -= OnSizeChanged;
                }

                m_canvasPart = value;

                if (value != null)
                {
                    m_canvasPart.MouseMove += OnCanvasMouseMove;
                    m_canvasPart.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
                    m_canvasPart.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
                    m_canvasPart.MouseWheel += OnCanvasMouseWheel;
                    m_canvasPart.SizeChanged += OnSizeChanged;

                    m_canvasPart.DataContext = this;
                }
            }
        }

        void OnSizeChanged(
            object                                      sender, 
            SizeChangedEventArgs                        e
        )
        {
            this.ClipRect.Rect = new Rect(new Point(0, 0), 
                new Size(CanvasPart.ActualWidth, CanvasPart.ActualHeight));
        }

        void OnCanvasMouseWheel(
            object                                      sender, 
            MouseWheelEventArgs                         e
        )
        {
            TimeSpan                                    span;

            span = m_calc.PixelsToTimeSpan(e.Delta / 5);
            SafeDateChange(span, true);
        }

        public Rectangle VisibleDatesPart
        {
            get;
            set;
        }

        #region NavigateLeft 
        
        private Button                                  m_navigateLeftButton;
        public Button NavigateLeftButton
        {
            get
            {
                return m_navigateLeftButton;
            }

            set
            {
                if (m_navigateLeftButton != null)
                {
                    m_navigateLeftButton.Click -= OnNavigateLeftClick;
                }

                m_navigateLeftButton = value;

                if (m_navigateLeftButton != null)
                {
                    m_navigateLeftButton.Click += OnNavigateLeftClick;
                }
            }
        }

        void OnNavigateLeftClick(object sender, RoutedEventArgs e)
        {
            SafeDateChange(m_calc.ColumnTimeWidth, true);
        }

        #endregion

        #region NavigateRight 
        
        private Button                                  m_navigateRightButton;
        public Button NavigateRightButton
        {
            get
            {
                return m_navigateRightButton;
            }

            set
            {
                if (m_navigateRightButton != null)
                {
                    m_navigateRightButton.Click -= OnNavigateRightClick;
                }

                m_navigateRightButton = value;

                if (m_navigateRightButton != null)
                {
                    m_navigateRightButton.Click += OnNavigateRightClick;
                }
            }
        }

        void OnNavigateRightClick(object sender, RoutedEventArgs e)
        {
            SafeDateChange(m_calc.ColumnTimeWidth, false);
        }

        #endregion

        #region Template Parts' Event Handlers

        private void OnCanvasMouseLeftButtonDown(
            object                                      sender, 
            MouseButtonEventArgs                        e
        )
        {
            if (!m_dragging)
            {
                m_canvasPart.CaptureMouse();
                m_canvasPart.Cursor = Cursors.Hand;
 	            m_dragging = true;
            }
        }

        private void OnCanvasMouseLeftButtonUp(
            object                                      sender, 
            MouseButtonEventArgs                        e
        )
        {
            StopDragging();
        }

        private void OnCanvasMouseMove(
            object                                      sender, 
            MouseEventArgs                              e
        )
        {
            if (m_calc != null)
            {
                MoveScale(e);
            }
        }

        private void StopDragging(
        )
        {
            if (m_dragging)
            {
                m_canvasPart.ReleaseMouseCapture();
                m_canvasPart.Cursor = Cursors.Arrow;
            }

            m_dragging = false;
            m_needMove = false;
        }

        #endregion

        #endregion

        #region Public Properties

        public TimelineTray TimelineTray
        {
            get;
            set;
        }

        public String CalendarType
        {
            get;
            set;
        }

        public string ItemSourceType
        {
            get
            {
                return m_sourceType;
            }

            set
            {
                string                                  itemsType;

                itemsType = value.ToLower();
                m_sourceType = value;

                switch (itemsType)
                {
                    case TL_TYPE_DECADES:
                        m_timelineType = TimelineCalendarType.Decades;
                        break;

                    case TL_TYPE_YEARS:
                        m_timelineType = TimelineCalendarType.Years;
                        break;

                    case TL_TYPE_MONTHS:
                        m_timelineType = TimelineCalendarType.Months;
                        break;

                    case TL_TYPE_DAYS:
                        m_timelineType = TimelineCalendarType.Days;
                        break;

                    case TL_TYPE_HOURS:
                        m_timelineType = TimelineCalendarType.Hours;
                        break;

                    case TL_TYPE_MINUTES10:
                        m_timelineType = TimelineCalendarType.Minutes10;
                        break;

                    case TL_TYPE_MINUTES:
                        m_timelineType = TimelineCalendarType.Minutes;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// 
        /// <summary>
        /// This sets/gets both dependency property and internal current datetime</summary>
        /// 
        public DateTime CurrentDateTime
        {
            get
            {
                return m_calc == null ? new DateTime() : m_calc.CurrentDateTime;
            }
            set
            {
                if (m_calc != null && m_calc.CurrentDateTime != value)
                {
                    m_calc.TimeMove(m_calc.CurrentDateTime - value);
                    
                    if (OnCurrentDateChanged != null)
                    {
                        OnCurrentDateChanged(this, new RoutedEventArgs());
                    }
                }
            }
        }

        public string DisplayField
        { 
            get; 
            set;
        }

        public string InitialDateTime
        {
            get;
            set;
        } 

        public TimelineCalendar ItemsSource 
        { 
            get; 
            set; 
        }

        public List<TimelineEvent> TimelineEvents
        {
            get
            {
                return m_calc.TimelineEvents;
            }
            set
            {
                Debug.Assert(value != null);
                m_calc.TimelineEvents = value;
            }
        }

        public double VisibleDatesAreaWidth
        {
            get
            {
                double                                  width;

                Debug.Assert(m_calc != null);
                if (IsMainBand)
                {
                    width = 0;
                }
                else 
                {
                    width = m_calc.TimeSpanToPixels(VisibleTimeSpan);
                }

                return width;
            }
        }

        public double VisibleDatesAreaHeight
        {
            get
            {
                return CanvasPart.ActualHeight - 1;
            }
        }

        public TimeSpan VisibleTimeSpan
        {
            get
            {
                return (m_calc == null ? new TimeSpan(0L) : m_calc.VisibleWindowSize);
            }

            internal set
            {
                Debug.Assert(m_calc != null);

                m_calc.VisibleWindowSize = value;
            }
        }

        public TimelineBuilder Calculator
        {
            get
            {
                return m_calc;
            }
        }

        #endregion 

        #region Public and Internal methods

       public void OnMoreInfoClick(
            object                                      sender, 
            RoutedEventArgs                             args
        )
        {
            EventDetailsWindow                          details;
            FrameworkElement                            element;

            details = new EventDetailsWindow();
            details.DataContext = ((FrameworkElement) sender).DataContext;

            element = (FrameworkElement) sender;

            if (TooltipServiceEx.LastTooltip != null)
            {
                TooltipServiceEx.LastTooltip.Hide();
                TooltipServiceEx.LastTooltip = null;
            }

            details.Show();
        }

        public override void OnApplyTemplate(
        )
        {
            Utilities.Trace(this);

            base.OnApplyTemplate();

            MainGridPart = (Grid) GetTemplateChild(TP_MAIN_GRID_PART);
            CanvasPart = (Canvas) GetTemplateChild(TP_CANVAS_PART);
            NavigateLeftButton = (Button) GetTemplateChild(TP_NAVIGATE_LEFT_BUTTON_PART);
            NavigateRightButton = (Button) GetTemplateChild(TP_NAVIGATE_RIGHT_BUTTON_PART);
            VisibleDatesPart = (Rectangle) GetTemplateChild(TP_VISIBLE_DATES_PART);
        }

        public TimelineBand(
        )
        {
            Utilities.Trace(this);

            this.DefaultStyleKey = typeof(TimelineBand);
            this.Loaded += OnControlLoaded;
            this.SizeChanged += OnControlSizeChanged;

            Application.Current.Host.Content.FullScreenChanged += OnFullScreen;
        }

        void OnFullScreen(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            Utilities.Trace(this);

            UpdateControlSize();
        }

        void OnControlSizeChanged(
            object                                      sender, 
            SizeChangedEventArgs                        e
        )
        {
            Utilities.Trace(this);

            UpdateControlSize();
        }

        void UpdateControlSize(
        )
        {
            Utilities.Trace(this);

            if (m_calcInitialized)
            {
                m_calc.BuildColumns();
                this.ResetVisibleDaysHighlight();
                this.DisplayEvents();
            }
        }

        
        void OnControlLoaded(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            Utilities.Trace(this);
        }

        public void CreateTimelineCalculator(
            string                                      calendarType,
            DateTime                                    initialDateTime,
            DateTime                                    minDateTime,
            DateTime                                    maxDateTime
        )
        {
            Debug.Assert(this.ActualWidth != 0);
            Debug.Assert(this.ActualHeight != 0);
            Debug.Assert(this.TimelineTray != null);

            ItemsSource = new TimelineCalendar(calendarType, m_timelineType,
                minDateTime, maxDateTime);

            m_calendarType = calendarType;

            if (ItemTemplate == null)
            {
                ItemTemplate = DefaultItemTemplate;
            }
            
            if (ShortEventTemplate == null)
            {
                ShortEventTemplate = DefaultShortEventTemplate;
            }

            if (EventTemplate == null)
            {
                EventTemplate = DefaultEventTemplate;
            }

            if (m_calc != null)
            {
                m_calc.ClearEvents();
                m_calc.ClearColumns();
            }

            m_calc = new TimelineBuilder(
                this,
                CanvasPart, 
                ItemTemplate, 
                TimelineWindowSize, 
                ItemsSource, 
                !IsMainBand ? ShortEventTemplate : EventTemplate,
                MaxEventHeight,
                IsMainBand,
                initialDateTime);

            m_calc.BuildColumns();
            m_calcInitialized = true;
        }


        public void CalculateEventRows(
        )
        {
            //
            // it only makes sence to calculate row positions for main band becuase
            // other bands should use rows from main band to look similar with main.
            //
            Debug.Assert(IsMainBand);
            Debug.Assert(m_calc != null);

            m_calc.CalculateEventRows();
        }

        /// 
        /// <summary>
        /// Calculates event positions (should be called after CalculateEventRows for main (see IsMainBand)
        /// timelineband)</summary>
        /// 
        public void CalculateEventPositions(
        )
        {
            Debug.Assert(m_calc != null);

            m_calc.CalculateEventPositions();
        }
        
        /// 
        /// <summary>
        /// Clear all events from timelineband screen</summary>
        /// 
        public void ClearEvents(
        )
        {
            if (m_calc != null)
            {
                m_calc.ClearEvents();
            }
        }

        /// 
        /// <summary>
        /// Display all events which should be visible in current timelineband window</summary>
        /// 
        public void DisplayEvents(
        )
        {
            Debug.Assert(m_calc != null);
            m_calc.DisplayEvents();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ResetVisibleDaysHighlight(
        )
        {
            if (VisibleDatesPart != null)
            {
                if (VisibleTimeSpan.Ticks == 0 || IsMainBand)
                {
                    VisibleDatesPart.Visibility = Visibility.Collapsed;
                }
                else
                {
                    VisibleDatesPart.Width = VisibleDatesAreaWidth;
                    VisibleDatesPart.Height = VisibleDatesAreaHeight;

                    VisibleDatesPart.SetValue(Canvas.LeftProperty, 
                        (CanvasPart.ActualWidth - VisibleDatesAreaWidth) / 2 + 1);

                    VisibleDatesPart.SetValue(Canvas.ZIndexProperty, 1);
                }
            }
        }

        public override string ToString(
            )
        {
            return String.Format(FMT_TRACE, Name, ItemSourceType, GetValue(Canvas.LeftProperty),
                GetValue(Canvas.TopProperty), ActualHeight, ActualWidth);
        }

        #endregion

        #region Private Methods and Properties

        private RectangleGeometry ClipRect
        {
            get
            {
                if (m_clipRect == null)
                {
                    m_clipRect = (RectangleGeometry) GetTemplateChild("ClipRect");
                }
                return m_clipRect;
            }
        }

        private string GetDataContext(
            int                                         index
        )
        {
            return TimelineCalendar.ItemToString(ItemsSource, ItemsSource[index]);
        }

        /// 
        /// <summary>
        /// Moves timeline according to mouse move during drag-drop</summary>
        /// 
        private void MoveScale(
            MouseEventArgs                              e
        )
        {
            Point                                       newPos;
            TimeSpan                                    span;

            Debug.Assert(m_calc != null);
            Debug.Assert(e != null);

            if (m_dragging)
            {
                newPos = e.GetPosition(m_canvasPart);

                if (m_needMove)
                {
                    span = m_calc.PixelsToTimeSpan(newPos.X - m_prevMousePoint.X);
                    SafeDateChange(span, true);
                }
                else
                {
                    m_needMove = true;
                }
                m_prevMousePoint = newPos;
            }
        }

        private void SafeDateChange(
            TimeSpan                                    span,
            bool                                        subtract
        )
        {
            bool                                        fixDate;

            fixDate = true;

            try
            {
                if (subtract)
                {
                    if (CurrentDateTime - span > Calculator.Calendar.MinDateTime)  
                    {
                        CurrentDateTime -= span;
                        fixDate = false;
                    }
                }
                else
                {
                    if (CurrentDateTime + span < Calculator.Calendar.MaxDateTime)  
                    {
                        CurrentDateTime += span;
                        fixDate = false;
                    }
                }
            }
            catch(ArgumentOutOfRangeException)
            {
                fixDate = true;
            }
            
            if (fixDate)
            {
                if (subtract)
                {
                    CurrentDateTime = Calculator.Calendar.MinDateTime;
                }
                else
                {
                    CurrentDateTime = Calculator.Calendar.MaxDateTime;
                }
            }
        }

        #endregion
    }
}
