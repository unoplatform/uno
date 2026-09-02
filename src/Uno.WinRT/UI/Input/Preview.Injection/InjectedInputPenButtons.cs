#nullable enable

using System;

namespace Windows.UI.Input.Preview.Injection;

[Flags]
public enum InjectedInputPenButtons : uint
{
	None = 0,

	Barrel = 1,

	Inverted = 2,

	Eraser = 4,
}
