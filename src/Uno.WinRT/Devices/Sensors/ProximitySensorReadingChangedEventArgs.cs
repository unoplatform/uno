#nullable enable

using System;

namespace Windows.Devices.Sensors;

/// <summary>
/// Provides data for the reading– changed event of the proximity sensor.
/// </summary>
public partial class ProximitySensorReadingChangedEventArgs
{
	internal ProximitySensorReadingChangedEventArgs(ProximitySensorReading reading) =>
		Reading = reading ?? throw new ArgumentNullException(nameof(reading));

	/// <summary>
	/// Gets the most recent proximity sensor reading.
	/// </summary>
	public ProximitySensorReading Reading { get; }
}
