// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// SliderKeyProcess.h

using Windows.System;

namespace Windows.UI.Xaml.Controls;

public partial class Slider
{
	internal static class KeyProcess
	{
		internal static void KeyDown(VirtualKey key, Slider control, out bool handled)
		{
			handled = false;

			var orientation = control.Orientation;
			var flowDirection = control.FlowDirection;
			var shouldReverse = control.IsDirectionReversed;

			var shouldInvert = (flowDirection == FlowDirection.RightToLeft) ^ shouldReverse;

			if (key == VirtualKey.Left ||
				(orientation == Orientation.Horizontal &&
				 (key == VirtualKey.GamepadDPadLeft ||
				  key == VirtualKey.GamepadLeftThumbstickLeft)))
			{
				control.Step(true, shouldInvert);
				handled = true;
			}
			else if (key == VirtualKey.Right ||
					 (orientation == Orientation.Horizontal &&
					  (key == VirtualKey.GamepadDPadRight ||
					   key == VirtualKey.GamepadLeftThumbstickRight)))
			{
				control.Step(true, !shouldInvert);
				handled = true;
			}
			else if (key == VirtualKey.Up ||
					 (orientation == Orientation.Vertical &&
					  (key == VirtualKey.GamepadDPadUp ||
					   key == VirtualKey.GamepadLeftThumbstickUp)))
			{
				control.Step(true, !shouldReverse);
				handled = true;
			}
			else if (key == VirtualKey.Down ||
					 (orientation == Orientation.Vertical &&
					  (key == VirtualKey.GamepadDPadDown ||
					   key == VirtualKey.GamepadLeftThumbstickDown)))
			{
				control.Step(true, shouldReverse);
				handled = true;
			}
			else if (key == VirtualKey.Home || key == VirtualKey.GamepadLeftShoulder)
			{
				var delta = control.Minimum;
				control.Value = delta;
				handled = true;
			}
			else if (key == VirtualKey.End || key == VirtualKey.GamepadRightShoulder)
			{
				var delta = control.Maximum;
				control.Value = delta;
				handled = true;
			}
		}
	}
}
