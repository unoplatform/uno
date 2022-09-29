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

	VirtualDesk = 16384,

	Absolute = 32768,
}
