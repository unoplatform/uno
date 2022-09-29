#nullable disable

using System;

namespace Windows.Gaming.Input;

/// <summary>
/// Specifies the button type.
/// </summary>
[Flags]
public enum GamepadButtons : uint
{
	/// <summary>
	/// No button.
	/// </summary>
	None = 0,

	/// <summary>
	/// Menu button.
	/// </summary>
	Menu = 1,

	/// <summary>
	/// View button.
	/// </summary>
	View = 2,

	/// <summary>
	/// A button.
	/// </summary>
	A = 4,

	/// <summary>
	/// B button.
	/// </summary>
	B = 8,

	/// <summary>
	/// X button.
	/// </summary>
	X = 16,

	/// <summary>
	/// Y button.
	/// </summary>
	Y = 32,

	/// <summary>
	/// D-pad up.
	/// </summary>
	DPadUp = 64,

	/// <summary>
	/// D-pad down.
	/// </summary>
	DPadDown = 128,

	/// <summary>
	/// D-pad left.
	/// </summary>
	DPadLeft = 256,

	/// <summary>
	/// D-pad right.
	/// </summary>
	DPadRight = 512,

	/// <summary>
	/// Left bumper.
	/// </summary>
	LeftShoulder = 1024,

	/// <summary>
	/// Right bumper.
	/// </summary>
	RightShoulder = 2048,

	/// <summary>
	/// Left stick.
	/// </summary>
	LeftThumbstick = 4096,

	/// <summary>
	/// Right stick.
	/// </summary>
	RightThumbstick = 8192,

	/// <summary>
	/// The first paddle.
	/// </summary>
	Paddle1 = 16384,

	/// <summary>
	///	The second paddle.
	/// </summary>
	Paddle2 = 32768,

	/// <summary>
	/// The third paddle.
	/// </summary>
	Paddle3 = 65536,

	/// <summary>
	/// The fourth paddle.
	/// </summary>
	Paddle4 = 131072,
}
