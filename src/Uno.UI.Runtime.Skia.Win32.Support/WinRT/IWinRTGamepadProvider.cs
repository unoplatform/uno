using System;

namespace Uno.UI.Runtime.Skia.Win32.Support.WinRT;

/// <summary>
/// Provides gamepad input data using WinRT APIs.
/// Uses only primitive types to avoid namespace clashes with Uno.UWP.
/// </summary>
public interface IWinRTGamepadProvider : IDisposable
{
	/// <summary>
	/// Gets the IDs of all currently connected gamepads.
	/// </summary>
	int[] GetConnectedGamepadIds();

	/// <summary>
	/// Gets the current reading for the specified gamepad.
	/// </summary>
	WinRTGamepadReading GetReading(int gamepadId);

	/// <summary>
	/// Raised when a gamepad is connected.
	/// </summary>
	event Action<int>? GamepadConnected;

	/// <summary>
	/// Raised when a gamepad is disconnected.
	/// </summary>
	event Action<int>? GamepadDisconnected;

	/// <summary>
	/// Starts monitoring for gamepad connect/disconnect events.
	/// </summary>
	void StartMonitoring();

	/// <summary>
	/// Stops monitoring for gamepad connect/disconnect events.
	/// </summary>
	void StopMonitoring();
}

/// <summary>
/// Represents a snapshot of gamepad state using primitive types.
/// Button flags match the WinRT GamepadButtons enum values.
/// </summary>
public readonly record struct WinRTGamepadReading(
	ulong Timestamp,
	uint Buttons,
	double LeftTrigger,
	double RightTrigger,
	double LeftThumbstickX,
	double LeftThumbstickY,
	double RightThumbstickX,
	double RightThumbstickY);
