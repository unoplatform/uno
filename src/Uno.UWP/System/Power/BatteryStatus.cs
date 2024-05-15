namespace Windows.System.Power;

/// <summary>
/// Indicates the status of the battery.
/// </summary>
public enum BatteryStatus
{
	/// <summary>
	/// The battery or battery controller is not present.
	/// </summary>
	NotPresent,

	/// <summary>
	/// The battery is discharging.
	/// </summary>
	Discharging,

	/// <summary>
	/// The battery is idle.
	/// </summary>
	Idle,

	/// <summary>
	/// The battery is charging.
	/// </summary>
	Charging,
}
