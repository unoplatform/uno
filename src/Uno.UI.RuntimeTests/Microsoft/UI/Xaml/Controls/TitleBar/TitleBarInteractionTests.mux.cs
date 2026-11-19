// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/TitleBarTests.cs

#if HAS_UNO_WINUI || WINDOWS
using System;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

// Upstream drives the TitleBar test page through UI Automation, where each button calls one API and
// writes the result to a status TextBlock. Here the page's TitleBar is built in code and the APIs are called directly.
[TestClass]
[RunsOnUIThread]
public class TitleBarInteractionTests
{
	[TestMethod]
	public async Task AutoRefreshDragRegionsDefaultValueTest()
	{
		await RunOnTitleBarPage(async (titleBar, _) =>
		{
			await UITestHelper.WaitForIdle();

			Verify.AreEqual(false, titleBar.AutoRefreshDragRegions, "Default AutoRefreshDragRegions should be False");
		});
	}

	[TestMethod]
	public async Task SetAutoRefreshDragRegionsTrueTest()
	{
		await RunOnTitleBarPage(async (titleBar, _) =>
		{
			titleBar.AutoRefreshDragRegions = true;
			await UITestHelper.WaitForIdle();

			Verify.AreEqual(true, titleBar.AutoRefreshDragRegions, "AutoRefreshDragRegions should be True");
		});
	}

	[TestMethod]
	public async Task SetAutoRefreshDragRegionsFalseTest()
	{
		await RunOnTitleBarPage(async (titleBar, _) =>
		{
			titleBar.AutoRefreshDragRegions = true;
			await UITestHelper.WaitForIdle();

			titleBar.AutoRefreshDragRegions = false;
			await UITestHelper.WaitForIdle();

			Verify.AreEqual(false, titleBar.AutoRefreshDragRegions, "AutoRefreshDragRegions should be False after setting back");
		});
	}

	[TestMethod]
	public async Task RecomputeDragRegionsTest()
	{
		await RunOnTitleBarPage(async (titleBar, _) =>
		{
			Exception error = null;
			try
			{
				titleBar.RecomputeDragRegions();
				await UITestHelper.WaitForIdle();
			}
			catch (Exception ex)
			{
				error = ex;
			}

			Verify.IsNull(error, "RecomputeDragRegions should succeed");
		});
	}

	[TestMethod]
	public async Task IsDragRegionSetTrueTest()
	{
		await RunOnTitleBarPage(async (_, autoSuggestBox) =>
		{
			TitleBar.SetIsDragRegion(autoSuggestBox, true);
			await UITestHelper.WaitForIdle();

			Verify.AreEqual(true, TitleBar.GetIsDragRegion(autoSuggestBox), "IsDragRegion should be True");
		});
	}

	[TestMethod]
	public async Task IsDragRegionSetFalseTest()
	{
		await RunOnTitleBarPage(async (_, autoSuggestBox) =>
		{
			TitleBar.SetIsDragRegion(autoSuggestBox, false);
			await UITestHelper.WaitForIdle();

			Verify.AreEqual(false, TitleBar.GetIsDragRegion(autoSuggestBox), "IsDragRegion should be False");
		});
	}

	[TestMethod]
	public async Task IsDragRegionDefaultValueTest()
	{
		await RunOnTitleBarPage(async (_, autoSuggestBox) =>
		{
			await UITestHelper.WaitForIdle();

			Verify.IsNull(TitleBar.GetIsDragRegion(autoSuggestBox), "Default IsDragRegion should be null");
		});
	}

	[TestMethod]
	public async Task IsDragRegionClearValueTest()
	{
		await RunOnTitleBarPage(async (_, autoSuggestBox) =>
		{
			TitleBar.SetIsDragRegion(autoSuggestBox, true);
			await UITestHelper.WaitForIdle();

			autoSuggestBox.ClearValue(TitleBar.IsDragRegionProperty);
			await UITestHelper.WaitForIdle();

			Verify.IsNull(TitleBar.GetIsDragRegion(autoSuggestBox), "IsDragRegion should be null after clearing");
		});
	}

	// Mirrors the TitleBar declared in TestUI/TitleBarPage.xaml.
	private static async Task RunOnTitleBarPage(Func<TitleBar, AutoSuggestBox, Task> test)
	{
		var autoSuggestBox = new AutoSuggestBox
		{
			Name = "AutoSuggestBoxInTitleBar",
			PlaceholderText = "Search",
			QueryIcon = new SymbolIcon(Symbol.Find),
			MinWidth = 300,
		};

		var titleBar = new TitleBar
		{
			Name = "WindowingTitleBar",
			Title = "Windowing TitleBar",
			IconSource = new SymbolIconSource { Symbol = Symbol.Home },
			Content = autoSuggestBox,
			RightHeader = new PersonPicture { Height = 32, Width = 32 },
		};

		try
		{
			await UITestHelper.Load(titleBar);

			await test(titleBar, autoSuggestBox);
		}
		finally
		{
			// The TitleBar punches regions into the shared test window; don't leak them into other tests.
			if (titleBar.XamlRoot?.ContentIslandEnvironment is { } environment)
			{
				var nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(environment.AppWindowId);
				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Passthrough);
				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Icon);
			}

			TestServices.WindowHelper.WindowContent = null;
		}
	}
}
#endif
