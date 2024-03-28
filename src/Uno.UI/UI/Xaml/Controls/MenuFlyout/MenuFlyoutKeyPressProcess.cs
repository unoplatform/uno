using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;

namespace Windows.UI.Xaml.Controls
{
	class KeyPressMenuFlyoutPresenter
	{
		internal static bool KeyDown(VirtualKey key, MenuFlyoutPresenter control)
		{
			var pbHandled = false;

			if (key == VirtualKey.Up ||
				key == VirtualKey.GamepadDPadUp ||
				key == VirtualKey.GamepadLeftThumbstickUp)
			{
				control.HandleUpOrDownKey(false);
				pbHandled = true;
			}
			else if (key == VirtualKey.Down ||
					 key == VirtualKey.GamepadDPadDown ||
					 key == VirtualKey.GamepadLeftThumbstickDown)
			{
				control.HandleUpOrDownKey(true);
				pbHandled = true;
			}
			else if (key == VirtualKey.Tab)
			{
				pbHandled = true;
			}

			// Handle the left key to close the opened MenuFlyoutSubItem.
			// The right arrow key is directly handled from the MenuFlyoutSubItem
			// to open the sub menu item.
			else if (key == VirtualKey.Left ||
					 key == VirtualKey.Escape)
			{
				if (control.IsSubPresenter)
				{
					control.HandleKeyDownLeftOrEscape();
					pbHandled = true;
				}
				else
				{
					// If this is the top-level menu, let Popup close it (on Escape key press)
					pbHandled = false;
				}
			}

			return pbHandled;
		}
	}

	class KeyPressMenuFlyout
	{
		internal static bool KeyDown(VirtualKey key, MenuFlyoutItem control)
		{
			var pbHandled = false;

			// If SPACE/ENTER/NAVIGATION_ACCEPT/GAMEPAD_A is already down and a different key is now pressed,
			// then cancel the SPACE/ENTER/NAVIGATION_ACCEPT/GAMEPAD_A press.
			if (control.m_bIsSpaceOrEnterKeyDown || control.m_bIsNavigationAcceptOrGamepadAKeyDown)
			{
				if (key != VirtualKey.Space && key != VirtualKey.Enter && control.m_bIsSpaceOrEnterKeyDown)
				{
					control.m_bIsSpaceOrEnterKeyDown = false;
				}
				if (control.m_bIsNavigationAcceptOrGamepadAKeyDown &&             // The key down flag is set
					key != VirtualKey.GamepadA)                 // AND it's not the GamepadA key
				{
					control.m_bIsNavigationAcceptOrGamepadAKeyDown = false;
				}

				control.m_bIsPressed = false;
				control.UpdateVisualState();
			}

			if (key == VirtualKey.Up ||
				key == VirtualKey.GamepadDPadUp ||
				key == VirtualKey.GamepadLeftThumbstickUp)
			{
				MenuFlyoutPresenter spParentMenuFlyoutPresenter = control.GetParentMenuFlyoutPresenter();
				if (spParentMenuFlyoutPresenter != null)
				{
					spParentMenuFlyoutPresenter.HandleUpOrDownKey(false);
					pbHandled = true;
				}
			}
			else if (key == VirtualKey.Down ||
					 key == VirtualKey.GamepadDPadDown ||
					 key == VirtualKey.GamepadLeftThumbstickDown)
			{
				MenuFlyoutPresenter spParentMenuFlyoutPresenter = control.GetParentMenuFlyoutPresenter();
				if (spParentMenuFlyoutPresenter != null)
				{
					spParentMenuFlyoutPresenter.HandleUpOrDownKey(true);
					pbHandled = true;
				}
			}
			else if (key == VirtualKey.Space ||
					 key == VirtualKey.Enter ||
					 key == VirtualKey.GamepadA)
			{
				control.m_bIsPressed = true;

				if (key == VirtualKey.Space || key == VirtualKey.Enter)
				{
					control.m_bIsSpaceOrEnterKeyDown = true;
				}
				else if (key == VirtualKey.GamepadA)
				{
					control.m_bIsNavigationAcceptOrGamepadAKeyDown = true;
				}

				control.UpdateVisualState();
				pbHandled = true;
			}

			return pbHandled;
		}

		internal static bool KeyUp(VirtualKey key, MenuFlyoutItem control)
		{
			var pbHandled = false;

			if (key == VirtualKey.Space ||
				key == VirtualKey.Enter ||
				key == VirtualKey.GamepadA)
			{
				if (key == VirtualKey.Space || key == VirtualKey.Enter)
				{
					control.m_bIsSpaceOrEnterKeyDown = false;
				}
				else if (key == VirtualKey.GamepadA)
				{
					control.m_bIsNavigationAcceptOrGamepadAKeyDown = false;
				}

				if (control.m_bIsPressed && !control.m_bIsPointerLeftButtonDown)
				{
					control.m_bIsPressed = false;
					control.UpdateVisualState();
					control.Invoke();
					pbHandled = true;
				}
			}

			return pbHandled;
		}
	}
}
