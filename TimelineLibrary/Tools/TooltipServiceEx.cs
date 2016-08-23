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
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TimelineLibrary
{
    /// 
    /// <summary>
    /// This service extends functionality of standard ToolTipService by allowing tooltip stay on 
    /// the screen till timeout occurs</summary>
    /// 
    public static class TooltipServiceEx
    {
        public static readonly DependencyProperty ToolTipExProperty = 
            DependencyProperty.RegisterAttached("ToolTipEx", typeof(ToolTipEx), 
            typeof(TooltipServiceEx), new PropertyMetadata(OnEventToolTipPropertyChanged));


        public static ToolTipEx                        LastTooltip;

        private static void OnEventToolTipPropertyChanged(
            DependencyObject                            d, 
            DependencyPropertyChangedEventArgs          e
        )
        {
            FrameworkElement                            element;

            element = (FrameworkElement) d;

            element.Loaded += OnParentLoaded;
            element.MouseMove += OnMouseMove;
            element.MouseLeftButtonDown += OnMouseDown;
            element.MouseLeave += OnMouseLeave;
        }

        static void OnMouseLeave(
            object                                      sender, 
            MouseEventArgs                              e
        )
        {
            HideTooltip(sender);
        }

        static void OnMouseDown(
            object                                      sender, 
            MouseButtonEventArgs                        e
        )
        {
            HideTooltip(sender);
        }

        static void OnMouseMove(
            object                                      sender, 
            MouseEventArgs                              e
        )
        {
            HideTooltip(sender);
        }

        static void HideTooltip(
            object                                      sender
        )
        {
            FrameworkElement                            owner;
            ToolTipEx                                   tooltip;

            owner = (FrameworkElement) sender;
            tooltip = owner.GetValue(TooltipServiceEx.ToolTipExProperty) as ToolTipEx;

            if (tooltip != null)
            {
                tooltip.Hide();
            }
            LastTooltip = null;
        }

        static void OnParentLoaded(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            FrameworkElement                            owner;
            ToolTipEx                                   tooltip;
            ToolTip                                     orgTooltip;

            owner = (FrameworkElement) sender;

            owner.Loaded -= OnParentLoaded;
            tooltip = owner.GetValue(TooltipServiceEx.ToolTipExProperty) as ToolTipEx;
            orgTooltip = owner.GetValue(ToolTipService.ToolTipProperty) as ToolTip;

            tooltip.Tooltip = orgTooltip;
        }

        public static void SetToolTipEx(
            DependencyObject                            o,
            ToolTipEx                                   t
        )
        {
            o.SetValue(ToolTipExProperty, t);
        }

        public static ToolTipEx GetToolTipEx(
            DependencyObject                            o
        )
        {
            return o.GetValue(ToolTipExProperty) as ToolTipEx;
        }
    }

    public class ToolTipEx
    {
        private DispatcherTimer                          m_timer;
        private int                                      m_timeLeft;
        private ToolTip                                  m_tooltip;

        public ToolTip Tooltip
        {
            get
            {
                return m_tooltip;
            }

            set 
            {
                if (m_tooltip != null)
                {
                    m_tooltip.Opened -= OnTooltipOpened;
                    m_tooltip.Closed -= OnTooltipClosed;
                }

                m_tooltip = value;

                if (m_tooltip != null)
                {
                    m_tooltip.Opened += OnTooltipOpened;
                    m_tooltip.Closed += OnTooltipClosed;
                }
            }
        }

        public void Hide(
        )
        {
            if (m_tooltip.IsOpen)
            {
                StopTimer();
                m_tooltip.IsOpen = false;
            }
        }

        /// 
        /// <summary>
        /// Tooltip timeout interval in seconds, 0 for infinite</summary>
        /// 
        public int HideToolTipTimeout
        {
            get;
            set;
        }

        void OnTooltipClosed(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            if (m_timeLeft > 0)
            {
                m_tooltip.IsOpen = true;
            }
            else
            {
                StopTimer();
                TooltipServiceEx.LastTooltip = null;
            }
        }

        private void StopTimer(
        )
        {
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer.Tick -= OnTimerTick;
                m_timer = null;
                m_timeLeft = 0;
            }
        }

        void OnTooltipOpened(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            m_timer = new DispatcherTimer();
            
            
            m_timer.Interval = new TimeSpan(0, 0, 1);
            m_timer.Tick += OnTimerTick;
            m_timeLeft = HideToolTipTimeout;
            m_timer.Start();

            if (TooltipServiceEx.LastTooltip != null)
            {
                TooltipServiceEx.LastTooltip.Hide();
            }
            TooltipServiceEx.LastTooltip = this;
        }

        void OnTimerTick(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            --m_timeLeft;
            m_tooltip.IsOpen = m_timeLeft > 0;
        }
    }
}
