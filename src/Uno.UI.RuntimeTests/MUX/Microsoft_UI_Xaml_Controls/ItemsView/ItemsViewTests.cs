#if HAS_UNO_WINUI
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Automation.Peers;
using Private.Infrastructure;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
#if !__SKIA__
[Ignore("Only works properly on Skia")]
#endif
public class ItemsViewTests : MUXApiTestBase
{
	private const int c_MaxWaitDuration = 5000;

	private const double c_defaultUIItemsViewWidth = 200.0;
	private const double c_defaultUIItemsViewHeight = 400.0;

	public IEnumerable<int> Digits
	{
		get
		{
			return Enumerable.Range(0, 10);
		}
	}

	private enum LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger
	{
		ItemsRangeStartIndexNegative,
		ItemsRangeStartIndexIncreased,
		ItemsRangeStartIndexTooSmall,
		ArrayLengthSmallerThanItemsRangeRequestedLength,
		ArrayLengthTooSmallForDecreasedItemsRangeStartIndex,
		ArrayLengthInconsistent
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ItemsView default property values.")]
	public void VerifyDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			ItemsView itemsView = new ItemsView();
			Verify.IsNotNull(itemsView);

			Log.Comment("Logging ItemsView default property values");
			LogItemsViewProperties(itemsView);

