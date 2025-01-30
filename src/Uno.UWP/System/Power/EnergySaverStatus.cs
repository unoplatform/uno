namespace Windows.System.Power;

/// <summary>
/// Specifies the status of battery saver.
/// </summary>
public enum EnergySaverStatus
{
	/// <summary>
	/// Battery saver is off permanently or the device is plugged in.
	/// </summary>
	Disabled,

	/// <summary>
	/// Battery saver is off now, but ready to turn on automatically.
	/// </summary>
	Off,

	/// <summary>
	/// Battery saver is on. Save energy where possible.
	/// </summary>
	On,
}
