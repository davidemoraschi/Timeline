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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace TimelineLibrary
{
    [TemplatePart(Name = TimelineToolbox.TP_MAIN_GRID_PART, Type = typeof(Grid))]
    public class TimelineToolbox : Control, ITimelineToolbox
    {
        private const string                            TP_MAIN_GRID_PART = "MainGrid";
        ITimelineToolboxTarget                          m_target;
        Grid                                            m_grid;


        public TimelineToolbox()
        {
            this.DefaultStyleKey = typeof(TimelineToolbox);
        }

        public void SetSite(
            ITimelineToolboxTarget                      target
        )
        {
            m_target = target;
        }

        public override void OnApplyTemplate(
        )
        {
            Utilities.Trace(this);

            base.OnApplyTemplate();
            m_grid = (Grid) GetTemplateChild(TP_MAIN_GRID_PART);

            if (m_grid != null)
            {
                HookButtonEvents(m_grid.Children);
            }
        }

        private void HookButtonEvents(
            UIElementCollection                         col
        )
        {
            Button                                      b;
            Panel                                       p;

            
            foreach (FrameworkElement el in col)
            {
                b = el as Button;
                p = el as Panel;

                if (b != null)
                {
                    b.Click += OnButtonClick;
                }
                else if (p != null)
                {
                    HookButtonEvents(p.Children);
                }
            }
        }

        public void OnButtonClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            FrameworkElement                            el;

            Debug.Assert(m_target != null);

            el = (FrameworkElement) sender;

            switch (el.Name.ToLower())
            {
                case "fullscreen":
                    Application.Current.Host.Content.IsFullScreen = 
                        !Application.Current.Host.Content.IsFullScreen;
                    break;

                case "zoomin":
                    m_target.ZoomIn();
                    break;

                case "zoomout":
                    m_target.ZoomOut();
                    break;

                case "findfirst":
                    m_target.FindMinDate();
                    break;

                case "findlast":
                    m_target.FindMaxDate();
                    break;

                case "moveleft":
                    m_target.MoveLeft();
                    break;

                case "moveright":
                    m_target.MoveRight();
                    break;

                default:
                    throw new ArgumentException("Toolbox command cannot be found");
            }

        }
    }
}
