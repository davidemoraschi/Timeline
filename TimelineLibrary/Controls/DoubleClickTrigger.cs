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
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Threading;

namespace TimelineLibrary
{
    public class DoubleClickTrigger:  TriggerBase<UIElement>
    {
        private const int                               DBLCLK_TIMEOUT = 300;
        private readonly DispatcherTimer                m_timer;

        public event MouseButtonEventHandler            OnDoubleClick;

        public UIElement AttachedObject
        {
            get
            {
                return AssociatedObject;
            }
        }

        public DoubleClickTrigger(
        )
        {
            m_timer = new DispatcherTimer();
            m_timer.Interval = new TimeSpan(0, 0, 0, 0, DBLCLK_TIMEOUT);
            m_timer.Tick += OnTimer;
        }
     
        protected override void OnAttached(
        )
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnMouseButtonDown;
        }

        protected override void OnDetaching(
        )
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= OnMouseButtonDown;

            if(m_timer.IsEnabled)
            {
                m_timer.Stop();
            }
        }

        private void OnMouseButtonDown(
            object                                      sender, 
            MouseButtonEventArgs                        e
        )
        {
            if(!m_timer.IsEnabled)
            {
                m_timer.Start();
            }
            else
            {
                m_timer.Stop();
                InvokeActions(e);
                if (OnDoubleClick != null)
                {
                    OnDoubleClick(this, e);
                }
            }
        }

        private void OnTimer(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            m_timer.Stop();
        }
    }
}
