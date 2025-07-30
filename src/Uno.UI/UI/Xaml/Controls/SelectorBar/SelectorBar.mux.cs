// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBar.cpp, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

#nullable enable

using System;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;
using static Uno.UI.Helpers.WinUI.CppWinRTHelpers;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class SelectorBar
{
	// Change to 'true' to turn on debugging outputs in Output window
	//bool SelectorBarTrace.s_IsDebugOutputEnabled { false };
	//bool SelectorBarTrace.s_IsVerboseDebugOutputEnabled { false };

	public SelectorBar()
	{
		// SELECTORBAR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_SelectorBar);

		// EnsureProperties();
		DefaultStyleKey = typeof(SelectorBar);

		var items = new ObservableVector<SelectorBarItem>();
		SetValue(ItemsProperty, items);

		Loaded += OnLoaded;
		// _loadedRevoker.Disposable = Disposable.Create(() => Loaded -= OnLoaded);
	}

	//SelectorBar.~public SelectorBar()
	//{
	//	// SELECTORBAR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
	//}

	// Invoked by SelectorBarTestHooks
	internal ItemsView? GetItemsViewPart() => _itemsView;

	protected override void OnApplyTemplate()
	{
		//// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		_itemsViewSelectedItemPropertyChangedRevoker.Disposable = null;

		base.OnApplyTemplate();

		//IControlProtected controlProtected{ this };

		_itemsView = (ItemsView)GetTemplateChild(s_itemsViewPartName);

		if (_itemsView is { } itemsView)
		{
			_itemsViewSelectedItemPropertyChangedRevoker.Disposable = RegisterPropertyChanged(
				itemsView,
				ItemsView.SelectedItemProperty,
				OnItemsViewSelectedItemPropertyChanged);

			if (itemsView.ScrollView is { } scrollView)
			{
				// Allow the items to scroll horizontally if the SelectorBar is sized too small horizontally (the default
				// ScrollView ContentOrientation is Vertical for vertical scrolling which is not useful for the SelectorBar).
				scrollView.ContentOrientation = ScrollingContentOrientation.Horizontal;
			}
		}

		if (SelectedItem != null)
		{
			ValidateSelectedItem();
			UpdateItemsViewSelectionFromSelectedItem();
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var dependencyProperty = args.Property;

		if (dependencyProperty == SelectedItemProperty)
		{
			// SELECTORBAR_TRACE_INFO_DBG(this, TRACE_MSG_METH_STR, METH_NAME, this, "SelectedItem property changed");

			ValidateSelectedItem();
			UpdateItemsViewSelectionFromSelectedItem();
			RaiseSelectionChanged();
		}
	}

	private void OnItemsViewSelectedItemPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		UpdateSelectedItemFromItemsView();
	}

	protected override void OnGotFocus(RoutedEventArgs args)
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		base.OnGotFocus(args);

		var selectedItem = SelectedItem;
		var selectedItemIsFocusable = selectedItem == null ? false : SharedHelpers.IsFocusableElement(selectedItem);

		if (selectedItem == null || !selectedItemIsFocusable)
		{
			// Automatically attempt to select an item when the SelectorBar got focus without any selection.
			if (_itemsView is { } itemsView)
			{
				int currentItemIndex = itemsView.CurrentItemIndex;

				if (currentItemIndex != -1)
				{
					// Select the current item index
					itemsView.Select(currentItemIndex);
				}
			}

			if (SelectedItem == null)
			{
				SelectFirstFocusableItem();
			}
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		if (SelectedItem == null)
		{
			UpdateSelectedItemFromItemsView();
		}
	}

	private void RaiseSelectionChanged()
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		SelectionChanged?.Invoke(this, new());
	}

	private void SelectFirstFocusableItem()
	{
		// SELECTORBAR_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(SelectedItem == null);

		var items = Items;
		var itemsCount = items.Count;

		for (int itemIndex = 0; itemIndex < itemsCount; itemIndex++)
		{
			var item = items[itemIndex];

			if (item != null && SharedHelpers.IsFocusableElement(item))
			{
				SelectedItem = item;
				break;
			}
		}
	}

	private void UpdateItemsViewSelectionFromSelectedItem()
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		if (_itemsView is { } itemsView)
		{
			var selectedItem = SelectedItem;

			if (selectedItem == null)
			{
				itemsView.DeselectAll();
			}
			else
			{
				int selectedIndex = Items.IndexOf(selectedItem);
				MUX_ASSERT(selectedIndex >= 0);
				itemsView.Select(selectedIndex);
			}
		}
	}

	private void UpdateSelectedItemFromItemsView()
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		if (_itemsView is { } itemsView)
		{
			var selectedItem = SelectedItem;
			var itemsViewSelectedItem = (SelectorBarItem)itemsView.SelectedItem;

			if (selectedItem != itemsViewSelectedItem)
			{
				SelectedItem = itemsViewSelectedItem;
			}
		}
	}

	private void ValidateSelectedItem()
	{
		// SELECTORBAR_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

		var selectedItem = SelectedItem;

		if (selectedItem is not null)
		{
			int selectedIndex = Items.IndexOf(selectedItem);
			var result = selectedIndex >= 0;
			if (!result)
			{
				throw new ArgumentException("SelectedItem must be an element of Items.");
			}
		}
	}
}
