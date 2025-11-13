#if HAS_UNO_WINUI
using System;
using System.Text;

namespace Microsoft.UI.Input;

public enum PointerDeviceType
{
	/// <summary>
	/// A touch-enabled device
	/// </summary>
	Touch = 0,

	/// <summary>
	/// Pen
	/// </summary>
	Pen = 1,

	/// <summary>
	/// Mouse
	/// </summary>
	Mouse = 2,

	/// <summary>
	/// Touchpad
	/// </summary>
	Touchpad = 3,
}
#endif
