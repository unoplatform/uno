using System;
using System.Collections.Generic;

namespace Windows.Devices.Sensors;

public partial class CompassReading
{
	internal CompassReading(
		double headingMagneticNorth,
		double headingTrueNorth,
		DateTimeOffset timestamp)
	{
		HeadingMagneticNorth = headingMagneticNorth;
		HeadingTrueNorth = headingTrueNorth;
		Timestamp = timestamp;
	}


	public double HeadingMagneticNorth { get; }
	public double HeadingTrueNorth { get; }

	public DateTimeOffset Timestamp { get; }
}
