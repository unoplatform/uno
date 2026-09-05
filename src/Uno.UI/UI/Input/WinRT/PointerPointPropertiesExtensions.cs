#nullable enable

using System;
using System.Linq;

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

internal static class PointerPointPropertiesExtensions
{
	public static global::Microsoft.UI.Input.PointerPointProperties SetUpdateKindFromPrevious(this global::Microsoft.UI.Input.PointerPointProperties current, global::Microsoft.UI.Input.PointerPointProperties? previous)
	{
		if (previous is null)
		{
			return current;
		}

		// The global::Microsoft.UI.Input.PointerUpdateKind is not a [Flags] enum, so we allow only one pointer change.
		var result = global::Microsoft.UI.Input.PointerUpdateKind.Other;
		if (HasChanged(previous.IsLeftButtonPressed, current.IsLeftButtonPressed, global::Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed, global::Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased, ref result)
			|| HasChanged(previous.IsMiddleButtonPressed, current.IsMiddleButtonPressed, global::Microsoft.UI.Input.PointerUpdateKind.MiddleButtonPressed, global::Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased, ref result)
			|| HasChanged(previous.IsRightButtonPressed, current.IsRightButtonPressed, global::Microsoft.UI.Input.PointerUpdateKind.RightButtonPressed, global::Microsoft.UI.Input.PointerUpdateKind.RightButtonReleased, ref result)
			|| HasChanged(previous.IsXButton1Pressed, current.IsXButton1Pressed, global::Microsoft.UI.Input.PointerUpdateKind.XButton1Pressed, global::Microsoft.UI.Input.PointerUpdateKind.XButton1Released, ref result)
			|| HasChanged(previous.IsXButton2Pressed, current.IsXButton2Pressed, global::Microsoft.UI.Input.PointerUpdateKind.XButton2Pressed, global::Microsoft.UI.Input.PointerUpdateKind.XButton2Released, ref result))
		{
			current.PointerUpdateKind = result;
		}

		return current;

		static bool HasChanged(bool was, bool @is, global::Microsoft.UI.Input.PointerUpdateKind pressed, global::Microsoft.UI.Input.PointerUpdateKind released, ref global::Microsoft.UI.Input.PointerUpdateKind update)
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
