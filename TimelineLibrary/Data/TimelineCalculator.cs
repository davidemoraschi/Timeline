using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Collections;
using System.Xml.Linq;

namespace TimelineLibrary
{
    public class TimelineCalculator: TimelineCalculatorBase
    {
        private Canvas                                  m_canvas;            
        private DataTemplate                            m_template;
        private DataTemplate                            m_eventTemplate;
        private TimelineBand                            m_parent;

        public TimelineCalculator(
            TimelineBand                                band,
            Canvas                                      canvas,
            DataTemplate                                template,
            int                                         columnCount,
            TimelineCalendar                            timeline,
            DataTemplate                                eventTemplate,
            double                                      maxEventHeight, 
            bool                                        assignRows,
            DateTime                                    currDateTime
        ): 
            base(canvas.ActualWidth, canvas.ActualHeight, columnCount, 
                 timeline, maxEventHeight, assignRows, currDateTime) 
        {
            Debug.Assert(template != null);
            Debug.Assert(canvas != null);
            Debug.Assert(eventTemplate != null);
            Debug.Assert(band != null);

            m_parent = band;
            m_eventTemplate = eventTemplate;
            m_canvas = canvas;
            m_template = template;;

            Utilities.Trace(this);
        }

        public override object CreateColumn(
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
            Debug.Assert(base.ColumnPixelWidth > 0);

            el = m_template.LoadContent() as FrameworkElement;

            el.DataContext = content;
            m_canvas.Children.Add(el);

            el.SetValue(Canvas.LeftProperty, left);
            el.SetValue(Canvas.TopProperty, 0.0);
            el.Width = base.ColumnPixelWidth + 1;
            el.Height = height;

            return el;
        }

        public override double GetLeftPixelPosition(
            object                                      element
        )
        {
            FrameworkElement                            el;

            el = (FrameworkElement) element;

            Debug.Assert(el != null);

            return (double) el.GetValue(Canvas.LeftProperty);
        }

        public override void MoveColumn(
            object                                      element,
            double                                      left,
            string                                      content
        )
        {
            FrameworkElement                            el;

            Debug.Assert(element != null);
            Debug.Assert(content != null);

            el = (FrameworkElement) element;

            if (((string) el.DataContext) != content)
            {
                el.DataContext = content;
            }
            el.SetValue(Canvas.LeftProperty, left);
        }

        public override object CreateEvent(
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
            element.DataContext = de;
            element.SetValue(Canvas.ZIndexProperty, 2);
            
            link = (HyperlinkButton) element.FindName("EventLinkTextBlock");

            if (link != null)
            {
                link.Click += m_parent.OnMoreInfoClick;
            }

            m_canvas.Children.Add(element);
            return element;
        }

        public override void MoveEvent(
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

        public override void RemoveEvent(
            object                                      e
        )
        {
            RemoveElement(e);
        }

        public override void RemoveColumn(
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

            Debug.Assert(removed);
        }
    }
}
