// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ConversionFunctions.h, ConversionFunctions.cpp

using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal static class FocusConversionFunctions
	{
		internal static FocusNavigationDirection GetFocusNavigationDirectionFromReason(XamlSourceFocusNavigationReason reason) =>
			reason switch
			{
				XamlSourceFocusNavigationReason.First => FocusNavigationDirection.Next,
				XamlSourceFocusNavigationReason.Last => FocusNavigationDirection.Previous,
				XamlSourceFocusNavigationReason.Left => FocusNavigationDirection.Left,
				XamlSourceFocusNavigationReason.Right => FocusNavigationDirection.Right,
				XamlSourceFocusNavigationReason.Up => FocusNavigationDirection.Up,
				XamlSourceFocusNavigationReason.Down => FocusNavigationDirection.Down,
				_ => FocusNavigationDirection.None,
			};

		internal static XamlSourceFocusNavigationReason? GetFocusNavigationReasonFromDirection(FocusNavigationDirection direction) =>
			direction switch
			{
				FocusNavigationDirection.Next => XamlSourceFocusNavigationReason.First,
				FocusNavigationDirection.Previous => XamlSourceFocusNavigationReason.Last,
				FocusNavigationDirection.None => XamlSourceFocusNavigationReason.Programmatic,
				FocusNavigationDirection.Left => XamlSourceFocusNavigationReason.Left,
				FocusNavigationDirection.Right => XamlSourceFocusNavigationReason.Right,
				FocusNavigationDirection.Up => XamlSourceFocusNavigationReason.Up,
				FocusNavigationDirection.Down => XamlSourceFocusNavigationReason.Down,
				_ => null,
			};

		internal static InputDeviceType GetInputDeviceTypeFromDirection(FocusNavigationDirection direction)
		{
			switch (direction)
			{
				case FocusNavigationDirection.Next:
				case FocusNavigationDirection.Previous:
					return InputDeviceType.Keyboard;
				case FocusNavigationDirection.Left:
				case FocusNavigationDirection.Right:
				case FocusNavigationDirection.Up:
				case FocusNavigationDirection.Down:
					// Bug: 14219605: OnFocusNavigating should include last input device
					// In XAML is it possible to do XY focus navigation via the keyboard or the gamepad
					// Currently when a OnFocusNavigating is received XAML has no way of knowing which device
					// was used to do XY navigation.
					// XAML assumes a Gamepad device but this might be wrong
					// in the cases where Keyboard navigation was used.
					return InputDeviceType.GamepadOrRemote;
				default:
					return InputDeviceType.None;
			}
		}

		internal static FocusNavigationDirection GetReverseDirection(FocusNavigationDirection direction) =>
			direction switch
			{
				FocusNavigationDirection.Left => FocusNavigationDirection.Right,
				FocusNavigationDirection.Right => FocusNavigationDirection.Left,
				FocusNavigationDirection.Up => FocusNavigationDirection.Down,
				FocusNavigationDirection.Down => FocusNavigationDirection.Up,
				FocusNavigationDirection.Next => FocusNavigationDirection.Previous,
				FocusNavigationDirection.Previous => FocusNavigationDirection.Next,
				_ => FocusNavigationDirection.None,
			};
	}
}
