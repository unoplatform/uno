// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using static Private.Infrastructure.TestServices;

namespace Windows.UI.Xaml.Tests.Common;

public static class LoopingSelectorHelper
{
	public static async Task PanSingleDateTimeLoopingSelector()
	{
		var loopingSelectors = await GetAllLoopingSelectors();
		VERIFY_IS_TRUE(loopingSelectors.Count > 0);

		var loopingSelectorFirst = loopingSelectors[0];
		await DoLoopingSelectorSelectionChange(loopingSelectorFirst);
	}

	public static async Task PanDateTimeLoopingSelector()
	{
		var loopingSelectors = await GetAllLoopingSelectors();
		VERIFY_IS_TRUE(loopingSelectors.Count == 3);

		var loopingSelectorFirst = loopingSelectors[0];
		await DoLoopingSelectorSelectionChange(loopingSelectorFirst);
		await ValidateLoopingSelectorPanel(loopingSelectorFirst);

		var loopingSelectorSecond = loopingSelectors[1];
		await DoLoopingSelectorSelectionChange(loopingSelectorSecond);
		await ValidateLoopingSelectorPanel(loopingSelectorSecond);

		var loopingSelectorThird = loopingSelectors[2];
		await DoLoopingSelectorSelectionChange(loopingSelectorThird);
		await ValidateLoopingSelectorPanel(loopingSelectorThird);
	}

