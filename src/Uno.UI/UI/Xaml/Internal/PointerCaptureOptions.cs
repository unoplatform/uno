#nullable enable
using System;
using System.Linq;

namespace Uno.UI.Xaml.Core;

[Flags]
internal enum PointerCaptureOptions : byte
{
	None = 0,

	/// <summary>
	/// DEPRECATED: Setting ManipulationMode=None is equivalent to setting this option.
	/// 
	/// Indicates that the pointer capture should prevent the "direct manipulation" (a.k.a. OS steal) which would takes over the capture (mainly for scrolling using touch and pen).
	/// This applies for:
	///   * iOS and Android, where we configure the OS to forbid it to steal the pointer when it detects a scroll gesture.
	///   * Managed pointers (Skia), which prevents the direct manipulation to kick in (cf. InputManager.Pointers).
	/// </summary>
	PreventDirectManipulation = 1, // Note: We should actually rely on the ManipulationMode.System instead of this!
}
