using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Geolocation
{
	public partial interface IGeoshape
	{
		AltitudeReferenceSystem AltitudeReferenceSystem { get; }
		GeoshapeType GeoshapeType { get; }
		uint SpatialReferenceId { get; }
		// Forced skipping of method Windows.Devices.Geolocation.IGeoshape.GeoshapeType.get
		// Forced skipping of method Windows.Devices.Geolocation.IGeoshape.SpatialReferenceId.get
		// Forced skipping of method Windows.Devices.Geolocation.IGeoshape.AltitudeReferenceSystem.get
	}
}
