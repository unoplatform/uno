// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Input
{
	public enum PointerUpdateKind
	{
		/// <summary>
		/// Pointer updates not identified by other PointerUpdateKind values.
		/// </summary>
		Other = 0,
		/// <summary>
		/// Left button pressed.
		/// </summary>
		LeftButtonPressed = 1,
		/// <summary>
		/// Left button released.
		/// </summary>
		LeftButtonReleased = 2,
		/// <summary>
		/// Right button pressed.
		/// </summary>
		RightButtonPressed = 3,
		/// <summary>
		/// Right button released.
		/// </summary>
		RightButtonReleased = 4,
		/// <summary>
		/// Middle button pressed.
		/// </summary>
		MiddleButtonPressed = 5,
		/// <summary>
		/// Middle button released.
		/// </summary>
		MiddleButtonReleased = 6,
		/// <summary>
		/// XBUTTON1 pressed.
		/// </summary>
		XButton1Pressed = 7,
		/// <summary>
		/// XBUTTON1 released.
		/// </summary>
		XButton1Released = 8,
		/// <summary>
		/// XBUTTON2 pressed.
		/// </summary>
		XButton2Pressed = 9,
		/// <summary>
		/// XBUTTON2 released.
		/// </summary>
		XButton2Released = 10,
	}
}
#endif
