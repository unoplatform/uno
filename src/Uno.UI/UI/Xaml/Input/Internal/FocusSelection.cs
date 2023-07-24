// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusSelection.h, FocusSelection.cpp

#nullable enable

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal class FocusSelection
	{
		internal struct DirectionalFocusInfo
		{
			public DirectionalFocusInfo(bool handled, bool shouldBubble, bool focusCandidateFound, bool directionalFocusEnabled)
			{
				Handled = handled;
				ShouldBubble = shouldBubble;
				FocusCandidateFound = focusCandidateFound;
				DirectionalFocusEnabled = directionalFocusEnabled;
			}

			public static DirectionalFocusInfo Default => new DirectionalFocusInfo(false, true, false, false);

			public bool Handled { get; set; }

			public bool ShouldBubble { get; set; }

			public bool FocusCandidateFound { get; set; }

			public bool DirectionalFocusEnabled { get; set; }
		}

		internal static bool ShouldUpdateFocus(DependencyObject element, FocusState focusState)
		{
			return !(focusState == FocusState.Pointer && GetAllowFocusOnInteraction(element) == false);
		}

		internal static bool GetAllowFocusOnInteraction(DependencyObject element)
		{
			if (element is TextElement textElement)
			{
				return textElement.AllowFocusOnInteraction;
			}
			else if (element is FlyoutBase flyoutBase)
			{
				return flyoutBase.AllowFocusOnInteraction;
			}
			else if (element is FrameworkElement frameworkElement)
			{
				return frameworkElement.AllowFocusOnInteraction;
			}

			return true;
		}

		internal static FocusNavigationDirection GetNavigationDirection(VirtualKey key)
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

		internal static FocusNavigationDirection GetNavigationDirectionForKeyboardArrow(VirtualKey key)
		{
			if (key is VirtualKey.Up)
			{
				return FocusNavigationDirection.Up;
			}
			else if (key is VirtualKey.Down)
			{
				return FocusNavigationDirection.Down;
			}
			else if (key is VirtualKey.Left)
			{
				return FocusNavigationDirection.Left;
			}
			else if (key is VirtualKey.Right)
			{
				return FocusNavigationDirection.Right;
			}

			return FocusNavigationDirection.None;
		}

		internal static DirectionalFocusInfo TryDirectionalFocus(IFocusManager focusManager, FocusNavigationDirection direction, DependencyObject searchScope)
		{
			var info = DirectionalFocusInfo.Default;

			if (direction == FocusNavigationDirection.Next ||
				direction == FocusNavigationDirection.Previous ||
				direction == FocusNavigationDirection.None)
			{
				return info;
			}

			// We do not want to process direction focus if the element is not a UIElement (ie. Hyperlink)
			if (!(searchScope is UIElement uiElement))
			{
				return info;
			}

			var mode = uiElement.XYFocusKeyboardNavigation;

			if (mode == XYFocusKeyboardNavigationMode.Disabled)
			{
				info.ShouldBubble = false;
			}
			else if (mode == XYFocusKeyboardNavigationMode.Enabled)
			{
				info.DirectionalFocusEnabled = true;
				var xyFocusOptions = XYFocusOptions.Default;
				xyFocusOptions.SearchRoot = searchScope;
				xyFocusOptions.ShouldConsiderXYFocusKeyboardNavigation = true;

				var candidate = focusManager.FindNextFocus(new FindFocusOptions(direction), xyFocusOptions, null, true);

				if (candidate != null)
				{
					FocusMovementResult result = focusManager.SetFocusedElement(new FocusMovement(candidate, direction, FocusState.Keyboard));
					info.Handled = result.WasMoved;
					info.FocusCandidateFound = true;
				}
			}

			return info;
		}
	}
}
