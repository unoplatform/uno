#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Geolocation;

namespace Windows.Services.Maps
{
    public sealed partial class MapLocation
    {
        internal MapLocation(
            Geopoint point, 
            MapAddress address, 
            string displayName = "", 
            string description = ""
        )
        {
            Point = point;
            Address = address;
            DisplayName = displayName;
            Description = description;
        }
        
        public MapAddress Address { get; }
        public string Description { get; }
        public string DisplayName { get; }
        public Geopoint Point { get; }
    }
}