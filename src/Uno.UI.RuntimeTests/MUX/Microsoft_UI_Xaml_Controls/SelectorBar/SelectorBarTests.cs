#if HAS_UNO_WINUI
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.UI;
using Uno.UI.RuntimeTests;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
#if !__SKIA__
[Ignore("Only works properly on Skia")]
#endif
public class SelectorBarTests : MUXApiTestBase
{
	private const int c_MaxWaitDuration = 5000;

	private const double c_defaultUISelectorBarParentWidth = 400.0;
	private const double c_defaultUISelectorBarParentHeight = 200.0;

	[TestMethod]
	public void VerifyDefaultSelectorBarItemPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			SelectorBarItem selectorBarItem = new SelectorBarItem();
			Verify.IsNotNull(selectorBarItem);
			Verify.AreEqual("", selectorBarItem.Text);
			Verify.IsNull(selectorBarItem.Icon);
			Verify.IsNull(selectorBarItem.Child);
			Verify.IsFalse(selectorBarItem.IsSelected);
		});
	}

	[TestMethod]
	public void VerifyDefaultSelectorBarPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			SelectorBar selectorBar = new SelectorBar();
			Verify.IsNotNull(selectorBar);
			Verify.IsNotNull(selectorBar.Items);
			Verify.IsNull(selectorBar.SelectedItem);
		});
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public async Task VerifySelectorBarItems()
	{
		//using (PrivateLoggingHelper privateIVLoggingHelper = new PrivateLoggingHelper("ItemsView", "ScrollView"))
		{
			SelectorBar selectorBar = null;
			AutoResetEvent selectorBarLoadedEvent = new AutoResetEvent(false);
			AutoResetEvent selectorBarUnloadedEvent = new AutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				selectorBar = new SelectorBar();
				Verify.IsNotNull(selectorBar);
				Verify.IsNotNull(selectorBar.Items);

				SelectorBarItem selectorBarItemDeleted = new SelectorBarItem()
				{
					Text = "Deleted",
					Icon = new SymbolIcon(Symbol.Delete),
					IsEnabled = false
				};

				selectorBar.Items.Add(selectorBarItemDeleted);

				SelectorBarItem selectorBarItemRemote = new SelectorBarItem()
				{
					Text = "Remote",
					Icon = new SymbolIcon(Symbol.Remote),
					IsSelected = true
				};

				selectorBar.Items.Add(selectorBarItemRemote);

				SelectorBarItem selectorBarItemShared = new SelectorBarItem()
				{
					Text = "Shared",
					Icon = new SymbolIcon(Symbol.Share)
				};

				selectorBar.Items.Add(selectorBarItemShared);

				SelectorBarItem selectorBarItemFavorites = new SelectorBarItem()
				{
					Text = "Favorites",
					Icon = new SymbolIcon(Symbol.Favorite)
				};

				selectorBar.Items.Add(selectorBarItemFavorites);

				Verify.AreEqual(4, selectorBar.Items.Count);

				SetupDefaultUI(selectorBar, selectorBarLoadedEvent, selectorBarUnloadedEvent);
			});

			WaitForEvent("Waiting for Loaded event", selectorBarLoadedEvent);

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Logging SelectorBar property values after Loaded event");
				LogSelectorBarProperties(selectorBar);

				Log.Comment("Verifying SelectorBar property values after Loaded event");
				Verify.AreEqual(selectorBar.Items[1], selectorBar.SelectedItem);
				Verify.IsTrue(selectorBar.IsEnabled);
				Verify.IsFalse(selectorBar.IsTabStop);
				Verify.AreEqual(XYFocusKeyboardNavigationMode.Auto, selectorBar.XYFocusKeyboardNavigation);
				Verify.AreEqual(KeyboardNavigationMode.Once, selectorBar.TabNavigation);

				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				Log.Comment("Logging ItemsView property values after Loaded event");
				LogItemsViewProperties(itemsView);

				Log.Comment("Verifying ItemsView property values after Loaded event");
				Verify.IsNotNull(itemsView);
				Verify.AreEqual(XYFocusKeyboardNavigationMode.Disabled, itemsView.XYFocusKeyboardNavigation);
				Verify.AreEqual(KeyboardNavigationMode.Once, itemsView.TabNavigation);
				Verify.AreEqual(ItemsViewSelectionMode.Single, itemsView.SelectionMode);
				Verify.AreEqual(1, itemsView.SelectedItems.Count);
				Verify.AreEqual(-1, itemsView.CurrentItemIndex);
				Verify.AreEqual(selectorBar.Items[1], itemsView.SelectedItem);

				Log.Comment("Removing 2nd SelectorBarItem.");
				selectorBar.Items.RemoveAt(1);
				Verify.AreEqual(3, selectorBar.Items.Count);
				Verify.IsNull(selectorBar.SelectedItem);

				Log.Comment("Clearing all SelectorBarItems.");
				selectorBar.Items.Clear();
				Verify.AreEqual(0, selectorBar.Items.Count);

				Log.Comment("Resetting window content and SelectorBar");
				Content = null;
				selectorBar = null;
			});

			WaitForEvent("Waiting for Unloaded event", selectorBarUnloadedEvent);
			await TestServices.WindowHelper.WaitForIdle();
			Log.Comment("Done");
		}
	}

	private void SetupDefaultUI(
		SelectorBar selectorBar,
		AutoResetEvent selectorBarLoadedEvent = null,
		AutoResetEvent selectorBarUnloadedEvent = null,
		bool setAsContentRoot = true,
		bool useParentGrid = false)
	{
		Log.Comment("Setting up default UI with SelectorBar");

		Verify.IsNotNull(selectorBar);
		selectorBar.Name = "selectorBar";

		if (selectorBarLoadedEvent != null)
		{
			selectorBar.Loaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("SelectorBar.Loaded event handler");
				selectorBarLoadedEvent.Set();
			};
		}

		if (selectorBarUnloadedEvent != null)
		{
			selectorBar.Unloaded += (object sender, RoutedEventArgs e) =>
			{
				Log.Comment("SelectorBar.Unloaded event handler");
				selectorBarUnloadedEvent.Set();
			};
		}

		Grid parentGrid = null;

		if (useParentGrid)
		{
			parentGrid = new Grid();
			parentGrid.Width = c_defaultUISelectorBarParentWidth;
			parentGrid.Height = c_defaultUISelectorBarParentHeight;

			selectorBar.HorizontalAlignment = HorizontalAlignment.Left;
			selectorBar.VerticalAlignment = VerticalAlignment.Top;

			parentGrid.Children.Add(selectorBar);
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
				Content = selectorBar;
			}
		}
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task VerifySelectorBarItemVisualStateAfterPointerReleasedOutsideBounds()
	{
		var selectorBar = new SelectorBar();
		selectorBar.Items.Add(new SelectorBarItem { Text = "Item1" });
		selectorBar.Items.Add(new SelectorBarItem { Text = "Item2" });
		selectorBar.Items.Add(new SelectorBarItem { Text = "Item3" });

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		await UITestHelper.Load(selectorBar);

		var item = selectorBar.Items[1];

		// Get the bounds of the SelectorBarItem and compute its center.
		var bounds = item.GetAbsoluteBoundsRect();
		var center = new Point(bounds.GetMidX(), bounds.GetMidY());

		// Press mouse on the SelectorBarItem.
		mouse.MoveTo(center);
		await TestServices.WindowHelper.WaitForIdle();
		mouse.Press();
		await TestServices.WindowHelper.WaitForIdle();

		Verify.IsTrue(await ControlHelper.IsInVisualState(item, "CombinedStates", "UnselectedPressed"),
			"SelectorBarItem should be in UnselectedPressed state after pointer press.");

		// Move the mouse well outside the SelectorBarItem bounds and release.
		var outsidePoint = new Point(bounds.Right + 200, bounds.Bottom + 200);
		mouse.MoveTo(outsidePoint);
		mouse.Release();

		await TestServices.WindowHelper.WaitForIdle();

		// The visual state should return to UnselectedNormal, not be stuck on UnselectedPressed.
		Verify.IsTrue(await ControlHelper.IsInVisualState(item, "CombinedStates", "UnselectedNormal"),
			"SelectorBarItem should return to UnselectedNormal after pointer released outside bounds.");
	}

	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task VerifySelectorBarItemBecomesSelectedAfterMouseClick()
	{
		var selectorBar = new SelectorBar();
		selectorBar.Items.Add(new SelectorBarItem { Text = "Item1" });
		selectorBar.Items.Add(new SelectorBarItem { Text = "Item2" });
		selectorBar.Items.Add(new SelectorBarItem { Text = "Item3" });

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		await UITestHelper.Load(selectorBar);

		Verify.IsNull(selectorBar.SelectedItem);

		var item = selectorBar.Items[1];
		var bounds = item.GetAbsoluteBoundsRect();
		var center = new Point(bounds.GetMidX(), bounds.GetMidY());

		mouse.MoveTo(center);
		await TestServices.WindowHelper.WaitForIdle();
		mouse.Press();
		await TestServices.WindowHelper.WaitForIdle();
		mouse.Release();
		await TestServices.WindowHelper.WaitForIdle();

		Verify.AreEqual(item, selectorBar.SelectedItem,
			"SelectorBarItem should be selected after a mouse press+release inside its bounds.");
	}
