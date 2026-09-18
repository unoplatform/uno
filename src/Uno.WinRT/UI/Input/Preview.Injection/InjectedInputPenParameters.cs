#nullable enable

using System;

namespace Windows.UI.Input.Preview.Injection;

[Flags]
public enum InjectedInputPenParameters : uint
{
	None = 0,

	Pressure = 1,

	Rotation = 2,

	TiltX = 4,

	TiltY = 8,
}
