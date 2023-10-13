namespace Windows.System.Power;

/// <summary>
/// Represents the device's power supply status.
/// </summary>
public enum PowerSupplyStatus
{
	/// <summary>
	/// The device has no power supply.
	/// </summary>
	NotPresent,

	/// <summary>
	/// The device has an inadequate power supply.
	/// </summary>
	Inadequate,

	/// <summary>
	/// The device has an adequate power supply.
	/// </summary>
	Adequate
}
