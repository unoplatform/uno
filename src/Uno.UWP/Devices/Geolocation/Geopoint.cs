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

		public GeoshapeType GeoshapeType => GeoshapeType.Geopoint;

#if __WASM__
		public Geopoint(BasicGeoposition position, AltitudeReferenceSystem altitudeReferenceSystem) : this(position)
		{
			AltitudeReferenceSystem = altitudeReferenceSystem;
		}

		public Geopoint(BasicGeoposition position, AltitudeReferenceSystem altitudeReferenceSystem, uint spatialReferenceId) : this(position, altitudeReferenceSystem)
		{
			SpatialReferenceId = spatialReferenceId;
		}

		public AltitudeReferenceSystem AltitudeReferenceSystem { get; }
		
		public uint SpatialReferenceId { get; }
#endif
	}
}
