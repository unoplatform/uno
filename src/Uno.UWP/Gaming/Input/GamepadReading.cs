using System;
using Windows.ApplicationModel;

namespace Windows.Gaming.Input;

/// <summary>
/// Represents the current state of the gamepad.
/// </summary>
public partial struct GamepadReading : IEquatable<GamepadReading>
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

	// NOTE: Equality implementation should be modified if a new field/property is added.

	#region Equality Members
	public override bool Equals(object? obj) => obj is GamepadReading reading && Equals(reading);
	public bool Equals(GamepadReading other) => Timestamp == other.Timestamp && Buttons == other.Buttons && LeftTrigger == other.LeftTrigger && RightTrigger == other.RightTrigger && LeftThumbstickX == other.LeftThumbstickX && LeftThumbstickY == other.LeftThumbstickY && RightThumbstickX == other.RightThumbstickX && RightThumbstickY == other.RightThumbstickY;

	public override int GetHashCode()
	{
		var hashCode = -1623113290;
		hashCode = hashCode * -1521134295 + Timestamp.GetHashCode();
		hashCode = hashCode * -1521134295 + Buttons.GetHashCode();
		hashCode = hashCode * -1521134295 + LeftTrigger.GetHashCode();
		hashCode = hashCode * -1521134295 + RightTrigger.GetHashCode();
		hashCode = hashCode * -1521134295 + LeftThumbstickX.GetHashCode();
		hashCode = hashCode * -1521134295 + LeftThumbstickY.GetHashCode();
		hashCode = hashCode * -1521134295 + RightThumbstickX.GetHashCode();
		hashCode = hashCode * -1521134295 + RightThumbstickY.GetHashCode();
		return hashCode;
	}

	public static bool operator ==(GamepadReading left, GamepadReading right) => left.Equals(right);
	public static bool operator !=(GamepadReading left, GamepadReading right) => !left.Equals(right);
	#endregion
}
