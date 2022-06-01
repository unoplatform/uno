namespace Windows.Gaming.Input;

/// <summary>
/// Represents the current state of the gamepad.
/// </summary>
public partial struct GamepadReading
{
	/// <summary>
	/// Time when the state was retrieved from the gamepad.
	/// </summary>
	public ulong Timestamp;

	/// <summary>
	/// The state of the gamepad's buttons. This will be a combination
	/// of values in the GamepadButtons enumeration.
	/// </summary>
	public GamepadButtons Buttons;

	/// <summary>
	/// The position of the left trigger. The value is between 0.0 (not depressed) and 1.0 (fully depressed).
	/// </summary>
	public double LeftTrigger;

	/// <summary>
	/// The position of the right trigger. The value is between 0.0 (not depressed) and 1.0 (fully depressed).
	/// </summary>
	public double RightTrigger;

	/// <summary>
	/// The position of the left thumbstick on the X-axis. The value is between -1.0 and 1.0.
	/// </summary>
	public double LeftThumbstickX;

	/// <summary>
	/// The position of the left thumbstick on the Y-axis. The value is between -1.0 and 1.0.
	/// </summary>
	public double LeftThumbstickY;

	/// <summary>
	/// The position of the right thumbstick on the X-axis.The value is between -1.0 and 1.0.
	/// </summary>
	public double RightThumbstickX;

	/// <summary>
	/// The position of the right thumbstick on the Y-axis. The value is between -1.0 and 1.0.
	/// </summary>
	public double RightThumbstickY;
}
