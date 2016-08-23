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
using System.Windows.Browser;

namespace TimelineLibrary
{
    public partial class EventDetailsWindow : ChildWindow
    {
        public int                                      MOREINFO_WIDTH = 800;
        public int                                      MOREINFO_HEIGHT = 600;

        public EventDetailsWindow(
        )
        {
            InitializeComponent();
        }

        private void OnOkClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            this.DialogResult = true;
        }

        private void OnHyperlinkButtonClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            TimelineDisplayEvent                        ev;
            HtmlPopupWindowOptions                      options;

            ev = (TimelineDisplayEvent) this.DataContext;

            if (!String.IsNullOrEmpty(ev.Link))
            {
                options = new HtmlPopupWindowOptions();
                options.Width = MOREINFO_WIDTH;
                options.Height = MOREINFO_HEIGHT;

                HtmlPage.PopupWindow(new Uri(ev.Link), String.Empty, options); 
            }

        }
    }
}

