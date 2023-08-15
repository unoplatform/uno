using System;
using System.Collections.Generic;

namespace Windows.Devices.Sensors;

public partial class CompassReading
{
	internal CompassReading(
		double headingMagneticNorth,
		DateTimeOffset timestamp)
	{
		HeadingMagneticNorth = headingMagneticNorth;
		Timestamp = timestamp;
	}


	public double HeadingMagneticNorth { get; }

	public DateTimeOffset Timestamp { get; }
}
