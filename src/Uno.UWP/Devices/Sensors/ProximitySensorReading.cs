#nullable enable

using System;

namespace Windows.Devices.Sensors;

/// <summary>
/// Represents a reading from the proximity sensor.
/// </summary>
public partial class ProximitySensorReading
{
	internal ProximitySensorReading(uint? distanceInMillimeters, DateTimeOffset timestamp)
	{
		DistanceInMillimeters = distanceInMillimeters;
		Timestamp = timestamp;
	}

	/// <summary>
	/// Gets the distance from the proximity sensor to the detected object.
	/// </summary>
	public uint? DistanceInMillimeters { get; }

	/// <summary>
	/// Gets whether or not an object is detected by the proximity sensor.
	/// </summary>
	public bool IsDetected => DistanceInMillimeters is not null;

	/// <summary>
	/// Gets the time for the most recent proximity sensor reading.
	/// </summary>
	public DateTimeOffset Timestamp { get; }
}
