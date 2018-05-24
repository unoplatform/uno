using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Geolocation
{
    public sealed partial class Geopoint
    {
        public Geopoint(BasicGeoposition position)
        {
            Position = position;
        }

        public BasicGeoposition Position { get; }
    }
}