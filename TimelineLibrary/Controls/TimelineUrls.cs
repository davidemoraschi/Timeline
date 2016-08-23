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
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace TimelineLibrary
{
    /// 
    /// <summary>
    /// Uri data for timeline. Data sould be all in //data/event format</summary>
    /// 
    public class UriInfo
    {
        public Uri Url
        {
            get;
            set;
        }

        public Uri IconUrl
        {
            get;
            set;
        }

        public Color EventColor
        {
            get;
            set;
        }
    }

    /// 
    /// <summary>
    /// List of urls to xml load data from. Data sould be all in //data/event format </summary>
    /// 
    public class TimelineUrlCollection: ObservableCollection<UriInfo>
    {
    }
}