			Log.Comment("Verifying ItemsView default property values");
			Verify.IsNull(itemsView.ItemTemplate);
			Verify.IsNull(itemsView.ItemsSource);
			Verify.IsNull(itemsView.SelectedItem);
			Verify.IsNull(itemsView.Layout);
			Verify.IsTrue(itemsView.IsEnabled);
			Verify.IsTrue(itemsView.IsTabStop);
			Verify.AreEqual(XYFocusKeyboardNavigationMode.Auto, itemsView.XYFocusKeyboardNavigation);
			Verify.AreEqual(KeyboardNavigationMode.Local, itemsView.TabNavigation);
			Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
			Verify.AreEqual(0, itemsView.SelectedItems.Count());
			Verify.AreEqual(-1, itemsView.CurrentItemIndex);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Loads an ItemsView, verifies properties and unloads.")]
	[Ignore("Uno-specific: Fails for not-yet known reason. To be investigated.")]
	public async Task VerifyPropertyValuesAfterTemplateApplication()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ScrollView"))
		{
			ItemsView itemsView = null;
			Random rnd = new Random();
			List<string> itemsSource = new List<string>(Enumerable.Range(0, 50).Select(k => k + " - " + rnd.Next(100)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView() { ItemsSource = itemsSource };

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Logging ItemsView property values after Loaded event");
				LogItemsViewProperties(itemsView);

				Log.Comment("Verifying ItemsView property values after Loaded event");
				Verify.IsNotNull(itemsView.ItemTemplate);
				Verify.IsNotNull(itemsView.ItemsSource);
				Verify.IsNull(itemsView.SelectedItem);
				Verify.IsNotNull(itemsView.Layout);
				Verify.IsTrue(itemsView.Layout is StackLayout);
				Verify.IsTrue(itemsView.IsEnabled);
				Verify.IsFalse(itemsView.IsTabStop);
				Verify.AreEqual(XYFocusKeyboardNavigationMode.Disabled, itemsView.XYFocusKeyboardNavigation);
				Verify.AreEqual(KeyboardNavigationMode.Once, itemsView.TabNavigation);
				Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
				Verify.AreEqual(0, itemsView.SelectedItems.Count());
				Verify.AreEqual(-1, itemsView.CurrentItemIndex);

				Verify.AreEqual(c_defaultUIItemsViewWidth, itemsView.ActualWidth);
				Verify.AreEqual(c_defaultUIItemsViewHeight, itemsView.ActualHeight);

				Log.Comment("Verifying ScrollView property values after Loaded event");
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(itemsView);

				Verify.IsNotNull(scrollView);
				Verify.AreEqual(c_defaultUIItemsViewWidth, scrollView.ViewportWidth);
				Verify.AreEqual(c_defaultUIItemsViewHeight, scrollView.ViewportHeight);

				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);

			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Garbage collecting...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Loads an ItemsView, sets properties and unloads.")]
	public async Task VerifyPropertySetters()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ScrollView"))
		{
			ItemsView itemsView = null;
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemsSource");
				Random rnd = new Random();
				List<string> itemsSource = new List<string>(Enumerable.Range(0, 50).Select(k => k + " - " + rnd.Next(100)));
				Verify.IsNull(itemsView.ItemTemplate);
				Verify.IsNull(itemsView.ItemsSource);
				itemsView.ItemsSource = itemsSource;
				Verify.IsNotNull(itemsView.ItemTemplate);
				Verify.AreEqual(itemsSource, itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemTemplate");
				DataTemplate itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Red'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting Layout");
				UniformGridLayout uniformGridLayout = new UniformGridLayout()
				{
					MaximumRowsOrColumns = 4,
					MinColumnSpacing = 10,
					MinRowSpacing = 10,
					MinItemWidth = 100,
					MinItemHeight = 100
				};
				Verify.IsNotNull(itemsView.Layout);
				Verify.IsTrue(itemsView.Layout is StackLayout);
				itemsView.Layout = uniformGridLayout;
				Verify.IsTrue(itemsView.Layout is UniformGridLayout);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting SelectionMode");
				Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
				itemsView.SelectionMode = ItemsViewSelectionMode.None;
				Verify.AreEqual(ItemsViewSelectionMode.None, itemsView.SelectionMode);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Changing ItemTemplate");
				DataTemplate itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Green'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemsView.ItemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.IsNotNull(itemsView.ItemTemplate);
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Changing ItemsSource");
				Random rnd = new Random();
				List<string> itemsSource = new List<string>(Enumerable.Range(0, 50).Select(k => k + " - " + rnd.Next(100)));
				Verify.IsNotNull(itemsView.ItemsSource);
				itemsView.ItemsSource = itemsSource;
				Verify.IsNotNull(itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting Layout");
				Verify.IsNotNull(itemsView.Layout);
				itemsView.Layout = null;
				Verify.IsNull(itemsView.Layout);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting ItemsSource");
				Verify.IsNotNull(itemsView.ItemsSource);
				itemsView.ItemsSource = null;
				Verify.IsNull(itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);

			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Garbage collecting...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Loads and populates an ItemsView with an ItemsSource made of ItemContainers.")]
	public async Task CanUseItemsSourceWithItemContainers()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView"))
		{
			ItemsView itemsView = null;
			List<ItemContainer> itemsSource = new List<ItemContainer>();
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				for (int i = 0; i < 3; i++)
				{
					ItemContainer itemContainer = new ItemContainer()
					{
						Name = "itemContainer" + i.ToString(),
						Child = new TextBlock()
						{
							Text = i.ToString()
						}
					};
					itemsSource.Add(itemContainer);
				}

				itemsView = new ItemsView() { ItemsSource = itemsSource };
				SetupDefaultUI(itemsView, itemsViewLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Logging ItemsView property values after Loaded event");
				LogItemsViewProperties(itemsView);

				Log.Comment("Accessing inner ItemsRepeater");
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);
				Verify.IsNotNull(itemsRepeater);

				Log.Comment("Verifying ItemsRepeater children count");
				int childrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);
				Verify.AreEqual(3, childrenCount);

				Log.Comment($"Extracting first ItemContainer, children count: {childrenCount}");
				ItemContainer itemContainer = itemsRepeater.FindVisualChildByType<ItemContainer>();
				Verify.IsNotNull(itemContainer);

				Log.Comment("Verifying ItemsContainer name");
				Verify.AreEqual("itemContainer0", itemContainer.Name);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Loads and populates an ItemsView, changes SelectionMode property to all enum values.")]
	public async Task CanChangeSelectionModeProperty()
	{
		List<string> types = new List<string>(3);
		types.Add("ItemsView");
		types.Add("ItemsRepeater");
		types.Add("ItemContainer");

		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper(types, isLoggingInfoLevel: true, isLoggingVerboseLevel: true))
		{
			ItemsView itemsView = null;
			ItemContainer itemContainer = null;
			Random rnd = new Random();
			List<string> itemsSource = new List<string>(Enumerable.Range(0, 10).Select(k => k + " - " + rnd.Next(100)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.Layout is StackLayout);
				Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemTemplate with ItemContainer");
				DataTemplate itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Blue'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemsSource");
				itemsView.ItemsSource = itemsSource;
				Verify.AreEqual(itemsSource, itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ItemsRepeater");
				ScrollView scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);
				ItemsRepeater itemsRepeater = scrollView.Content as ItemsRepeater;
				Verify.IsNotNull(itemsRepeater);

				int childrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);
				Log.Comment($"Extracting first ItemContainer, children count: {childrenCount}");
				itemContainer = itemsRepeater.FindVisualChildByType<ItemContainer>();
				Verify.IsNotNull(itemContainer);
				Verify.IsFalse(itemContainer.IsSelected);
#if MUX_PRERELEASE
				Verify.AreEqual(ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect, itemContainer.CanUserSelect);
				Verify.AreEqual(ItemContainerMultiSelectMode.Auto | ItemContainerMultiSelectMode.Single, itemContainer.MultiSelectMode);
#endif

				Log.Comment("Selecting first ItemContainer");
				itemContainer.IsSelected = true;
				Verify.IsTrue(itemContainer.IsSelected);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Selecting Extended SelectionMode");
				itemsView.SelectionMode = ItemsViewSelectionMode.Extended;
				Verify.AreEqual(ItemsViewSelectionMode.Extended, itemsView.SelectionMode);
				Verify.IsTrue(itemContainer.IsSelected);
#if MUX_PRERELEASE
				Verify.AreEqual(ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect, itemContainer.CanUserSelect);
				Verify.AreEqual(ItemContainerMultiSelectMode.Auto | ItemContainerMultiSelectMode.Extended, itemContainer.MultiSelectMode);
#endif
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Selecting Extended SelectionMode");
				itemsView.SelectionMode = ItemsViewSelectionMode.Multiple;
				Verify.AreEqual(ItemsViewSelectionMode.Multiple, itemsView.SelectionMode);
				Verify.IsTrue(itemContainer.IsSelected);
#if MUX_PRERELEASE
				Verify.AreEqual(ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect, itemContainer.CanUserSelect);
				Verify.AreEqual(ItemContainerMultiSelectMode.Auto | ItemContainerMultiSelectMode.Multiple, itemContainer.MultiSelectMode);
#endif
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Selecting None SelectionMode");
				itemsView.SelectionMode = ItemsViewSelectionMode.None;
				Verify.AreEqual(ItemsViewSelectionMode.None, itemsView.SelectionMode);
				Verify.IsFalse(itemContainer.IsSelected);
#if MUX_PRERELEASE
				Verify.AreEqual(ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCannotSelect, itemContainer.CanUserSelect);
				Verify.AreEqual(ItemContainerMultiSelectMode.Auto | ItemContainerMultiSelectMode.Single, itemContainer.MultiSelectMode);
#endif
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Selecting Single SelectionMode");
				itemsView.SelectionMode = ItemsViewSelectionMode.Single;
				Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
				Verify.IsFalse(itemContainer.IsSelected);
#if MUX_PRERELEASE
				Verify.AreEqual(ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect, itemContainer.CanUserSelect);
				Verify.AreEqual(ItemContainerMultiSelectMode.Auto | ItemContainerMultiSelectMode.Single, itemContainer.MultiSelectMode);
#endif
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Select an item, call InvertSelection")]
	public async Task VerifyCanInvertSelection()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView"))
		{
			ItemsView itemsView = null;
			List<int> itemsSource = new List<int>(Enumerable.Range(0, 5000));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView() { ItemsSource = itemsSource };
				SetupDefaultUI(itemsView, itemsViewLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Logging ItemsView property values after Loaded event");
				LogItemsViewProperties(itemsView);

				itemsView.Select(0);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.IsSelected(0), "Selected item is 0");

				itemsView.InvertSelection();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var totalItemCount = itemsSource.Count;
				var numSelectedItems = itemsView.SelectedItems.Count;

				Verify.IsFalse(itemsView.IsSelected(0), "Item 0 is not selected");
				Verify.AreEqual(numSelectedItems, totalItemCount - 1, "Verify expected number of items are selected");
			});

			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Select an item, scroll to recycle selected item, scroll back to ensure selection persisted across recycling")]
	[Ignore("Uno-specific: ItemsView uses Layout.IndexBasedLayoutOrientation in TryGetItemIndex which is not yet implemented in Uno.")]
	public async Task VerifySelectionPersistsAfterRecycling()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ScrollView"))
		{
			ItemsView itemsView = null;
			ScrollView scrollView = null;
			List<int> itemsSource = new List<int>(Enumerable.Range(0, 5000));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewBringingIntoViewEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewScrollCompletedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView() { ItemsSource = itemsSource };
				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;

				scrollView.BringingIntoView += (sender, args) =>
				{
					Log.Comment("ScrollView.BringingIntoView raised - CorrelationId=" + args.CorrelationId + ", TargetVerticalOffset=" + args.TargetVerticalOffset);

					scrollViewBringingIntoViewEvent.Set();
				};

				scrollView.ScrollCompleted += (sender, args) =>
				{
					Log.Comment("ScrollView.ScrollCompleted raised - CorrelationId=" + args.CorrelationId + ", VerticalOffset=" + scrollView.VerticalOffset);

					scrollViewScrollCompletedEvent.Set();
				};
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Logging ItemsView property values after Loaded event");
				LogItemsViewProperties(itemsView);

				itemsView.Select(0);
			});

			await TestServices.WindowHelper.WaitForIdle();

			// Scroll to the end
			await BringItemIntoView(itemsSource.Count - 1, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.IsSelected(0), "Selected item is still 0");
			});

			// Scroll back to 0
			await BringItemIntoView(0, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.IsSelected(0), "Selected item is still 0");
			});

			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Loads and populates an ItemsView, sets ItemContainer.IsSelected even though ItemContainer.UserCanSelect==ItemContainerUserSelectMode.UserCannotSelect.")]
	public async Task CanSetItemContainerIsSelectedProperty()
	{
		List<string> types = new List<string>(3);
		types.Add("ItemsView");
		types.Add("ItemsRepeater");
		types.Add("ItemContainer");

		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper(types, isLoggingInfoLevel: true, isLoggingVerboseLevel: true))
		{
			ItemsView itemsView = null;
			ItemContainer itemContainer = null;
			Random rnd = new Random();
			List<string> itemsSource = new List<string>(Enumerable.Range(0, 10).Select(k => k + " - " + rnd.Next(100)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.Layout is StackLayout);
				Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemTemplate with ItemContainer");
				DataTemplate itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Blue'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemsSource");
				itemsView.ItemsSource = itemsSource;
				Verify.AreEqual(itemsSource, itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ItemsRepeater");
				ScrollView scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);
				ItemsRepeater itemsRepeater = scrollView.Content as ItemsRepeater;
				Verify.IsNotNull(itemsRepeater);

				int childrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);
				Log.Comment($"Extracting first ItemContainer, children count: {childrenCount}");
				itemContainer = itemsRepeater.FindVisualChildByType<ItemContainer>();
				Verify.IsNotNull(itemContainer);
				Verify.IsFalse(itemContainer.IsSelected);
#if MUX_PRERELEASE
				Verify.AreEqual(ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect, itemContainer.CanUserSelect);
				Verify.AreEqual(ItemContainerMultiSelectMode.Auto | ItemContainerMultiSelectMode.Single, itemContainer.MultiSelectMode);

				Log.Comment("Preventing interactive selection");
				itemContainer.CanUserSelect = ItemContainerUserSelectMode.UserCannotSelect;
				Verify.AreEqual(ItemContainerUserSelectMode.UserCannotSelect, itemContainer.CanUserSelect);
#endif

				Log.Comment("Selecting first ItemContainer programmatically");
				itemContainer.IsSelected = true;
				Verify.IsTrue(itemContainer.IsSelected);
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Loads an ItemsView, changes Layout property to various types.")]
	public async Task CanChangeLayoutProperty()
	{
		List<string> types = new List<string>
		{
			"ItemsView",
			"ScrollView",
			"ItemsRepeater",
			"LinedFlowLayout"
		};

		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper(types, isLoggingInfoLevel: true, isLoggingVerboseLevel: true))
		{
			ItemsView itemsView = null;
			Random rnd = new Random();
			List<string> itemsSource = new List<string>(Enumerable.Range(0, 50).Select(k => k + " - " + rnd.Next(100)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView() { ItemsSource = itemsSource };

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.Layout is StackLayout);

				itemsView.Layout = new UniformGridLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.Layout is UniformGridLayout);

				itemsView.Layout = new LinedFlowLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(itemsView.Layout is LinedFlowLayout);

				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ability to set the ItemTemplate property after the ItemsSource.")]
	public async Task CanSetItemTemplateAfterItemsSourceProperty()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ItemsRepeater"))
		{
			DataTemplate itemTemplate = null;
			ItemsView itemsView = null;
			List<string> itemsSource = null;
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsSource = new List<string>(Enumerable.Range(0, 10).Select(k => k.ToString()));
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemsSource");
				itemsView.ItemsSource = itemsSource;
				Verify.AreEqual(itemsSource, itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemTemplate with ItemContainer");
				itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Blue'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ScrollView");
				ScrollView scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);

				Log.Comment("Extracting ItemsRepeater");
				ItemsRepeater itemsRepeater = scrollView.Content as ItemsRepeater;
				Verify.IsNotNull(itemsRepeater);

				Log.Comment("Expecting ItemTemplate of ItemsView and ItemRepeater to be identical");
				Verify.AreEqual(itemsRepeater.ItemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting ItemTemplate");
				itemsView.ItemTemplate = null;

				Log.Comment("Expecting non-null ItemTemplate");
				Verify.IsNotNull(itemsView.ItemTemplate);

				Log.Comment("Expecting modified ItemTemplate");
				Verify.AreNotEqual(itemsView.ItemTemplate, itemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Invokes the ItemsView.StartBringItemIntoView methods.")]
	[Ignore("Uno-specific: ItemsView uses Layout.IndexBasedLayoutOrientation in TryGetItemIndex which is not yet implemented in Uno.")]
	public async Task CanBringItemIntoView()
	{
		await CanBringItemIntoView(useLinedFlowLayout: false, useUniformGridLayout: false);
		await CanBringItemIntoView(useLinedFlowLayout: true, useUniformGridLayout: false);
		await CanBringItemIntoView(useLinedFlowLayout: false, useUniformGridLayout: true);
	}

	[TestMethod]
	[TestProperty("Description", "Verify binding to the ItemsView's SelectedItem using XAML markup.")]
	public async Task CanBindSelectedItem()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ItemsRepeater"))
		{
			Panel rootPanel = null;
			TextBlock textBlock = null;
			UnoAutoResetEvent rootPanelLoadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				string panelXaml =
					@"<StackPanel 
							  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
							  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
							  <ItemsView x:Name='itemsView' ItemsSource='{Binding Digits, Mode=OneWay}'>
								  <ItemsView.ItemTemplate>
									  <DataTemplate>
										  <ItemContainer>
											  <TextBlock Text='{Binding}'/>
										  </ItemContainer>
									  </DataTemplate>
								  </ItemsView.ItemTemplate>
							  </ItemsView>
							  <TextBlock Text='{Binding ElementName=itemsView, Path=SelectedItem, Mode=OneWay}'/>
						  </StackPanel>";

				Log.Comment("Loading XAML markup.");
				rootPanel = XamlReader.Load(panelXaml) as Panel;
				Verify.IsNotNull(rootPanel);

				rootPanel.Loaded += (object sender, RoutedEventArgs e) =>
				{
					Log.Comment("Panel.Loaded event handler");
					rootPanelLoadedEvent.Set();
				};

				rootPanel.DataContext = this;

				Log.Comment("Setting window Content.");
				Content = rootPanel;
			});

			await WaitForEvent("Waiting for Panel.Loaded event", rootPanelLoadedEvent);

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Accessing ItemsView.");
				ItemsView itemsView = rootPanel.Children[0] as ItemsView;
				Verify.IsNotNull(itemsView);

				Log.Comment("Accessing databound TextBlock.");
				textBlock = rootPanel.Children[1] as TextBlock;
				Verify.IsNotNull(textBlock);

				Log.Comment($"TextBlock.Text={textBlock.Text}");
				Verify.AreEqual(string.Empty, textBlock.Text);

				Log.Comment("Selecting item @ index 5.");
				itemsView.Select(5);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment($"TextBlock.Text={textBlock.Text}");
				Verify.AreEqual("5", textBlock.Text);
			});
		}
	}

