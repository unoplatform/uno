// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
#nullable enable

using System;
using System.Linq;

namespace Microsoft.UI.Input;

internal static class PointerPointPropertiesExtensions
{
	public static PointerPointProperties SetUpdateKindFromPrevious(this PointerPointProperties current, PointerPointProperties? previous)
	{
		if (previous is null)
		{
			return current;
		}

		// The PointerUpdateKind is not a [Flags] enum, so we allow only one pointer change.
		var result = PointerUpdateKind.Other;
		if (HasChanged(previous.IsLeftButtonPressed, current.IsLeftButtonPressed, PointerUpdateKind.LeftButtonPressed, PointerUpdateKind.LeftButtonReleased, ref result)
			|| HasChanged(previous.IsMiddleButtonPressed, current.IsMiddleButtonPressed, PointerUpdateKind.MiddleButtonPressed, PointerUpdateKind.MiddleButtonReleased, ref result)
			|| HasChanged(previous.IsRightButtonPressed, current.IsRightButtonPressed, PointerUpdateKind.RightButtonPressed, PointerUpdateKind.RightButtonReleased, ref result)
			|| HasChanged(previous.IsXButton1Pressed, current.IsXButton1Pressed, PointerUpdateKind.XButton1Pressed, PointerUpdateKind.XButton1Released, ref result)
			|| HasChanged(previous.IsXButton2Pressed, current.IsXButton2Pressed, PointerUpdateKind.XButton2Pressed, PointerUpdateKind.XButton2Released, ref result))
		{
			current.PointerUpdateKind = result;
		}

		return current;

		static bool HasChanged(bool was, bool @is, PointerUpdateKind pressed, PointerUpdateKind released, ref PointerUpdateKind update)
		{
			if (was == @is)
			{
				return false;
			}
			else if (was)
			{
				update = released;
				return true;
			}
			else
			{
				update = pressed;
				return true;
			}
		}
	}
}
#endif
