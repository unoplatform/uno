// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ButtonBaseKeyProcess.h

using Windows.System;
using static Uno.UI.Xaml.Input.KeyPress;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase
	{
		private static class KeyProcess
		{
			internal static bool IsPress(VirtualKey key, bool acceptsReturn)
			{
				// Hitting the SPACE, ENTER, or GAMEPAD_A key is equivalent to pressing the pointer
				// button
				return (key == VirtualKey.Space ||
					   (key == VirtualKey.Enter && acceptsReturn) ||
					   (key == VirtualKey.GamepadA));
			}

			internal static void KeyDown(VirtualKey key, out bool handled, bool acceptsReturn, ButtonBase control)
			{
				handled = false;

				var isEnabled = control.IsEnabled;
				var clickMode = control.ClickMode;

				// Key presses can be ignored when disabled or in ClickMode.Hover
				if (isEnabled &&
					clickMode != ClickMode.Hover)
				{
					if (IsPress(key, acceptsReturn))
					{
						// Ignore the SPACE/ENTER/GAMEPAD_A key if we already have the pointer captured
						// or if it had been pressed previously.
						if (!control._isPointerCaptured && !control._isSpaceOrEnterKeyDown && !control._isNavigationAcceptOrGamepadAKeyDown)
						{
							if (key == VirtualKey.Space || (key == VirtualKey.Enter && acceptsReturn))
							{
								control._isSpaceOrEnterKeyDown = true;
							}
							else if (key == VirtualKey.GamepadA)
							{
								control._isNavigationAcceptOrGamepadAKeyDown = true;
							}

							control.IsPressed = true;


							if (control.ClickMode == ClickMode.Press)
							{
								control.OnClick();
							}

							handled = true;

							StartFocusPress(control);
						}
					}
					// Any other keys pressed are irrelevant
					else if (control._isSpaceOrEnterKeyDown || control._isNavigationAcceptOrGamepadAKeyDown)
					{
						control.IsPressed = false;

						control._isSpaceOrEnterKeyDown = false;
						control._isNavigationAcceptOrGamepadAKeyDown = false;
					}
				}
			}

			internal static void KeyUp(VirtualKey key, out bool handled, bool acceptsReturn, ButtonBase control)
			{
				handled = false;

				var isEnabled = control.IsEnabled;
				var clickMode = control.ClickMode;

				// Key presses can be ignored when disabled or in xaml_controls::ClickMode.Hover or if any
				// other key than SPACE, ENTER, or GAMEPAD_A was released.
				if (isEnabled &&
					clickMode != ClickMode.Hover &&
					IsPress(key, acceptsReturn))
				{
					if (key == VirtualKey.Space || (key == VirtualKey.Enter && acceptsReturn))
					{
						control._isSpaceOrEnterKeyDown = false;
					}
					else if (key == VirtualKey.GamepadA)
					{
						control._isNavigationAcceptOrGamepadAKeyDown = false;
					}

					// If the pointer isn't in use, raise the Click event if we're in the
					// correct click mode
					if (!control._isPointerLeftButtonDown)
					{
						var iPressed = control.IsPressed;

						if (iPressed && clickMode == ClickMode.Release)
						{
							control.OnClick();
						}

						control.IsPressed = false;
					}

					handled = true;

					EndFocusPress(control);
				}
			}
		}
	}
}