	internal static async Task<LoopingSelector> GetLoopingSelector(string namePickerHost)
	{
		LoopingSelector loopingSelector = null;

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(
				TestServices.WindowHelper.WindowContent.XamlRoot);

			for (int i = 0; i < popups.Count; ++i)
			{
				var popup = popups[i];
				var pickerHost = TreeHelper.GetVisualChildByName(popup.Child as FrameworkElement, namePickerHost) as Border;

				if (pickerHost is not null)
				{
					loopingSelector = pickerHost.Child as LoopingSelector;
					VERIFY_IS_NOT_NULL(loopingSelector);
					break;
				}
			}
		});

		return loopingSelector;
	}

	internal static async Task<IList<LoopingSelector>> GetAllLoopingSelectors()
	{
		var result = new List<LoopingSelector>();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.WindowContent.XamlRoot);

			for (int i = 0; i < popups.Count; ++i)
			{
				var popup = popups[i];
				var popupChild = popup.Child;

				TreeHelper.GetVisualChildrenByType<LoopingSelector>(popupChild, ref result);
			}
		});

		return result;
	}

	public static async Task ValidateLoopingSelectorPanel(LoopingSelector loopingSelector)
	{
		await RunOnUIThread(() =>
			{
				var scrollViewer = (ScrollViewer)TreeHelper.GetVisualChildByName(loopingSelector, "ScrollViewer");
				var loopingSelectorPanel = scrollViewer.Content as LoopingSelectorPanel;

				VERIFY_IS_NOT_NULL(loopingSelectorPanel);
				VERIFY_IS_TRUE(loopingSelectorPanel.AreHorizontalSnapPointsRegular);
				VERIFY_IS_TRUE(loopingSelectorPanel.AreVerticalSnapPointsRegular);

				loopingSelectorPanel.GetRegularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center, out var offset);
				VERIFY_ARE_EQUAL(offset, 0.0f);
			});
	}

	public static async Task DoLoopingSelectorSelectionChange(LoopingSelector loopingSelector)
	{
		throw new NotImplementedException("PanFromCenter is not implemented yet");
		//bool selectionChangedEvent = false;
		//var selectionChangedRegistration = CreateSafeEventRegistration<LoopingSelector, SelectionChangedEventHandler>("SelectionChanged");

		//await RunOnUIThread(() =>
		//{
		//	selectionChangedRegistration.Attach(
		//		loopingSelector,
		//		(object sender, SelectionChangedEventArgs args) =>
		//	{
		//		LOG_OUTPUT("DoLoopingSelectorSelectionChange: SelectionChanged event fired. new selection index=%d", loopingSelector.SelectedIndex);

		//		var selectedItem = loopingSelector.SelectedItem;
		//		VERIFY_IS_NOT_NULL(selectedItem);

		//		var items = loopingSelector.Items;
		//		LOG_OUTPUT("VerifyProperties: items size==%d ", items.Count);
		//		var selectedItemFromIndex = items[loopingSelector.SelectedIndex];
		//		VERIFY_IS_NOT_NULL(selectedItemFromIndex);
		//		VERIFY_ARE_EQUAL(selectedItem, selectedItemFromIndex);

		//		selectionChangedEvent = true;
		//	});
		//});

		//await TestServices.WindowHelper.WaitForIdle();

		//// These values were changed to work around Task 24429189: DCPP Test: InputManagerXaml.dll InjectPressAndDrag does not work correctly on 64 bit OS
		//InputHelper.PanFromCenter(loopingSelector, 0 /*relX*/, -100 /*relY*/, 10.0 /*velocityFactor*/);
		//await WindowHelper.WaitFor(() => selectionChangedEvent);
	}


	public enum SelectionMode
	{
		Keyboard,
		UpDownButtons
	}

	public static async Task SelectItemByIndex(LoopingSelector loopingSelector, int indexOfItemToSelect, SelectionMode selectionMode)
	{
		bool selectionChangedEvent = false;
		var selectionChangedRegistration = CreateSafeEventRegistration<LoopingSelector, SelectionChangedEventHandler>("SelectionChanged");
		selectionChangedRegistration.Attach(loopingSelector, (s, e) => selectionChangedEvent = true);

		int selectedIndex = -1;
		await RunOnUIThread(() =>
		{
			selectedIndex = loopingSelector.SelectedIndex;
		});

		if (selectionMode == SelectionMode.UpDownButtons)
		{
			TestServices.InputHelper.MoveMouse(loopingSelector);
			await TestServices.WindowHelper.WaitForIdle();

			ButtonBase upButton = null;
			ButtonBase downButton = null;
			await RunOnUIThread(() =>
			{
				upButton = TreeHelper.GetVisualChildByName(loopingSelector, "UpButton") as ButtonBase;
				downButton = TreeHelper.GetVisualChildByName(loopingSelector, "DownButton") as ButtonBase;
				THROW_IF_NULL(upButton);
				THROW_IF_NULL(downButton);
			});

			int delta = indexOfItemToSelect - selectedIndex;
			var buttonToTap = delta > 0 ? downButton : upButton;

			for (int i = 0; i < Math.Abs(delta); i++)
			{
				await TestServices.WindowHelper.WaitForIdle();
				TestServices.InputHelper.LeftMouseClick(buttonToTap);
				await WindowHelper.WaitFor(() => selectionChangedEvent);
				selectionChangedEvent = false;
			}
		}
		else if (selectionMode == SelectionMode.Keyboard)
		{
			await ControlHelper.EnsureFocused(loopingSelector);

			var upKeySequence = "$d$_up#$u$_up";
			var downKeySequence = "$d$_down#$u$_down";

			int delta = indexOfItemToSelect - selectedIndex;
			var keySequenceToUse = delta > 0 ? downKeySequence : upKeySequence;

			for (int i = 0; i < Math.Abs(delta); i++)
			{
				await TestServices.WindowHelper.WaitForIdle();
				TestServices.KeyboardHelper.PressKeySequence(keySequenceToUse);
				await TestServices.WindowHelper.WaitFor(() => selectionChangedEvent);
				selectionChangedEvent = false;
			}
		}

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(indexOfItemToSelect, loopingSelector.SelectedIndex);
		});
	}

	// Given a particular value to look for, return any LoopingSelectorItems in the child collection that have the same value.
	// Assumes that CreateTestLoopingSelector creates ints.
	//public static LoopingSelectorItem FindLoopingSelectorItemByInt<TCollection>(TCollection childCollection, int value)
	//{
	//	for (int i = 0; i < childCollection.Count; i++)
	//	{
	//		var child = childCollection[i] as LoopingSelectorItem;
	//		int childValue = (int)child.Content;
	//		if (childValue == value)
	//		{
	//			return child;
	//		}
	//	}
	//	return null;
	//}
}
