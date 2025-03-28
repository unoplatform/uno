// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;

namespace Windows.UI.Xaml.Controls;

internal static class FocusHelper
{
	public static bool IsGamepadNavigationDirection(VirtualKey key)
	{
		return key == VirtualKey.GamepadLeftThumbstickDown
			|| key == VirtualKey.GamepadDPadDown
			|| key == VirtualKey.GamepadLeftThumbstickUp
			|| key == VirtualKey.GamepadDPadUp
			|| key == VirtualKey.GamepadLeftThumbstickRight
			|| key == VirtualKey.GamepadDPadRight
			|| key == VirtualKey.GamepadLeftThumbstickLeft
			|| key == VirtualKey.GamepadDPadLeft;
	}

	public static bool IsGamepadPageNavigationDirection(VirtualKey key)
	{
		return
			key == VirtualKey.GamepadLeftShoulder ||
			key == VirtualKey.GamepadRightShoulder ||
			key == VirtualKey.GamepadLeftTrigger ||
			key == VirtualKey.GamepadRightTrigger;
	}

	public static FocusNavigationDirection GetNavigationDirection(VirtualKey key)
	{
		FocusNavigationDirection direction = FocusNavigationDirection.None;

		switch (key)
		{
			case VirtualKey.GamepadDPadUp:
			case VirtualKey.GamepadLeftThumbstickUp:
			case VirtualKey.Up:
				direction = FocusNavigationDirection.Up;
				break;
			case VirtualKey.GamepadDPadDown:
			case VirtualKey.GamepadLeftThumbstickDown:
			case VirtualKey.Down:
				direction = FocusNavigationDirection.Down;
				break;
			case VirtualKey.GamepadDPadLeft:
			case VirtualKey.GamepadLeftThumbstickLeft:
			case VirtualKey.Left:
				direction = FocusNavigationDirection.Left;
				break;
			case VirtualKey.GamepadDPadRight:
			case VirtualKey.GamepadLeftThumbstickRight:
			case VirtualKey.Right:
				direction = FocusNavigationDirection.Right;
				break;
		}

		return direction;
	}

	public static FocusNavigationDirection GetPageNavigationDirection(VirtualKey key)
	{
		FocusNavigationDirection direction = FocusNavigationDirection.None;

		switch (key)
		{
			case VirtualKey.GamepadLeftTrigger:
				direction = FocusNavigationDirection.Up;
				break;
			case VirtualKey.GamepadRightTrigger:
				direction = FocusNavigationDirection.Down;
				break;
			case VirtualKey.GamepadLeftShoulder:
				direction = FocusNavigationDirection.Left;
				break;
			case VirtualKey.GamepadRightShoulder:
				direction = FocusNavigationDirection.Right;
				break;
		}

		return direction;
	}

	public static FocusNavigationDirection GetOppositeDirection(FocusNavigationDirection direction)
	{
		FocusNavigationDirection oppositeDirection = FocusNavigationDirection.None;
		switch (direction)
		{
			case FocusNavigationDirection.Down:
				oppositeDirection = FocusNavigationDirection.Up;
				break;
			case FocusNavigationDirection.Up:
				oppositeDirection = FocusNavigationDirection.Down;
				break;
			case FocusNavigationDirection.Left:
				oppositeDirection = FocusNavigationDirection.Right;
				break;
			case FocusNavigationDirection.Right:
				oppositeDirection = FocusNavigationDirection.Left;
				break;
			case FocusNavigationDirection.Next:
				oppositeDirection = FocusNavigationDirection.Previous;
				break;
			case FocusNavigationDirection.Previous:
				oppositeDirection = FocusNavigationDirection.Next;
				break;
		}
		return oppositeDirection;
	}

	public static UIElement GetUIElementForFocusCandidate(DependencyObject dobj)
	{
		var uielement = dobj as UIElement;
		var parent = dobj;
		while (uielement == null && parent != null)
		{
			parent = VisualTreeHelper.GetParent(dobj);
			if (parent is not null)
			{
				uielement = dobj as UIElement;
			}
		}

		return uielement;
	}
};
