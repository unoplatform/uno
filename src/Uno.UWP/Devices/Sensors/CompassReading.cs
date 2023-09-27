using System;

namespace Windows.Devices.Sensors;

/// <summary>
/// Represents a compass reading.
/// </summary>
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

	/// <summary>
	/// Gets the heading in degrees relative to magnetic-north.
	/// </summary>
	public double HeadingMagneticNorth { get; }

	/// <summary>
	/// Gets the heading in degrees relative to geographic true-north.
	/// </summary>
	public double? HeadingTrueNorth { get; }

	/// <summary>
	/// Gets the time at which the sensor reported the reading.
	/// </summary>
	public DateTimeOffset Timestamp { get; }
}
