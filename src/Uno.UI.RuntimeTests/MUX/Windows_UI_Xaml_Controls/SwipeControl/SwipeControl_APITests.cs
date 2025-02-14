// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeControl_APITests/SwipeControlTests.cs

using MUXControlsTestApp.Utilities;
using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using SwipeMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeMode;
using SwipeItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItem;
using SwipeItems = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItems;
using SwipeControl = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeControl;
using FontIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.FontIconSource;
using System.Threading.Tasks;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class SwipeControlTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task SwipeItemTest()
		{
			SwipeItem swipeItem = null;
			RunOnUIThread.Execute(() =>
			{
				swipeItem = new SwipeItem();
				swipeItem.Text = "Selfie";
				swipeItem.IconSource = new FontIconSource() { Glyph = "&#xE114;" };
				swipeItem.Background = new SolidColorBrush(Windows.UI.Colors.Red);
				swipeItem.Foreground = new SolidColorBrush(Windows.UI.Colors.Blue);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(swipeItem.Text, "Selfie");
				Verify.IsTrue(swipeItem.IconSource is FontIconSource);
				Verify.AreEqual((swipeItem.IconSource as FontIconSource).Glyph, "&#xE114;");
				Verify.AreEqual(((SolidColorBrush)swipeItem.Background).Color, Windows.UI.Colors.Red);
				Verify.AreEqual(((SolidColorBrush)swipeItem.Foreground).Color, Windows.UI.Colors.Blue);
			});
		}

		[TestMethod]
		public async Task SwipeItemsTest()
		{
			SwipeItems swipeItems = null;

			RunOnUIThread.Execute(() =>
			{
				swipeItems = new SwipeItems();

				// verify the default value
				Verify.AreEqual(swipeItems.Mode, SwipeMode.Reveal);
				Verify.AreEqual(swipeItems.Count, 0);

				swipeItems.Add(new SwipeItem());
				swipeItems.Add(new SwipeItem());
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(swipeItems.Count, 2);
			});
		}

		[TestMethod]
		public void SwipeItemsExecuteThrowsExceptionWhenMoreThanOneItemAreAdded()
		{
			RunOnUIThread.Execute(() =>
			{
				var swipeItems = new SwipeItems();
				swipeItems.Mode = SwipeMode.Execute;
				swipeItems.Add(new SwipeItem());
				Verify.Throws<ArgumentException>(() => { swipeItems.Add(new SwipeItem()); });
			});
		}

		[TestMethod]
		public void SwipeControlTest()
		{
			RunOnUIThread.Execute(() =>
			{
				SwipeControl swipeControl = new SwipeControl();
				Verify.AreEqual(swipeControl.ActualHeight, 0);
				Verify.AreEqual(swipeControl.ActualWidth, 0);
				Verify.IsNull(swipeControl.LeftItems);
				Verify.IsNull(swipeControl.RightItems);
				Verify.IsNull(swipeControl.TopItems);
				Verify.IsNull(swipeControl.BottomItems);
				swipeControl.LeftItems = new SwipeItems();
				swipeControl.RightItems = new SwipeItems();
				Content = swipeControl;
				Content.UpdateLayout();
				Verify.IsFalse(swipeControl.IsTabStop);
				Verify.IsNotNull(swipeControl.LeftItems);
				Verify.IsNotNull(swipeControl.RightItems);
			});
		}

		[TestMethod]
		public void SwipeControlCanOnlyBeHorizontalOrVertical()
		{
			RunOnUIThread.Execute(() =>
			{
				SwipeControl swipeControl = new SwipeControl();
				swipeControl.LeftItems = new SwipeItems();
				var topItems = new SwipeItems();
				topItems.Add(new SwipeItem());
				swipeControl.TopItems = topItems;
				Verify.Throws<ArgumentException>(() => { swipeControl.LeftItems.Add(new SwipeItem()); });
			});
		}

		[TestMethod]
		public void SwipeControlCanOnlyBeHorizontalOrVerticalAfterRendering()
		{
			var resetEvent = new UnoAutoResetEvent(false);
			RunOnUIThread.Execute(() =>
			{
				SwipeControl swipeControl = new SwipeControl();
				swipeControl.Loaded += (object sender, RoutedEventArgs args) => { resetEvent.Set(); };
				Content = swipeControl;
				Content.UpdateLayout();
				swipeControl.TopItems = new SwipeItems();
				swipeControl.LeftItems = new SwipeItems();
				swipeControl.LeftItems.Add(new SwipeItem());
				Verify.Throws<ArgumentException>(() => { swipeControl.TopItems.Add(new SwipeItem()); });
			});
		}

#if false
		[TestMethod]
		public void MarkupDefinedSwipeItemDoesNotCrash()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone3))
			{
				Log.Warning("Test is disabled pre RS3.");
				return;
			}

			RunOnUIThread.Execute(() =>
			{
				var rootGrid = (Windows.UI.Xaml.Controls.Grid)XamlReader.LoadWithInitialTemplateValidation(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'> " +
					"<GridView> " +
						"<GridViewItem> " +
							"<SwipeControl> " +
								"<SwipeControl.RightItems> " +
									"<SwipeItems> " +
										"<SwipeItem " +
											"Background='#E81123' " +
											"Foreground='White' " +
											"Text='Remove'/> " +
									"</SwipeItems> " +
								"</SwipeControl.RightItems> " +
								"<Grid Width='200' Height='200' Background='green'/> " +
							"</SwipeControl> " +
						"</GridViewItem> " +
					"</GridView> " +
				"</Grid>");

				Content = rootGrid;
				Content.UpdateLayout();
			});
		}
#endif
	}
}
