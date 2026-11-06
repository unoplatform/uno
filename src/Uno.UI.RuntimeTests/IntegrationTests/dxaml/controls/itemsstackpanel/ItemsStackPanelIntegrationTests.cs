// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Controls;

[TestClass]
[RequiresFullWindow]
[RequiresScaling(1.0f)]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class ItemsStackPanelIntegrationTests : BaseDxamlTestClass
{
	private const int s_itemCount = 3;
	private const int s_itemCountForVirtualization = 9;

	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	[ClassCleanup]
	public static void TestCleanup()
	{
		WindowHelper.VerifyTestCleanup();
	}

	//
	// Test Cases
	//
	[TestMethod]
	[RunsOnUIThread]
	public void CanInstantiate()
	{
		var act = () => new ItemsStackPanel();
		act.Should().NotThrow();
	}

	[TestMethod]
	public async Task CanEnterAndLeaveLiveTree()
	{
		var itemsControl = await PanelsHelper.CreateItemsControlWithPanel<ItemsControl, ItemsStackPanel>();

		var loadedEvent = new Event();
		var unloadedEvent = new Event();

		await RunOnUIThread(() =>
		{
			itemsControl.Loaded += (s, e) => loadedEvent.Set();
			itemsControl.Unloaded += (s, e) => unloadedEvent.Set();
			WindowHelper.WindowContent = itemsControl;
		});
		await loadedEvent.WaitForDefault();

		await RunOnUIThread(() => WindowHelper.WindowContent = null);
		await unloadedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task CanStackItemsHorizontally()
	{
		var itemsControl = await PanelsHelper.AddItemsControlWithPanel<ItemsControl, ItemsStackPanel>(s_itemCount, "Orientation='Horizontal'");
		var expectedPositions = new List<Point>();

		for (int i = 0; i < s_itemCount; i++)
		{
			expectedPositions.Add(new Point(100.0f * i, 100.0f));
		}

		await PanelsHelper.VerifyItemPositions(itemsControl, expectedPositions);
	}

	[TestMethod]
	public async Task CanStackItemsVertically()
	{
		var itemsControl = await PanelsHelper.AddItemsControlWithPanel<ItemsControl, ItemsStackPanel>(s_itemCount, "Orientation='Vertical'");
		var expectedPositions = new List<Point>();

		for (int i = 0; i < s_itemCount; i++)
		{
			expectedPositions.Add(new Point(100.0f, 100.0f * i));
		}

		await PanelsHelper.VerifyItemPositions(itemsControl, expectedPositions);
	}

	[TestMethod]
	public async Task CacheIsUpdatedDuringScrolling()
	{
		var listView = await PanelsHelper.AddItemsControlWithPanel<ListView, ItemsStackPanel>(s_itemCountForVirtualization, "Orientation='Vertical'");
		var itemsWrapGrid = await ItemsControlHelper.GetPanelFromItemsControl<ItemsStackPanel>(listView);
		Control firstContainer = null;

		await RunOnUIThread(() =>
		{
			firstContainer = (Control)listView.ContainerFromIndex(0);
		});

		await FocusTestHelper.EnsureFocus(firstContainer, FocusState.Pointer);

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(itemsWrapGrid.FirstVisibleIndex, 0);
			VERIFY_ARE_EQUAL(itemsWrapGrid.LastVisibleIndex, 2);

			VERIFY_ARE_EQUAL(itemsWrapGrid.FirstCacheIndex, 0);
			VERIFY_IS_GREATER_THAN_OR_EQUAL(itemsWrapGrid.LastCacheIndex, 2);
		});

		await ItemsControlHelper.ScrollToIndex<ListView, UIElement>(listView, s_itemCountForVirtualization - 1);

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(itemsWrapGrid.FirstVisibleIndex, 6);
			VERIFY_ARE_EQUAL(itemsWrapGrid.LastVisibleIndex, 8);

			VERIFY_IS_LESS_THAN_OR_EQUAL(itemsWrapGrid.FirstCacheIndex, 6);
			VERIFY_ARE_EQUAL(itemsWrapGrid.LastCacheIndex, 8);
		});
	}
}