#endif

	private string GetStringFromSelectorBarItem(SelectorBarItem selectorBarItem)
	{
		return "Icon:" + (selectorBarItem.Icon == null ? "null" : selectorBarItem.Icon.ToString()) + ", Text:" + selectorBarItem.Text + ", Child:" + (selectorBarItem.Child == null ? "null" : selectorBarItem.Child.ToString());
	}

	private void LogSelectorBarProperties(SelectorBar selectorBar)
	{
		Log.Comment(" - selectorBar.SelectedItem: " + (selectorBar.SelectedItem == null ? "null" : GetStringFromSelectorBarItem(selectorBar.SelectedItem)));
		foreach (var selectorBarItem in selectorBar.Items)
		{
			Log.Comment(" - selectorBar.Item: " + GetStringFromSelectorBarItem(selectorBarItem));
		}
		Log.Comment(" - selectorBar.IsEnabled: " + selectorBar.IsEnabled);
		Log.Comment(" - selectorBar.IsTabStop: " + selectorBar.IsTabStop);
		Log.Comment(" - selectorBar.XYFocusKeyboardNavigation: " + selectorBar.XYFocusKeyboardNavigation);
		Log.Comment(" - selectorBar.TabNavigation: " + selectorBar.TabNavigation);
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

	private void WaitForEvent(string logComment, EventWaitHandle eventWaitHandle)
	{
		Log.Comment(logComment);
		if (!eventWaitHandle.WaitOne(TimeSpan.FromMilliseconds(c_MaxWaitDuration)))
		{
			throw new Exception("Timeout expiration in WaitForEvent.");
		}
	}
}
#endif
