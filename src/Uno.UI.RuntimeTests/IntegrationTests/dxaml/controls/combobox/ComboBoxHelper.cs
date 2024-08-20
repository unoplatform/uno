// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Common;

internal class ComboBoxHelper
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

	//public static async Task FocusComboBoxIfNecessary(ComboBox comboBox)
	//{
	//	var comboBoxGotFocusEvent = std.make_shared<Event>();
	//	var gotFocusRegistration = CreateSafeEventRegistration(ComboBox, GotFocus);
	//	gotFocusRegistration.Attach(comboBox, [&]()

	//		{
	//		LOG_OUTPUT(L"[ComboBox]: Got Focus Event Fired.");
	//		comboBoxGotFocusEvent->Set();
	//	});

	//	bool alreadyHasFocus = false;
	//	RunOnUIThread([&]()

	//		{
	//		alreadyHasFocus = comboBox->FocusState != xaml.FocusState.Unfocused;
	//		comboBox->Focus(xaml.FocusState.Keyboard);
	//	});

	//	if (!alreadyHasFocus)
	//	{
	//		comboBoxGotFocusEvent->WaitForDefault();
	//	}
	//}

	public static async Task OpenComboBox(ComboBox comboBox, OpenMethod openMethod)
	{
		var comboBoxOpenedEvent = std.make_shared<Event>();
		var openedRegistration = CreateSafeEventRegistration(ComboBox, DropDownOpened);
		openedRegistration.Attach(comboBox, [&](){ comboBoxOpenedEvent->Set(); });

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
			FocusComboBoxIfNecessary(comboBox);
			TestServices.KeyboardHelper.PressKeySequence(" ");
		}
		else if (openMethod == OpenMethod.Gamepad)
		{
			FocusComboBoxIfNecessary(comboBox);
			CommonInputHelper.Accept(InputDevice.Gamepad);
		}
		else if (openMethod == OpenMethod.Programmatic)
		{
			await RunOnUIThread(() =>
			{
				comboBox.IsDropDownOpen = true;
			});
		}
		await TestServices.WindowHelper.WaitForIdle();

		comboBoxOpenedEvent->WaitForDefault();
	}

	public static async Task CloseComboBox(ComboBox comboBox)
	{
		await CloseComboBox(comboBox, CloseMethod.Programmatic);
	}

	public static async Task CloseComboBox(ComboBox comboBox, CloseMethod closeMethod)
	{
		var dropDownClosedEvent = std.make_shared<Event>();
		var dropDownClosedRegistration = CreateSafeEventRegistration(ComboBox, DropDownClosed);
		dropDownClosedRegistration.Attach(comboBox, [&](){ dropDownClosedEvent->Set(); });

		if (closeMethod == CloseMethod.Touch || closeMethod == CloseMethod.Mouse)
		{
			Rect dropdownBounds = GetBoundsOfOpenDropdown(comboBox);
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
			CommonInputHelper.Cancel(InputDevice.Keyboard);
		}
		else if (closeMethod == CloseMethod.Gamepad)
		{
			CommonInputHelper.Cancel(InputDevice.Gamepad);
		}
		else if (closeMethod == CloseMethod.Programmatic)
		{
			await RunOnUIThread(() =>
			{
				comboBox->IsDropDownOpen = false;
			});
		}

		dropDownClosedEvent->WaitForDefault();
		Private.Infrastructure.await TestServices.WindowHelper.WaitForIdle();

		dropDownClosedRegistration.Detach();
	}

	static async Rect GetBoundsOfOpenDropdown(xaml.DependencyObject^ element)
	{
		Rect dropdownBounds = { };

		await RunOnUIThread(() =>

			{
				var dropdownScrollViewer = safe_cast < ScrollViewer ^> (TreeHelper.GetVisualChildByNameFromOpenPopups(L"ScrollViewer", element));
				WEX.Common.Throw.IfFalse(dropdownScrollViewer != nullptr, E_FAIL, L"DropDown not found.");
				dropdownBounds = ControlHelper.GetBounds(dropdownScrollViewer);

				LOG_OUTPUT(L"dropdownBounds: (%f, %f, %f, %f)", dropdownBounds.X, dropdownBounds.Y, dropdownBounds.Width, dropdownBounds.Height);

			});
		Private.Infrastructure.await TestServices.WindowHelper.WaitForIdle();

		return dropdownBounds;
	}

	// Verify the selected Index on the ComboBox.
	static void VerifySelectedIndex(ComboBox comboBox, int expected)
	{
		SelectorHelper.VerifySelectedIndex(comboBox, expected);
	}

	// Uses touch to select the ComboBoxItem with the specified index.
	// Note: The function does not currently scroll the popup, so it won't work if the item to be selected
	// is not immediately visible.
	public static async Task SelectItemWithTap(ComboBox comboBox, int index)
	{
		await OpenComboBox(comboBox, OpenMethod.Touch);
		await TestServices.WindowHelper.WaitForIdle();

		Event selectionChangedEvent;
		var selectionChangedRegistration = CreateSafeEventRegistration(ComboBox, SelectionChanged);
		selectionChangedRegistration.Attach(comboBox, [&](){ selectionChangedEvent.Set(); });

		ComboBoxItem comboBoxItemToSelect;
		RunOnUIThread([&]()

			{
			comboBoxItemToSelect = safe_cast < ComboBoxItem ^> (comboBox->ContainerFromIndex(index));
			THROW_IF_NULL(comboBoxItemToSelect);
		});

		TestServices.InputHelper.Tap(comboBoxItemToSelect);
		selectionChangedEvent.WaitForDefault();
	}
}
