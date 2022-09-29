using System;

namespace Windows.UI.Input.Preview.Injection;

[Flags]
public enum InjectedInputTouchParameters : uint
{
	None = 0,

	Contact = 1,

	Orientation = 2,

	Pressure = 4,
}
