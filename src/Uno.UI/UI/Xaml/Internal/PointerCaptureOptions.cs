#nullable enable
using System;
using System.Linq;

namespace Uno.UI.Xaml.Core;

[Flags]
internal enum PointerCaptureOptions : byte
{
	None = 0,

	/// <summary>
	/// This applies mainly for iOS and Android, where the OS can steal the pointer when it detects a scroll gesture (touch and pen).
	/// </summary>
	PreventOsStole = 1,
}
