// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Windows.UI.Xaml.Tests.Common;

internal static class ComboBoxHelper
{
	public enum OpenMethod
	{
		Mouse,
		Touch,
		Keyboard,
		Gamepad,
		Programmatic
	};

	public enum CloseMethod
	{
		Mouse,
		Touch,
		Keyboard,
		Gamepad,
		Programmatic
	};

	public static async Task FocusComboBoxIfNecessary(ComboBox comboBox)
	{
		var comboBoxGotFocusEvent = new Event();
		var gotFocusRegistration = CreateSafeEventRegistration<ComboBox, RoutedEventHandler>("GotFocus");
		gotFocusRegistration.Attach(comboBox, (s, e) =>
		{
			LOG_OUTPUT("[ComboBox]: Got Focus Event Fired.");
			comboBoxGotFocusEvent.Set();
		});

		bool alreadyHasFocus = false;
		await RunOnUIThread(() =>
		{
			alreadyHasFocus = comboBox.FocusState != FocusState.Unfocused;
			comboBox.Focus(FocusState.Keyboard);
		});

		if (!alreadyHasFocus)
		{
			await comboBoxGotFocusEvent.WaitForDefault();
		}
	}

	public static async Task OpenComboBox(ComboBox comboBox, OpenMethod openMethod)
	{
		var comboBoxOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownOpened");
		openedRegistration.Attach(comboBox, (s, e) => { comboBoxOpenedEvent.Set(); });

		if (openMethod == OpenMethod.Mouse)
		{
			TestServices.InputHelper.LeftMouseClick(comboBox);
		}
		else if (openMethod == OpenMethod.Touch)
		{
			TestServices.InputHelper.Tap(comboBox);
		}
		else if (openMethod == OpenMethod.Keyboard)
		{
			await FocusComboBoxIfNecessary(comboBox);
			await TestServices.KeyboardHelper.PressKeySequence(" ");
		}
		else if (openMethod == OpenMethod.Gamepad)
		{
			await FocusComboBoxIfNecessary(comboBox);
			await CommonInputHelper.Accept(InputDevice.Gamepad);
		}
		else if (openMethod == OpenMethod.Programmatic)
		{
			await RunOnUIThread(() =>
			{
				comboBox.IsDropDownOpen = true;
			});
		}
		await TestServices.WindowHelper.WaitForIdle();

		await comboBoxOpenedEvent.WaitForDefault();
	}

	public static async Task CloseComboBox(ComboBox comboBox)
	{
		await CloseComboBox(comboBox, CloseMethod.Programmatic);
	}

	public static async Task CloseComboBox(ComboBox comboBox, CloseMethod closeMethod)
	{
		var dropDownClosedEvent = new Event();
		var dropDownClosedRegistration = CreateSafeEventRegistration<ComboBox, EventHandler<object>>("DropDownClosed");
		dropDownClosedRegistration.Attach(comboBox, (s, e) => { dropDownClosedEvent.Set(); });

		if (closeMethod == CloseMethod.Touch || closeMethod == CloseMethod.Mouse)
		{
			Rect dropdownBounds = await GetBoundsOfOpenDropdown(comboBox);
			int outsideBuffer = 10; // Tap at least this far away from the dropdown in order to ensure that it closes.
			var closeTapPoint = new Point(dropdownBounds.X + dropdownBounds.Width + outsideBuffer, dropdownBounds.Y + dropdownBounds.Height + outsideBuffer);

			if (closeMethod == CloseMethod.Touch)
			{
				TestServices.InputHelper.Tap(closeTapPoint);
			}
			else
			{
				TestServices.InputHelper.LeftMouseClick(closeTapPoint);
			}
		}
		else if (closeMethod == CloseMethod.Keyboard)
		{
			await CommonInputHelper.Cancel(InputDevice.Keyboard);
		}
		else if (closeMethod == CloseMethod.Gamepad)
		{
			await CommonInputHelper.Cancel(InputDevice.Gamepad);
		}
		else if (closeMethod == CloseMethod.Programmatic)
		{
			await RunOnUIThread(() =>
			{
				comboBox.IsDropDownOpen = false;
			});
		}

		await dropDownClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		dropDownClosedRegistration.Detach();
	}

	private static async Task<Rect> GetBoundsOfOpenDropdown(DependencyObject element)
	{
		Rect dropdownBounds = new();

		await RunOnUIThread(async () =>
		{
			var dropdownScrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByNameFromOpenPopups("ScrollViewer", element));
			Assert.IsNotNull(dropdownScrollViewer, "DropDown not found.");
			dropdownBounds = await ControlHelper.GetBounds(dropdownScrollViewer);

			LOG_OUTPUT("dropdownBounds: (%f, %f, %f, %f)", dropdownBounds.X, dropdownBounds.Y, dropdownBounds.Width, dropdownBounds.Height);
		});
		await TestServices.WindowHelper.WaitForIdle();

		return dropdownBounds;
	}

	// Verify the selected Index on the ComboBox.
	public static async Task VerifySelectedIndex(ComboBox comboBox, int expected)
	{
		await SelectorHelper.VerifySelectedIndex(comboBox, expected);
	}

	// Uses touch to select the ComboBoxItem with the specified index.
	// Note: The function does not currently scroll the popup, so it won't work if the item to be selected
	// is not immediately visible.
	public static async Task SelectItemWithTap(ComboBox comboBox, int index)
	{
		await OpenComboBox(comboBox, OpenMethod.Touch);
		await TestServices.WindowHelper.WaitForIdle();

		Event selectionChangedEvent = new();
		var selectionChangedRegistration = CreateSafeEventRegistration<ComboBox, SelectionChangedEventHandler>("SelectionChanged");
		selectionChangedRegistration.Attach(comboBox, (s, e) => selectionChangedEvent.Set());

		ComboBoxItem comboBoxItemToSelect = null;
		await RunOnUIThread(() =>
		{
			comboBoxItemToSelect = (ComboBoxItem)comboBox.ContainerFromIndex(index);
			THROW_IF_NULL(comboBoxItemToSelect);
		});

		TestServices.InputHelper.Tap(comboBoxItemToSelect);
		await selectionChangedEvent.WaitForDefault();
	}
}
