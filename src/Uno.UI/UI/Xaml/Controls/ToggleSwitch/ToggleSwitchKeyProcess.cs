using Windows.System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleSwitch
	{
		private static class KeyProcess
		{
			internal static bool KeyDown(VirtualKey key, ToggleSwitch control)
			{
				if (control.HandlesKey(key))
				{
					return true;
				}

				return false;
			}

			internal static bool KeyUp(VirtualKey key, ToggleSwitch control)
			{
				var handled = false;
				bool shouldToggleOff = false;
				bool shouldToggleOn = false;
				bool handlesKey = false;
				bool handledKeyDown = false;
				bool isOn = false;

				var flowDirection = control.FlowDirection;
				bool isLTR = (flowDirection == FlowDirection.LeftToRight);

				handlesKey = control.HandlesKey(key);
				if (handlesKey)
				{
					handledKeyDown = control._handledKeyDown;
					control._handledKeyDown = false;
				}

				if (key == VirtualKey.GamepadA)
				{
					control.Toggle();
					handled = true;
				}

				if (handlesKey && handledKeyDown && (!handled && !control._isDragging))
				{
					isOn = control.IsOn;

					if ((key == VirtualKey.Left && isLTR)          // Left toggles us off in LTR
						|| (key == VirtualKey.Right && !isLTR)     // Right toggles us off in RTR
						|| key == VirtualKey.Down
						|| key == VirtualKey.Home)
					{
						shouldToggleOff = true;
					}
					else if ((key == VirtualKey.Right && isLTR)    // Right toggles us on in LTR
						|| (key == VirtualKey.Left && !isLTR)      // Left toggles us off in RTL
						|| key == VirtualKey.Up
						|| key == VirtualKey.End)
					{
						shouldToggleOn = true;
					}

					if ((!isOn && shouldToggleOn) || (isOn && shouldToggleOff) || key == VirtualKey.Space)
					{
						control.Toggle();
						handled = true;
					}
				}

				return handled;
			}
		}
	}
}
