#nullable enable

using System;

namespace Windows.UI.Input.Preview.Injection;

[Flags]
public enum InjectedInputMouseOptions : uint
{
	None = 0,

	Move = 1,

	LeftDown = 2,

	LeftUp = 4,

	RightDown = 8,

	RightUp = 16,

	MiddleDown = 32,

	MiddleUp = 64,

	XDown = 128,

	XUp = 256,

	Wheel = 2048,

	HWheel = 4096,

	MoveNoCoalesce = 8192,

	/// <remarks>
	/// Uno has no cross-platform notion of a multi-monitor virtual desktop, so this flag has no effect:
	/// normalized coordinates set via <see cref="Absolute"/> always map onto the current XamlRoot's bounds,
	/// with or without this flag.
	/// </remarks>
	VirtualDesk = 16384,

	/// <remarks>
	/// On Uno, the "display surface" normalized coordinates map onto is the current XamlRoot's bounds -
	/// there is no cross-platform notion of screen/monitor resolution independent of the app window - and
	/// this mapping is unaffected by <see cref="VirtualDesk"/>.
	/// </remarks>
	Absolute = 32768,
}