	[TestMethod]
	[Ignore("Uno-specific: Fails for unknown reason, possibly ItemsRepeater issue")]
	public async Task VerifyItemsViewUIABehavior()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ScrollView"))
		{
			ItemsView itemsView = null;
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemsSource");
				Random rnd = new Random();
				List<string> itemsSource = new List<string>(Enumerable.Range(0, 25).Select(k => k + " - " + rnd.Next(100)));
				Verify.IsNull(itemsView.ItemsSource);
				itemsView.ItemsSource = itemsSource;

				Verify.IsNotNull(itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				for (int i = 0; i < 25; i++)
				{
					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);
					Verify.IsNotNull(itemsRepeater);

					var element = itemsRepeater.TryGetElement(i);
					Verify.IsNotNull(element);

					Verify.AreEqual(i + 1, element.GetValue(AutomationProperties.PositionInSetProperty));
					Verify.AreEqual(25, element.GetValue(AutomationProperties.SizeOfSetProperty));
				}
			});
		}
	}

	[TestMethod]
	[Ignore("Uno-specific: ItemsView uses Layout.IndexBasedLayoutOrientation in TryGetItemIndex which is not yet implemented in Uno.")]
	public async Task VerifyItemsViewUIASelectionProviderBehavior()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ScrollView"))
		{
			ItemsView itemsView = null;
			ItemContainer itemContainer = null;
			ItemsRepeater itemsRepeater = null;
			ScrollView scrollView = null;
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewScrollCompletedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewBringingIntoViewEvent = new UnoAutoResetEvent(false);
			ISelectionProvider selectionProvider = null;
			ISelectionItemProvider selectionItemProvider = null;

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemTemplate with ItemContainer");
				DataTemplate itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Blue'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ScrollView");
				scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);

				scrollView.BringingIntoView += (sender, args) =>
				{
					Log.Comment("ScrollView.BringingIntoView raised - CorrelationId=" + args.CorrelationId + ", TargetVerticalOffset=" + args.TargetVerticalOffset);

					scrollViewBringingIntoViewEvent.Set();
				};

				scrollView.ScrollCompleted += (sender, args) =>
				{
					Log.Comment("ScrollView.ScrollCompleted raised - CorrelationId=" + args.CorrelationId + ", VerticalOffset=" + scrollView.VerticalOffset);

					scrollViewScrollCompletedEvent.Set();
				};

				Log.Comment("Setting ItemsSource");
				Random rnd = new Random();
				List<string> itemsSource = new List<string>(Enumerable.Range(0, 50).Select(k => k + " - " + rnd.Next(100)));
				Verify.IsNull(itemsView.ItemsSource);
				itemsView.ItemsSource = itemsSource;

				Verify.IsNotNull(itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var itemsViewPeer = FrameworkElementAutomationPeer.CreatePeerForElement(itemsView);
				Verify.IsNotNull(itemsViewPeer);
				var itemsViewSelectionPattern = itemsViewPeer.GetPattern(PatternInterface.Selection);
				Verify.IsNotNull(itemsViewSelectionPattern);
				selectionProvider = itemsViewSelectionPattern as ISelectionProvider;

				// ItemsView CanSelectMultiple is false by default.
				Verify.IsFalse(selectionProvider.CanSelectMultiple, "CanSelectMultiple returns false when ItemsViewSelectionMode is single");

				itemsView.SelectionMode = ItemsViewSelectionMode.Multiple;
				Verify.IsTrue(selectionProvider.CanSelectMultiple, "CanSelectMultiple returns true when ItemsViewSelectionMode is multiple");

				itemsView.SelectionMode = ItemsViewSelectionMode.Extended;
				Verify.IsTrue(selectionProvider.CanSelectMultiple, "CanSelectMultiple returns true when ItemsViewSelectionMode is extended");

				Verify.IsNull(selectionProvider.GetSelection());
				Log.Comment("GetSelection returns null as no items are selected.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ItemsRepeater");
				ScrollView scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);
				itemsRepeater = scrollView.Content as ItemsRepeater;
				Verify.IsNotNull(itemsRepeater);

				int childrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);
				Log.Comment($"Extracting first ItemContainer, children count: {childrenCount}");
				itemContainer = itemsRepeater.TryGetElement(0) as ItemContainer;
				Verify.IsNotNull(itemContainer);
				Verify.IsFalse(itemContainer.IsSelected);

				Log.Comment("Selecting first ItemContainer");
				itemContainer.IsSelected = true;
				Verify.IsTrue(itemContainer.IsSelected);

				Verify.IsNotNull(selectionProvider.GetSelection());
				Log.Comment("GetSelection returns peer of selected ItemContainer.");

				Log.Comment("Extracting second ItemContainer.");
				itemContainer = itemsRepeater.TryGetElement(1) as ItemContainer;
				Verify.IsNotNull(itemContainer);
				Verify.IsFalse(itemContainer.IsSelected);

				Log.Comment("Selecting second ItemContainer");
				itemContainer.IsSelected = true;
				Verify.IsTrue(itemContainer.IsSelected);

				Verify.IsNotNull(selectionProvider.GetSelection());
				Verify.AreEqual(selectionProvider.GetSelection().Count(), 2);
				Log.Comment("GetSelection returns list of 2 ItemContainer peers.");

				Log.Comment("Extracting last ItemContainer.");
				itemContainer = itemsRepeater.TryGetElement(49) as ItemContainer;
				Verify.IsNull(itemContainer);
				Log.Comment("ItemContainer is null as it is out of view and not realized.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			await BringItemIntoView(49, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);
			Log.Comment("Scroll to last item.");

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting last ItemContainer.");
				itemContainer = itemsRepeater.TryGetElement(49) as ItemContainer;
				Verify.IsNotNull(itemContainer);
				Log.Comment("ItemContainer is null as it is out of view and not realized.");

				Log.Comment("Selecting last ItemContainer");
				itemContainer.IsSelected = true;
				Verify.IsTrue(itemContainer.IsSelected);

				Verify.IsNotNull(selectionProvider.GetSelection());
				Verify.AreEqual(selectionProvider.GetSelection().Count(), 1);
				Log.Comment("GetSelection returns 1 ItemContainer peer that is in view and realized.");

				var itemContainerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(itemContainer);
				Verify.IsNotNull(itemContainerPeer);
				var itemContainerSelectionPattern = itemContainerPeer.GetPattern(PatternInterface.SelectionItem);
				Verify.IsNotNull(itemContainerSelectionPattern);
				selectionItemProvider = itemContainerSelectionPattern as ISelectionItemProvider;

				Verify.IsNotNull(selectionItemProvider.SelectionContainer);
				Log.Comment("ItemContainer SelectionContainer returns parent ItemsView.");
			});

			await BringItemIntoView(0, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);
			Log.Comment("Scroll back to first item.");

			RunOnUIThread.Execute(() =>
			{
				Verify.IsNotNull(selectionProvider.GetSelection());
				Verify.AreEqual(selectionProvider.GetSelection().Count(), 2);
				Log.Comment("GetSelection returns list of 2 ItemContainer peers that are in view and realized.");

				var itemContainerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(itemContainer);
				Verify.IsNotNull(itemContainerPeer);
				var itemContainerSelectionPattern = itemContainerPeer.GetPattern(PatternInterface.SelectionItem);
				Verify.IsNotNull(itemContainerSelectionPattern);
				selectionItemProvider = itemContainerSelectionPattern as ISelectionItemProvider;

				Verify.IsNotNull(selectionItemProvider.SelectionContainer);
				Log.Comment("ItemContainer SelectionContainer returns parent ItemsView.");
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and provides the minimum information requested.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task HandleItemsInfoRequested()
	{
		await HandleItemsInfoRequested(useSizeArrays: false, useExtraInfo: false, useTemporaryAspectRatio: false);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and provides the minimum information requested, with min/max size arrays.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task HandleItemsInfoRequestedWithSizeArrays()
	{
		await HandleItemsInfoRequested(useSizeArrays: true, useExtraInfo: false, useTemporaryAspectRatio: false);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and provides information beyond the minimum requested.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task HandleItemsInfoRequestedWithExtraInfo()
	{
		await HandleItemsInfoRequested(useSizeArrays: false, useExtraInfo: true, useTemporaryAspectRatio: false);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and provides information beyond the minimum requested, with min/max size arrays.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task HandleItemsInfoRequestedWithSizeArraysAndExtraInfo()
	{
		await HandleItemsInfoRequested(useSizeArrays: true, useExtraInfo: true, useTemporaryAspectRatio: false);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and provides partial temporary aspect ratios.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task HandleItemsInfoRequestedWithTemporaryInfo()
	{
		await HandleItemsInfoRequested(useSizeArrays: false, useExtraInfo: false, useTemporaryAspectRatio: true);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and provides information beyond the minimum requested, with partial temporary aspect ratios.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task HandleItemsInfoRequestedWithTemporaryAndExtraInfo()
	{
		await HandleItemsInfoRequested(useSizeArrays: false, useExtraInfo: true, useTemporaryAspectRatio: true);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and triggers various exceptions exercising LinedFlowLayoutItemsInfoRequestedEventArgs APIs.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task TriggerLinedFlowLayoutItemsInfoRequestedEventArgsExceptions()
	{
		await TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ItemsRangeStartIndexNegative);
		await TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ItemsRangeStartIndexIncreased);
		await TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ItemsRangeStartIndexTooSmall);
		await TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ArrayLengthSmallerThanItemsRangeRequestedLength);
		await TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ArrayLengthTooSmallForDecreasedItemsRangeStartIndex);
		await TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ArrayLengthInconsistent);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and alternatively triggers the fast and regular paths. LinedFlowLayoutItemsInfoRequestedEventArgs.SetMinWidths & LinedFlowLayoutItemsInfoRequestedEventArgs.SetMaxWidths are used.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task AlternateLayoutPathsWithSizeArrays()
	{
		await AlternateLayoutPaths(useSizeArrays: true);
	}

	[TestMethod]
	[TestProperty("Description", "Handles the LinedFlowLayout.ItemsInfoRequested event and alternatively triggers the fast and regular paths. LinedFlowLayoutItemsInfoRequestedEventArgs.MinWidth & LinedFlowLayoutItemsInfoRequestedEventArgs.MaxWidth are used.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task AlternateLayoutPathsWithUniformSizes()
	{
		await AlternateLayoutPaths(useSizeArrays: false);
	}

	[TestMethod]
	[TestProperty("Description", "Alternatively hooks and unhooks the LinedFlowLayout.ItemsInfoRequested event to use the fast and regular paths.")]
	public async Task AlternateLayoutPathsByUnhookingItemsInfoRequested()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "LinedFlowLayout"))
		{
			ItemsView itemsView = null;
			LinedFlowLayout linedFlowLayout = null;
			Random rnd = new Random();
			ObservableCollection<string> itemsSource = new ObservableCollection<string>(Enumerable.Range(0, 75).Select(k => k + "-" + rnd.Next(20)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				linedFlowLayout = new LinedFlowLayout()
				{
					LineHeight = 50.0
				};

				itemsView = new ItemsView()
				{
					Layout = linedFlowLayout,
					ItemsSource = itemsSource
				};

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			await TestServices.WindowHelper.WaitForIdle();

			for (int resizeCount = 0; resizeCount < 6; resizeCount++)
			{
				RunOnUIThread.Execute(() =>
				{
					if (resizeCount % 2 == 0)
					{
						Log.Comment($"Hooking LinedFlowLayout.ItemsInfoRequested");
						linedFlowLayout.ItemsInfoRequested += LinedFlowLayout_ItemsInfoRequested;
					}
					else
					{
						Log.Comment($"Unhooking LinedFlowLayout.ItemsInfoRequested");
						linedFlowLayout.ItemsInfoRequested -= LinedFlowLayout_ItemsInfoRequested;
					}

					Log.Comment($"ItemsView resized to {itemsView.ActualWidth + 1}");
					itemsView.Width = itemsView.ActualWidth + 1;
				});

				await TestServices.WindowHelper.WaitForIdle();
			}

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
			Log.Comment("Done");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the stability of the LinedFlowLayout's average-items-per-line by progressively lowering it through new collection items.")]
	[Ignore("Uno-specific: The test uses LinedFlowLayout which is not yet implemented in Uno.")]
	public async Task VerifyLinedFlowLayoutAverageItemsPerLineStability()
	{
		ItemsView itemsView = null;
		LinedFlowLayout linedFlowLayout = null;
		ObservableCollection<string> itemsSource = new ObservableCollection<string>();
		UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);
		//double previousRawAverageItemsPerLine = 0.0;
		//double previousSnappedAverageItemsPerLine = 0.0;

		RunOnUIThread.Execute(() =>
		{
			linedFlowLayout = new LinedFlowLayout()
			{
				LineHeight = 50.0
			};

			itemsView = new ItemsView()
			{
				Layout = linedFlowLayout,
				ItemsSource = itemsSource
			};

			SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);

			itemsView.Height = 700.0;
		});

		await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

		await TestServices.WindowHelper.WaitForIdle();

		for (int newItemCount = 0; newItemCount < 10; newItemCount++)
		{
			for (int duplicate = 0; duplicate < 16; duplicate++)
			{
				RunOnUIThread.Execute(() =>
				{
					string newItem = Math.Pow(2, newItemCount).ToString();

					Log.Comment($"Adding item: {newItem}");

					itemsSource.Add(newItem);
				});

				await TestServices.WindowHelper.WaitForIdle();

				RunOnUIThread.Execute(() =>
				{
					//double averageItemAspectRatio = LayoutsTestHooks.GetLinedFlowLayoutAverageItemAspectRatio(linedFlowLayout);
					//double snappedAverageItemsPerLine = LayoutsTestHooks.GetLinedFlowLayoutSnappedAverageItemsPerLine(linedFlowLayout);
					//double rawAverageItemsPerLine1 = LayoutsTestHooks.GetLinedFlowLayoutRawAverageItemsPerLine(linedFlowLayout);
					//double rawAverageItemsPerLine2 = c_defaultUIItemsViewWidth / (averageItemAspectRatio * linedFlowLayout.ActualLineHeight);

					//Log.Comment($"LinedFlowLayout averageItemAspectRatio: {averageItemAspectRatio.ToString()}, snappedAverageItemsPerLine: {snappedAverageItemsPerLine.ToString()}");
					//Log.Comment($"LinedFlowLayout rawAverageItemsPerLine1: {rawAverageItemsPerLine1.ToString()}, rawAverageItemsPerLine2: {rawAverageItemsPerLine2.ToString()}");

					//double previousRawAverageItemsPerLineSnappedToPower = SnapToPower(previousRawAverageItemsPerLine);
					//double rawAverageItemsPerLineSnappedToPower = SnapToPower(rawAverageItemsPerLine2);

					//if (previousRawAverageItemsPerLineSnappedToPower != rawAverageItemsPerLineSnappedToPower &&
					//	previousSnappedAverageItemsPerLine != snappedAverageItemsPerLine)
					//{
					//	Log.Comment($"previousRawAverageItemsPerLine: {previousRawAverageItemsPerLine}");
					//	Log.Comment($"previousSnappedAverageItemsPerLine: {previousSnappedAverageItemsPerLine}");

					//	Log.Comment($"previousRawAverageItemsPerLineSnappedToPower: {previousRawAverageItemsPerLineSnappedToPower}");
					//	Log.Comment($"rawAverageItemsPerLineSnappedToPower: {rawAverageItemsPerLineSnappedToPower}");

					//	// When the previous and new snapped average-items-per-line differ, it must be a raw variation of more than 0.1.
					//	Verify.IsGreaterThan(Math.Abs(rawAverageItemsPerLine2 - previousRawAverageItemsPerLine), 0.1);
					//}

					//previousRawAverageItemsPerLine = rawAverageItemsPerLine2;
					//previousSnappedAverageItemsPerLine = snappedAverageItemsPerLine;
				});
			}
		}

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Resetting window content and ItemsView");
			Content = null;
			itemsView = null;
		});

		await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
		Log.Comment("Done");
	}

	// Snaps the provided value to a power of valuePower.
	// Example: value=3.75 snaps to 1.1^14 = 3.7975, valuePower being 1.1.
	private double SnapToPower(double value)
	{
		double valuePower = 1.1;
		double dividerLog = Math.Log(valuePower);
		double valueLog = Math.Log(value);
		double valueExp = Math.Ceiling(valueLog / dividerLog);
		double valueRndCeil = Math.Pow(valuePower, valueExp);
		double valueRndFloor = valueRndCeil / valuePower;

		// Return valuePower^valueExp or valuePower^(valueExp - 1), whichever is closest to value.
		if (Math.Abs(valueRndCeil - value) <= Math.Abs(valueRndFloor - value))
		{
			return valueRndCeil;
		}
		else
		{
			return valueRndFloor;
		}
	}

	private void LinedFlowLayout_ItemsInfoRequested(LinedFlowLayout sender, LinedFlowLayoutItemsInfoRequestedEventArgs args)
	{
		Log.Comment("LinedFlowLayout.ItemsInfoRequested raised - ItemsRangeStartIndex=" + args.ItemsRangeStartIndex + ", ItemsRangeRequestedLength=" + args.ItemsRangeRequestedLength);

		double[] desiredAspectRatios = new double[100];

		for (int desiredAspectRatioIndex = 0; desiredAspectRatioIndex < 100; desiredAspectRatioIndex++)
		{
			desiredAspectRatios[desiredAspectRatioIndex] = 1.0;
		}

		args.ItemsRangeStartIndex = 0;
		args.SetDesiredAspectRatios(desiredAspectRatios);
	}

	private async Task AlternateLayoutPaths(bool useSizeArrays)
	{
		List<string> types = new List<string>
		{
			"ItemsView",
			"ScrollView",
			"ItemsRepeater",
			"LinedFlowLayout"
		};

		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper(types, isLoggingInfoLevel: true, isLoggingVerboseLevel: true))
		{
			ItemsView itemsView = null;
			ScrollView scrollView = null;
			LinedFlowLayout linedFlowLayout = null;
			int itemsInfoRequestedCount = 0;
			bool provideItemsInfo = false;
			bool provideExtraItemsInfo = false;
			Random rnd = new Random();
			ObservableCollection<string> itemsSource = new ObservableCollection<string>(Enumerable.Range(0, 1000).Select(k => k + "-" + rnd.Next(20)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewScrollCompletedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent linedFlowLayoutItemsInfoRequestedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				linedFlowLayout = new LinedFlowLayout()
				{
					LineHeight = 50.0
				};

				linedFlowLayout.ItemsInfoRequested += (sender, args) =>
				{
					Log.Comment("LinedFlowLayout.ItemsInfoRequested raised - ItemsRangeStartIndex=" + args.ItemsRangeStartIndex + ", ItemsRangeRequestedLength=" + args.ItemsRangeRequestedLength);

					Verify.IsGreaterThanOrEqual(args.ItemsRangeStartIndex, 0);
					Verify.IsGreaterThan(args.ItemsRangeRequestedLength, 0);

					if (!provideItemsInfo)
					{
						// For the next ItemsInfoRequested handling: provide requested items info only.
						provideItemsInfo = true;

						// Trigger the regular path without ItemsInfo at all.
						return;
					}

					int arrayStart = provideExtraItemsInfo ? 0 : args.ItemsRangeStartIndex;
					int arrayLength = provideExtraItemsInfo ? itemsSource.Count : args.ItemsRangeRequestedLength;

					args.ItemsRangeStartIndex = arrayStart;

					double[] desiredAspectRatios = new double[arrayLength];

					for (int index = 0; index < arrayLength; index++)
					{
						desiredAspectRatios[index] = 0.8 + (index + arrayStart) / 600.0;
					}

					args.SetDesiredAspectRatios(desiredAspectRatios);

					if (useSizeArrays)
					{
						args.MinWidth = -1.0;
						args.MaxWidth = -1.0;

						double[] minWidths = new double[arrayLength];

						for (int index = 0; index < arrayLength; index++)
						{
							minWidths[index] = (index + arrayStart) / 25.0;
						}

						args.SetMinWidths(minWidths);

						double[] maxWidths = new double[arrayLength];

						for (int index = 0; index < arrayLength; index++)
						{
							maxWidths[index] = (index + arrayStart) / 5.0 + 125.0;
						}

						args.SetMaxWidths(maxWidths);
					}
					else
					{
						args.MinWidth = 40.0;
						args.MaxWidth = 140.0;
					}

					itemsInfoRequestedCount++;

					if (provideExtraItemsInfo)
					{
						// For the next ItemsInfoRequested handling: do not provide any items info at all.

						provideItemsInfo = false;
						provideExtraItemsInfo = false;
					}
					else
					{
						// For the next ItemsInfoRequested handling: provide items info for the entire collection.

						provideExtraItemsInfo = true;
					}

					linedFlowLayoutItemsInfoRequestedEvent.Set();
				};

				itemsView = new ItemsView()
				{
					Layout = linedFlowLayout,
					ItemsSource = itemsSource
				};

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);
			await WaitForEvent("Waiting for ItemsInfoRequested event", linedFlowLayoutItemsInfoRequestedEvent);

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ScrollView");
				scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);

				scrollView.ScrollCompleted += (sender, args) =>
				{
					Log.Comment("ScrollView.ScrollCompleted raised - CorrelationId=" + args.CorrelationId + ", VerticalOffset=" + scrollView.VerticalOffset);

					scrollViewScrollCompletedEvent.Set();
				};
			});

			for (int scrollCount = 0; scrollCount < 5; scrollCount++)
			{
				RunOnUIThread.Execute(() =>
				{
					scrollViewScrollCompletedEvent.Reset();

					Log.Comment("Invoking ScrollView.ScrollTo");
					scrollView.ScrollTo(0.0, scrollView.ScrollableHeight / 6 * (scrollCount + 1), new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
				});

				await WaitForEvent("Waiting for ScrollCompleted event", scrollViewScrollCompletedEvent);

				await TestServices.WindowHelper.WaitForIdle();

				RunOnUIThread.Execute(() =>
				{
					linedFlowLayoutItemsInfoRequestedEvent.Reset();

					Log.Comment("Adding datasource item");
					itemsSource.Add((1000 + scrollCount).ToString() + "-" + rnd.Next(20));
				});

				await WaitForEvent("Waiting for ItemsInfoRequested event", linedFlowLayoutItemsInfoRequestedEvent);

				await TestServices.WindowHelper.WaitForIdle();

				Log.Comment($"ItemsInfoRequested event count={itemsInfoRequestedCount}");
			}

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
			Log.Comment("Done");
		}
	}

	private async Task HandleItemsInfoRequested(bool useSizeArrays, bool useExtraInfo, bool useTemporaryAspectRatio)
	{
		List<string> types = new List<string>
		{
			"ItemsView",
			"ScrollView",
			"ItemsRepeater",
			"LinedFlowLayout"
		};

		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper(types, isLoggingInfoLevel: true, isLoggingVerboseLevel: true))
		{
			ItemsView itemsView = null;
			ScrollView scrollView = null;
			LinedFlowLayout linedFlowLayout = null;
			int itemsInfoRequestedCount = 0;
			bool provideNilAspectRatios = useTemporaryAspectRatio;
			Random rnd = new Random();
			List<string> itemsSource = new List<string>(Enumerable.Range(0, 200).Select(k => k + " - " + rnd.Next(100)));
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewScrollCompletedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent linedFlowLayoutItemsInfoRequestedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				linedFlowLayout = new LinedFlowLayout()
				{
					LineHeight = 50.0
				};

				linedFlowLayout.ItemsInfoRequested += (sender, args) =>
				{
					Log.Comment("LinedFlowLayout.ItemsInfoRequested raised - ItemsRangeStartIndex=" + args.ItemsRangeStartIndex + ", ItemsRangeRequestedLength=" + args.ItemsRangeRequestedLength);

					Verify.IsGreaterThanOrEqual(args.ItemsRangeStartIndex, 0);
					Verify.IsGreaterThan(args.ItemsRangeRequestedLength, 0);

					int arrayStart = useExtraInfo ? 0 : args.ItemsRangeStartIndex;
					int arrayLength = useExtraInfo ? itemsSource.Count : args.ItemsRangeRequestedLength;

					args.ItemsRangeStartIndex = arrayStart;

					double[] desiredAspectRatios = new double[arrayLength];

					for (int index = 0; index < arrayLength; index++)
					{
						if (provideNilAspectRatios && index % 10 == 0)
						{
							// An aspect ratio <= 0 indicates that the actual number is still unknown.
							desiredAspectRatios[index] = 0.0;
						}
						else
						{
							desiredAspectRatios[index] = 5.0 / (index + arrayStart + 1.0);
						}
					}

					args.SetDesiredAspectRatios(desiredAspectRatios);

					if (useSizeArrays)
					{
						double[] minWidths = new double[arrayLength];

						for (int index = 0; index < arrayLength; index++)
						{
							minWidths[index] = index + arrayStart;
						}

						args.SetMinWidths(minWidths);

						double[] maxWidths = new double[arrayLength];

						for (int index = 0; index < arrayLength; index++)
						{
							maxWidths[index] = index + arrayStart + 100.0;
						}

						args.SetMaxWidths(maxWidths);
					}
					else
					{
						args.MinWidth = 40.0;
						args.MaxWidth = 140.0;
					}

					itemsInfoRequestedCount++;

					linedFlowLayoutItemsInfoRequestedEvent.Set();
				};

				itemsView = new ItemsView()
				{
					Layout = linedFlowLayout,
					ItemsSource = itemsSource
				};

				SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);
			await WaitForEvent("Waiting for ItemsInfoRequested event", linedFlowLayoutItemsInfoRequestedEvent);

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ScrollView");
				scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;
				Verify.IsNotNull(scrollView);

				scrollView.ScrollCompleted += (sender, args) =>
				{
					Log.Comment("ScrollView.ScrollCompleted raised - CorrelationId=" + args.CorrelationId + ", VerticalOffset=" + scrollView.VerticalOffset);

					scrollViewScrollCompletedEvent.Set();
				};

				linedFlowLayoutItemsInfoRequestedEvent.Reset();

				Log.Comment("Invoking ScrollView.ScrollTo");
				scrollView.ScrollTo(0.0, 2000.0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
			});

			await WaitForEvent("Waiting for ScrollCompleted event", scrollViewScrollCompletedEvent);

			await TestServices.WindowHelper.WaitForIdle();

			Log.Comment($"ItemsInfoRequested event count={itemsInfoRequestedCount}");

			if (useExtraInfo)
			{
				Verify.AreEqual(itemsInfoRequestedCount, 1);
			}
			else
			{
				await WaitForEvent("Waiting for ItemsInfoRequested event", linedFlowLayoutItemsInfoRequestedEvent);

				Verify.IsGreaterThan(itemsInfoRequestedCount, 1);
			}

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Extracting ItemsRepeater");
				ItemsRepeater itemsRepeater = scrollView.Content as ItemsRepeater;
				Verify.IsNotNull(itemsRepeater);

				int itemsRepeaterChildrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);
				Log.Comment($"ItemsRepeater children count: {itemsRepeaterChildrenCount}");

				if (useTemporaryAspectRatio)
				{
					provideNilAspectRatios = false;
					linedFlowLayoutItemsInfoRequestedEvent.Reset();

					Log.Comment("Invoking LinedFlowLayout.InvalidateItemsInfo to trigger a new ItemsInfoRequested event");
					linedFlowLayout.InvalidateItemsInfo();
				}
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(async () =>
			{
				if (useTemporaryAspectRatio)
				{
					await WaitForEvent("Waiting for ItemsInfoRequested event", linedFlowLayoutItemsInfoRequestedEvent);

					Log.Comment($"ItemsInfoRequested event count={itemsInfoRequestedCount}");
					if (useExtraInfo)
					{
						Verify.AreEqual(itemsInfoRequestedCount, 2);
					}
					else
					{
						Verify.IsGreaterThan(itemsInfoRequestedCount, 2);
					}
				}

				Log.Comment("Resetting window content and ItemsView");
				Content = null;
				itemsView = null;
			});

			await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
			Log.Comment("Done");
		}
	}

	private async Task TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException(
		LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger trigger)
	{
		Log.Comment($"TriggerLinedFlowLayoutItemsInfoRequestedEventArgsException - trigger={trigger}");

		ItemsView itemsView = null;
		LinedFlowLayout linedFlowLayout = null;
		List<string> itemsSource = new List<string>(Enumerable.Range(0, 300).Select(k => k + " - " + (new Random()).Next(100)));
		UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent itemsViewUnloadedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollViewBringingIntoViewEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent scrollViewScrollCompletedEvent = new UnoAutoResetEvent(false);
		UnoAutoResetEvent linedFlowLayoutItemsInfoRequestedEvent = new UnoAutoResetEvent(false);
		bool linedFlowLayoutItemsInfoRequestedEventArgsExceptionThrown = false;

		RunOnUIThread.Execute(() =>
		{
			linedFlowLayout = new LinedFlowLayout()
			{
				LineHeight = 50.0
			};

			itemsView = new ItemsView()
			{
				Layout = linedFlowLayout,
				ItemsSource = itemsSource
			};

			SetupDefaultUI(itemsView, itemsViewLoadedEvent, itemsViewUnloadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			itemsView.ScrollView.BringingIntoView += (sender, args) =>
			{
				Log.Comment($"ScrollView.BringingIntoView raised - CorrelationId={args.CorrelationId}, TargetVerticalOffset={args.TargetVerticalOffset}");

				scrollViewBringingIntoViewEvent.Set();
			};

			itemsView.ScrollView.ScrollCompleted += (sender, args) =>
			{
				Log.Comment($"ScrollView.ScrollCompleted raised - CorrelationId={args.CorrelationId}, VerticalOffset={itemsView.ScrollView.VerticalOffset}");

				scrollViewScrollCompletedEvent.Set();
			};
		});

		await BringItemIntoView(150, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);

		RunOnUIThread.Execute(() =>
		{
			linedFlowLayout.ItemsInfoRequested += (sender, args) =>
			{
				Log.Comment($"LinedFlowLayout.ItemsInfoRequested raised - ItemsRangeStartIndex={args.ItemsRangeStartIndex}, ItemsRangeRequestedLength={args.ItemsRangeRequestedLength}");

				Verify.IsGreaterThanOrEqual(args.ItemsRangeStartIndex, 0);
				Verify.IsGreaterThan(args.ItemsRangeRequestedLength, 0);

				try
				{
					switch (trigger)
					{
						case LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ItemsRangeStartIndexNegative:
							{
								args.ItemsRangeStartIndex = -1;
								break;
							}
						case LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ItemsRangeStartIndexIncreased:
							{
								args.ItemsRangeStartIndex++;
								break;
							}
						case LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ItemsRangeStartIndexTooSmall:
							{
								args.SetMinWidths(new double[args.ItemsRangeRequestedLength]);
								args.ItemsRangeStartIndex--;
								break;
							}
						case LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ArrayLengthSmallerThanItemsRangeRequestedLength:
							{
								args.SetMinWidths(new double[args.ItemsRangeRequestedLength - 1]);
								break;
							}
						case LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ArrayLengthTooSmallForDecreasedItemsRangeStartIndex:
							{
								args.ItemsRangeStartIndex--;
								args.SetMinWidths(new double[args.ItemsRangeRequestedLength]);
								break;
							}
						case LinedFlowLayoutItemsInfoRequestedEventArgsExceptionTrigger.ArrayLengthInconsistent:
							{
								args.SetMinWidths(new double[args.ItemsRangeRequestedLength]);
								args.SetMaxWidths(new double[args.ItemsRangeRequestedLength + 1]);
								break;
							}
					}
				}
				catch (Exception exception)
				{
					Log.Comment($"Exception={exception.ToString()}");
					linedFlowLayoutItemsInfoRequestedEventArgsExceptionThrown = true;
				}

				linedFlowLayoutItemsInfoRequestedEvent.Set();
			};

			Log.Comment("Triggering the ItemsInfoRequested event");
			itemsView.ScrollView.ScrollBy(0.0, 1.0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
		});

		await WaitForEvent("Waiting for ItemsInfoRequested event", linedFlowLayoutItemsInfoRequestedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"linedFlowLayoutItemsInfoRequestedEventArgsExceptionThrown={linedFlowLayoutItemsInfoRequestedEventArgsExceptionThrown}");
			Verify.IsTrue(linedFlowLayoutItemsInfoRequestedEventArgsExceptionThrown);

			Log.Comment("Resetting window content and ItemsView");
			Content = null;
			itemsView = null;
		});

		await WaitForEvent("Waiting for Unloaded event", itemsViewUnloadedEvent);
		Log.Comment("Done");
	}

	private async Task CanBringItemIntoView(bool useLinedFlowLayout, bool useUniformGridLayout)
	{
		List<string> types = null;

		if (useLinedFlowLayout)
		{
			types = new List<string>(4);
			types.Add("LinedFlowLayout");
		}
		else
		{
			types = new List<string>(3);
		}

		types.Add("ItemsRepeater");
		types.Add("ScrollView");
		types.Add("ItemsView");

		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper(types, isLoggingInfoLevel: true, isLoggingVerboseLevel: true))
		{
			ItemsView itemsView = null;
			ScrollView scrollView = null;
			UnoAutoResetEvent itemsViewLoadedEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewBringingIntoViewEvent = new UnoAutoResetEvent(false);
			UnoAutoResetEvent scrollViewScrollCompletedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemsView = new ItemsView();

				SetupDefaultUI(itemsView, itemsViewLoadedEvent);
			});

			await WaitForEvent("Waiting for Loaded event", itemsViewLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				scrollView = itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;

				scrollView.BringingIntoView += (sender, args) =>
				{
					Log.Comment("ScrollView.BringingIntoView raised - CorrelationId=" + args.CorrelationId + ", TargetVerticalOffset=" + args.TargetVerticalOffset);

					scrollViewBringingIntoViewEvent.Set();
				};

				scrollView.ScrollCompleted += (sender, args) =>
				{
					Log.Comment("ScrollView.ScrollCompleted raised - CorrelationId=" + args.CorrelationId + ", VerticalOffset=" + scrollView.VerticalOffset);

					scrollViewScrollCompletedEvent.Set();
				};

				Log.Comment("Setting ItemsSource");
				Random rnd = new Random();
				List<string> itemsSource = new List<string>(Enumerable.Range(0, 500).Select(k => k + " - " + rnd.Next(500)));
				Verify.IsNull(itemsView.ItemsSource);
				itemsView.ItemsSource = itemsSource;
				Verify.IsNotNull(itemsView.ItemsSource);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting ItemTemplate");
				DataTemplate itemTemplate = XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
							<ItemContainer>
							  <TextBlock Text='{Binding}' Foreground='Red'/>
							</ItemContainer>
						  </DataTemplate>") as DataTemplate;
				Verify.IsNotNull(itemsView.ItemTemplate);
				itemsView.ItemTemplate = itemTemplate;
				Verify.AreEqual(itemTemplate, itemsView.ItemTemplate);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Setting Layout");

				Verify.IsNotNull(itemsView.Layout);
				Verify.IsTrue(itemsView.Layout is StackLayout);

				if (useLinedFlowLayout)
				{
					LinedFlowLayout linedFlowLayout = new LinedFlowLayout()
					{
						LineHeight = 96,
						LineSpacing = 4,
						MinItemSpacing = 4
					};

					itemsView.Layout = linedFlowLayout;
					Verify.IsTrue(itemsView.Layout is LinedFlowLayout);
				}
				else if (useUniformGridLayout)
				{
					UniformGridLayout uniformGridLayout = new UniformGridLayout()
					{
						MaximumRowsOrColumns = 4,
						MinColumnSpacing = 10,
						MinRowSpacing = 10,
						MinItemWidth = 100,
						MinItemHeight = 100
					};

					itemsView.Layout = uniformGridLayout;
					Verify.IsTrue(itemsView.Layout is UniformGridLayout);
				}
			});

			await TestServices.WindowHelper.WaitForIdle();

			await BringItemIntoView(350, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);
			await BringItemIntoView(340, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);
			await BringItemIntoView(150, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);
			await BringItemIntoView(160, itemsView, scrollViewBringingIntoViewEvent, scrollViewScrollCompletedEvent);
		}
	}

	private async Task BringItemIntoView(
		int index,
		ItemsView itemsView,
		UnoAutoResetEvent scrollViewBringingIntoViewEvent,
		UnoAutoResetEvent scrollViewScrollCompletedEvent)
	{
		RunOnUIThread.Execute(() =>
		{
			scrollViewBringingIntoViewEvent.Reset();
			scrollViewScrollCompletedEvent.Reset();

			Log.Comment("Invoking ItemsView.StartBringItemIntoView(250)");

			BringIntoViewOptions bringIntoViewOptions = new BringIntoViewOptions()
			{
				AnimationDesired = false
			};

			itemsView.StartBringItemIntoView(index, bringIntoViewOptions);
		});

		await WaitForEvent("Waiting for BringingIntoView event", scrollViewBringingIntoViewEvent);

		await WaitForEvent("Waiting for ScrollCompleted event", scrollViewScrollCompletedEvent);

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			int topLeftElementIndex;
			int bottomRightElementIndex;

			itemsView.TryGetItemIndex(horizontalViewportRatio: 0.0, verticalViewportRatio: 0.0, out topLeftElementIndex);
			itemsView.TryGetItemIndex(horizontalViewportRatio: 1.0, verticalViewportRatio: 1.0, out bottomRightElementIndex);

			Log.Comment("Top left element index=" + topLeftElementIndex);
			Log.Comment("Bottom right element index=" + bottomRightElementIndex);

			Verify.IsGreaterThanOrEqual(index, topLeftElementIndex);
			Verify.IsLessThanOrEqual(index, bottomRightElementIndex);
		});
	}

	private void SetupDefaultUI(
		ItemsView itemsView,
		UnoAutoResetEvent itemsViewLoadedEvent = null,
		UnoAutoResetEvent itemsViewUnloadedEvent = null,
		bool setAsContentRoot = true,
		bool useParentGrid = false)
	{
		Log.Comment("Setting up default UI with ItemsView");

		Verify.IsNotNull(itemsView);
		itemsView.Name = "itemsView";
		itemsView.Width = c_defaultUIItemsViewWidth;
		itemsView.Height = c_defaultUIItemsViewHeight;

		if (itemsViewLoadedEvent != null)
		{
			itemsView.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ItemsView.Loaded event handler");
				itemsViewLoadedEvent.Set();
			};
		}

		if (itemsViewUnloadedEvent != null)
		{
			itemsView.Unloaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("ItemsView.Unloaded event handler");
				itemsViewUnloadedEvent.Set();
			};
		}

		Grid parentGrid = null;

		if (useParentGrid)
		{
			parentGrid = new Grid();
			parentGrid.Width = c_defaultUIItemsViewWidth * 2;
			parentGrid.Height = c_defaultUIItemsViewHeight * 2;

			itemsView.HorizontalAlignment = HorizontalAlignment.Left;
			itemsView.VerticalAlignment = VerticalAlignment.Top;

			parentGrid.Children.Add(itemsView);
		}

		if (setAsContentRoot)
		{
			Log.Comment("Setting window content");
			if (useParentGrid)
			{
				Content = parentGrid;
			}
			else
			{
				Content = itemsView;
			}
		}
	}

	private void LogItemsViewProperties(ItemsView itemsView)
	{
		Log.Comment(" - itemsView.ItemTemplate: " + (itemsView.ItemTemplate == null ? "null" : "non-null"));
		Log.Comment(" - itemsView.ItemsSource: " + (itemsView.ItemsSource == null ? "null" : "non-null"));
		Log.Comment(" - itemsView.SelectedItem: " + (itemsView.SelectedItem == null ? "null" : "non-null"));
		Log.Comment(" - itemsView.CurrentItemIndex: " + itemsView.CurrentItemIndex);
		Log.Comment(" - itemsView.Layout: " + (itemsView.Layout == null ? "null" : "non-null"));
		Log.Comment(" - itemsView.Layout as StackLayout: " + ((itemsView.Layout as StackLayout) == null ? "null" : "non-null"));
		Log.Comment(" - itemsView.IsEnabled: " + itemsView.IsEnabled);
		Log.Comment(" - itemsView.IsTabStop: " + itemsView.IsTabStop);
		Log.Comment(" - itemsView.XYFocusKeyboardNavigation: " + itemsView.XYFocusKeyboardNavigation);
		Log.Comment(" - itemsView.TabNavigation: " + itemsView.TabNavigation);
		Log.Comment(" - itemsView.SelectionMode: " + itemsView.SelectionMode);
	}

	private async Task WaitForEvent(string logComment, UnoAutoResetEvent eventWaitHandle)
	{
		Log.Comment(logComment);
		if (!await eventWaitHandle.WaitOne(TimeSpan.FromMilliseconds(c_MaxWaitDuration)))
		{
			throw new Exception("Timeout expiration in WaitForEvent.");
		}
	}
}
#endif
