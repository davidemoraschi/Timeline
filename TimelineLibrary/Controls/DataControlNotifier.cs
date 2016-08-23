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
using System.IO;
using System.Net;
using System.Windows;

namespace TimelineLibrary
{
    /// 
    /// <summary>
    /// This class aggregates resize events of TimelineBand controls and loadcomplete of datasources, so
    /// that we know when all controls are resized and all xml files downloaded.</summary>
    /// 
    public class DataControlNotifier
    {
        #region Private Fields
        
        List<TimelineBand>                              m_elements;
        private TimelineUrlCollection                   m_urls;
        private List<Stream>                            m_streams;

        private int                                     m_dataLoadCount;
        private int                                     m_sizeCount;
        private bool                                    m_started;
        
        #endregion

        public event EventHandler                       LoadComplete;

        #region Ctors

        public DataControlNotifier(
        )
        {
            m_elements = new List<TimelineBand>();
            m_urls = new TimelineUrlCollection();
            m_streams = new List<Stream>();
        }

        public DataControlNotifier(
            TimelineUrlCollection                       urls,
            List<TimelineBand>                          bands
        )
        {
            m_elements = bands;
            m_urls = urls;
            m_streams = new List<Stream>();

            foreach (FrameworkElement e in m_elements)
            {
                e.SizeChanged += OnSizeChanged;
            }

            StartDataDownload();
        }

        #endregion

        #region Public Methods and Properties

        public void AddElement(
            TimelineBand                                band
        )
        {
            Debug.Assert(band != null);

            band.SizeChanged += OnSizeChanged;
            m_elements.Add(band);
        }

        public void AddUrls(
            TimelineUrlCollection                       urls
        )
        {
           Debug.Assert(urls != null);

           m_urls = urls;
           StartDataDownload();
        }

        /// 
        /// <summary>
        /// After the class issues LoadComplete event this list contains
        /// all streams with data xmls from urls passed through AddUrls method</summary>
        /// 
        public List<Stream> StreamList
        {
            get
            {
                return m_streams;
            }
        }

        public void Start(
        )
        {
            Debug.Assert(LoadComplete != null);

            Utilities.Trace(this);

            m_started = true;
        }

        /// 
        /// <summary>
        /// Checks that all controls resized and all data received</summary>
        /// 
        public void CheckCompleted(
        )
        {
            if (m_started && 
                m_sizeCount == m_elements.Count && 
                m_dataLoadCount == m_urls.Count &&
                LoadComplete != null)
            {
                Utilities.Trace(this, "All data collected and all controls resized.");
                LoadComplete(this, new EventArgs());
                m_started = false;
                m_sizeCount = 0;
            }
        }

        #endregion

        #region Private Methods and Properties

        private void StartDataDownload(
        )
        {
            WebClient                                   client;

            foreach (UriInfo i in m_urls)
            {
                client = new WebClient();
                client.OpenReadCompleted += OnDataReadCompleted;
                client.OpenReadAsync(i.Url);
            }
        }


        private void OnSizeChanged(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            ((FrameworkElement) sender).SizeChanged -= OnSizeChanged;
            
            ++m_sizeCount;

            CheckCompleted();
        }

        /// 
        /// <summary>
        /// Occures every time next xml data file is available or error</summary>
        /// 
        private void OnDataReadCompleted(
            object                                      sender, 
            OpenReadCompletedEventArgs                  args
        )
        {
            Utilities.Trace(this);

            ++m_dataLoadCount;

            if (!args.Cancelled && args.Error == null)
            {
                m_streams.Add(args.Result);
            }
            CheckCompleted();

        }
        #endregion 
    }
}
