using System;

namespace Windows.UI.Input.Preview.Injection;

[Flags]
public enum InjectedInputPointerOptions : uint
{
	None = 0,

	New = 1,

	// Pointer properties
	InRange = 2,
	InContact = 4,
	Primary = 8192,
	Confidence = 16384,
	Canceled = 32768,

	// Pointer update target
	FirstButton = 16,
	SecondButton = 32,

	// Pointers update type
	PointerDown = 65536,
	Update = 131072,
	PointerUp = 262144,

	CaptureChanged = 2097152,
}
