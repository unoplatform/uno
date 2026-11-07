// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.MUX.Helpers;
using InputHelper = Private.Infrastructure.TestServices.InputHelper;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Enterprise
{
	[TestClass]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public partial class ListViewIntegrationTests : BaseDxamlTestClass
	{
		[Flags]
		private enum VerifyTabOrderFlags : uint
		{
			Default = 0,
			UseVirtualizingStackPanelAsItemsPanel = 1,
			UseStackPanelAsItemsPanel = 2,
			UseHeaderAndFooter = 4,
			UseGroupedItems = 8,
			UseNestedButtons = 16
		}

		//
		// Class & Test Setup
		//
		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
		}

		[TestCleanup]
		public async Task TestCleanup()
		{
			await RunOnUIThread(() => WindowHelper.WindowContent = null);
			WindowHelper.VerifyTestCleanup();
		}

		// WinUI's WaitForIdle also waits for DirectManipulation, which runs the animated view changes.
		private static async Task WaitForScrollViewerIdle(ScrollViewer scrollViewer)
		{
			await WindowHelper.WaitFor(() => !scrollViewer.IsInManipulation);
			await WindowHelper.WaitForIdle();
		}

		//
		// Test Cases
		//
		[TestMethod]
		public async Task AnimateItemIntoViewWithGamepad()
		{
			TestCleanupWrapper cleanup;

			InputDevice device = InputDevice.Gamepad;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			var rootLoadedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			ScrollViewer scrollViewer = null;
			ListView listView = null;
			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewXaml);
			VERIFY_IS_NOT_NULL(rootPage);

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootPage;
				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Root FE Loaded handler.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootPage.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				AttachScrollViewerLogging(viewChangingRegistration, viewChangedRegistration, scrollViewer);

				listView.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving 10 items down, to the 11th item, with the gamepad.");
			for (int i = 0; i < 10; i++)
			{
				await CommonInputHelper.Down(device);
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();
			await WaitForScrollViewerIdle(scrollViewer);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 124.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 10);
			});

			LOG_OUTPUT("Moving 10 times up, to the 1st item, with the gamepad.");
			for (int i = 0; i < 10; i++)
			{
				await CommonInputHelper.Up(device);
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();
			await WaitForScrollViewerIdle(scrollViewer);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 0);
			});
		}

		[TestMethod]
		public async Task JumpItemIntoViewWithKeyboard()
		{
			await JumpItemIntoViewWithKeyboard(false);
		}

		[TestMethod]
		public async Task JumpItemIntoViewWithKeyboardWithRootCanvas()
		{
			await JumpItemIntoViewWithKeyboard(true);
		}

		[TestMethod]
		[Ignore("Disabled in the WinUI test suite (TEST_METHOD_PROPERTY Ignore=TRUE in ListViewIntegrationTests.h).")]
		public async Task AnimateZoomableItemIntoView()
		{
			TestCleanupWrapper cleanup;

			InputDevice device = InputDevice.Gamepad;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			var rootLoadedEvent = new Event();
			var viewChangedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			ScrollViewer scrollViewer = null;
			ListView listView = null;
			ListViewItem listViewItem = null;
			Grid rootGrid = await LoadXamlFileOnUIThread<Grid>(ZoomableListViewXaml);
			VERIFY_IS_NOT_NULL(rootGrid);

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootGrid;
				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(
					rootGrid,
					(s, e) =>
					{
						LOG_OUTPUT("Root FE Loaded handler.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootGrid.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				AttachScrollViewerLogging(
					viewChangingRegistration,
					viewChangedRegistration,
					scrollViewer,
					onViewChanged: args =>
					{
						if (!args.IsIntermediate)
						{
							viewChangedEvent.Set();
						}
					});
			});

			await RunOnUIThread(() =>
			{
				scrollViewer.ChangeView(null, null, 1.2f, true /*disableAnimation*/);
				listViewItem = (ListViewItem)listView.ContainerFromIndex(0);
			});

			LOG_OUTPUT("Waiting for ScrollViewer ViewChanged event.");
			await viewChangedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Moving focus to first item.");
				listViewItem.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving 8 items down, to the 9th item, with the gamepad.");
			for (int i = 0; i < 8; i++)
			{
				await CommonInputHelper.Down(device);
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_IS_TRUE(scrollViewer.VerticalOffset >= 175.0);
				VERIFY_IS_TRUE(scrollViewer.VerticalOffset <= 176.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.2f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 8);
			});

			LOG_OUTPUT("Moving 8 times up, to the 1st item, with the gamepad.");
			for (int i = 0; i < 8; i++)
			{
				await CommonInputHelper.Up(device);
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Scrolling within the top margin, all the way to the top, with the gamepad.");
			for (int i = 0; i < 4; i++)
			{
				await CommonInputHelper.Up(device);
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.2f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 0);
			});


			LOG_OUTPUT("Moving to the end.");
			await KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving to bottom of margin.");
			await KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_IS_TRUE(scrollViewer.VerticalOffset >= 2538.0);
				VERIFY_IS_TRUE(scrollViewer.VerticalOffset <= 2539.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.2f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 52);
			});

			LOG_OUTPUT("Moving to the top.");
			await KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 40.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.2f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 0);
			});
		}

		[TestMethod]
		[RequiresScaling(1f)]
		public async Task AnimateItemIntoViewProgrammatically()
		{
			TestCleanupWrapper cleanup;

			InputDevice device = InputDevice.Gamepad;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			var rootLoadedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			CollectionViewSource cvs = null;
			List<object> itemsSource = null;

			ScrollViewer scrollViewer = null;
			ListView listView = null;
			ListViewItem listViewItem = null;
			Grid rootGrid = await LoadXamlFileOnUIThread<Grid>(GroupedListViewXaml);
			VERIFY_IS_NOT_NULL(rootGrid);

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootGrid;
				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(
					rootGrid,
					(s, e) =>
					{
						LOG_OUTPUT("Root FE Loaded handler.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootGrid.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				cvs = (CollectionViewSource)rootGrid.Resources["cvs"]; // UNO TODO: FindName doesn't see resources.
				VERIFY_IS_NOT_NULL(cvs);

				itemsSource = GetGroupedData();
				VERIFY_IS_NOT_NULL(itemsSource);

				cvs.Source = itemsSource;
				cvs.IsSourceGrouped = true;
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				AttachScrollViewerLogging(viewChangingRegistration, viewChangedRegistration, scrollViewer);

				listViewItem = (ListViewItem)listView.ContainerFromIndex(0);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Moving focus to first item.");
				listViewItem.Focus(FocusState.Keyboard);

			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving to 2nd item.");
			await CommonInputHelper.Down(device);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 1);

				LOG_OUTPUT("Animating to 10th item.");
				listViewItem = (ListViewItem)listView.ContainerFromIndex(9);
				object dataItem = listView.ItemFromContainer(listViewItem);
				ScrollIntoViewWithOptionalAnimation(listView, dataItem, ScrollIntoViewAlignment.Leading, false /*disableAnimation*/);
			});

			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();
			await WaitForScrollViewerIdle(scrollViewer);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 536.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 1);

				LOG_OUTPUT("Animating to footer.");
				ScrollIntoViewWithOptionalAnimation(listView, listView.Footer, ScrollIntoViewAlignment.Default, false /*disableAnimation*/);
			});

			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();
			await WaitForScrollViewerIdle(scrollViewer);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 1052.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 1);

				LOG_OUTPUT("Animating to header.");
				ScrollIntoViewWithOptionalAnimation(listView, listView.Header, ScrollIntoViewAlignment.Default, false /*disableAnimation*/);
			});

			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();
			await WaitForScrollViewerIdle(scrollViewer);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 1);

				LOG_OUTPUT("Animating to 4th group header.");
				var group = (ICollectionViewGroup)cvs.View.CollectionGroups[3];
				VERIFY_IS_NOT_NULL(group);
				VERIFY_IS_NOT_NULL(group.Group);
				ScrollIntoViewWithOptionalAnimation(listView, group.Group, ScrollIntoViewAlignment.Leading, false /*disableAnimation*/);
			});

			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();
			await WaitForScrollViewerIdle(scrollViewer);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 1052.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 1);
			});
		}

		[TestMethod]
		[Ignore("Needs TestServices.Utilities.ShrinkApplicationViewVisibleBounds (shrinks ApplicationView.VisibleBounds), which has no Uno equivalent.")]
		public async Task BringItemIntoShrunkViewWithGamepad()
		{
			// The WinUI cleanup also calls TestServices.Utilities.ShrinkApplicationViewVisibleBounds(false).
			TestCleanupWrapper cleanup;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			// TestServices.Utilities.ShrinkApplicationViewVisibleBounds(true) is not available.

			var rootLoadedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			ScrollViewer scrollViewer = null;
			ListView listView = null;
			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewXaml);
			VERIFY_IS_NOT_NULL(rootPage);

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootPage;
				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Root FE Loaded handler.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootPage.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				AttachScrollViewerLogging(viewChangingRegistration, viewChangedRegistration, scrollViewer);

				listView.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving 11 items down, to the 12th item, with the gamepad.");
			for (int i = 0; i < 11; i++)
			{
				await KeyboardHelper.GamepadDpadDown();
				await WindowHelper.WaitForIdle();
			}

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 168.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 11);
			});

			LOG_OUTPUT("Moving 8 times up, to the 4th item, with the gamepad.");
			for (int i = 0; i < 8; i++)
			{
				await KeyboardHelper.GamepadDpadUp();
				await WindowHelper.WaitForIdle();
			}

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_IS_TRUE(scrollViewer.VerticalOffset <= 82.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 3);
			});

			LOG_OUTPUT("Moving 3 times up, to the 1st item, with the gamepad.");
			for (int i = 0; i < 3; i++)
			{
				await KeyboardHelper.GamepadDpadUp();
				await WindowHelper.WaitForIdle();
			}

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 0);
			});
		}

		// Uses the gamepad up button to interrupt and reverse ScrollViewer's inertia.
		[TestMethod]
		public async Task StopInertialManipulationWithGamepad()
		{
			TestCleanupWrapper cleanup;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			var rootLoadedEvent = new Event();
			var viewChangedEvent = new Event();
			var inertiaEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			ScrollViewer scrollViewer = null;
			ListView listView = null;
			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewXaml);
			VERIFY_IS_NOT_NULL(rootPage);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Root FE Loaded handler.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootPage.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				WindowHelper.WindowContent = rootPage;
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				AttachScrollViewerLogging(
					viewChangingRegistration,
					viewChangedRegistration,
					scrollViewer,
					onViewChanging: args =>
					{
						if (args.IsInertial)
						{
							inertiaEvent.Set();
						}
					},
					onViewChanged: args =>
					{
						if (!args.IsIntermediate)
						{
							viewChangedEvent.Set();
						}
					});

				listView.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving 2 pages down.");
			for (int i = 0; i < 2; i++)
			{
				await KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			LOG_OUTPUT("Waiting for ScrollViewer ViewChanged event.");
			await viewChangedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 16);

				LOG_OUTPUT("Invoking ChangeView on ListView's ScrollViewer.");
				inertiaEvent.Reset();
				scrollViewer.ChangeView(null, 800.0, null);
			});

			// Let the ChangeView operation start in inertia mode.
			// TODO Uno: ScrollViewer.ViewChanging is not raised, so the motion is observed on the ScrollViewer.
			await WindowHelper.WaitFor(() => scrollViewer.IsInManipulation);

			LOG_OUTPUT("Moving up once with the gamepad.");
			await CommonInputHelper.Up(InputDevice.Gamepad);
			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 15);
			});
		}

		// Holds an item to bring up an empty context menu and then tries to pan the list with DManip.
		[TestMethod]
		[Ignore("Needs touch hold/pan injection (TestServices.InputHelper.DynamicPressCenter/DynamicRelease throw NotImplementedException) and the DirectManipulationStarted/Completed events, which Uno does not raise.")]
		public async Task CanOpenEmptyMenuFlyoutWhilePanningListView()
		{
			TestCleanupWrapper cleanup;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			var rootLoadedEvent = new Event();
			var holdingStartedEvent = new Event();
			var flyoutOpenedEvent = new Event();
			var flyoutClosedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var lviHoldingRegistration = CreateSafeEventRegistration<ListViewItem, HoldingEventHandler>("Holding");
			var directManipulationStartedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<object>>("DirectManipulationStarted");
			var directManipulationCompletedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<object>>("DirectManipulationCompleted");
			var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>("Closed");

			int directManipulationStartedCount = 0;
			int directManipulationCompletedCount = 0;

			MenuFlyout menuFlyout = null;
			ListViewItem listViewItem = null;
			ScrollViewer scrollViewer = null;
			ListView listView = null;
			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewWithDraggableItemsXaml);
			VERIFY_IS_NOT_NULL(rootPage);

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootPage;

				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Loaded raised.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootPage.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				var data = new ObservableCollection<string>();
				data.Add("ListViewItem");

				listView.ItemsSource = data;
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listViewItem = (ListViewItem)listView.ContainerFromIndex(0);
				VERIFY_IS_NOT_NULL(listViewItem);

				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				directManipulationStartedRegistration.Attach(scrollViewer, (s, e) =>
				{
					LOG_OUTPUT("DirectManipulationStarted raised.");
					directManipulationStartedCount++;
				});

				directManipulationCompletedRegistration.Attach(scrollViewer, (s, e) =>
				{
					LOG_OUTPUT("DirectManipulationCompleted raised.");
					directManipulationCompletedCount++;
				});

				lviHoldingRegistration.Attach(listViewItem, (s, args) =>
				{
					switch (args.HoldingState)
					{
						case Microsoft.UI.Input.HoldingState.Started:
							LOG_OUTPUT("Holding HoldingState==Started raised.");
							holdingStartedEvent.Set();
							break;
						case Microsoft.UI.Input.HoldingState.Completed:
							LOG_OUTPUT("Holding HoldingState==Completed raised.");
							break;
						case Microsoft.UI.Input.HoldingState.Canceled:
							LOG_OUTPUT("Holding HoldingState==Canceled raised.");
							break;
						default:
							Assert.Fail("Unexpected value for HoldingState");
							break;
					}
				});
			});

			LOG_OUTPUT("Finger down on ListViewItem.");
			InputHelper.DynamicPressCenter(listViewItem, 0, 0, PointerFinger.Finger1);
			LOG_OUTPUT("Waiting for ListViewItem Holding event with HoldingState==Started.");
			await WindowHelper.WaitForIdle();
			await holdingStartedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Showing empty MenuFlyout.");
				menuFlyout = new MenuFlyout();
				menuFlyout.Placement = FlyoutPlacementMode.Bottom;

				openedRegistration.Attach(menuFlyout, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout Opened raised.");
					flyoutOpenedEvent.Set();
				});

				closedRegistration.Attach(menuFlyout, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout Closed raised.");
					flyoutClosedEvent.Set();
				});

				menuFlyout.ShowAt(listViewItem);
			});

			LOG_OUTPUT("Waiting for MenuFlyout Opened event.");
			await WindowHelper.WaitForIdle();
			await flyoutOpenedEvent.WaitForDefault();

			LOG_OUTPUT("Finger move on ListViewItem.");
			for (int i = 0; i < 10; i++)
			{
				InputHelper.DynamicPressCenter(listViewItem, 0, 10 * i, PointerFinger.Finger1);
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT("Finger up from ListViewItem.");
			InputHelper.DynamicRelease(PointerFinger.Finger1);
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Tapping ListViewItem to close MenuFlyout.");
			InputHelper.Tap(listViewItem);
			LOG_OUTPUT("Waiting for MenuFlyout Closed event.");
			await WindowHelper.WaitForIdle();
			await flyoutClosedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				// No DManip manipulation is expected to start
				VERIFY_ARE_EQUAL(directManipulationStartedCount, 0);

				// Expecting one DirectManipulationCompleted event when moving Finger1
				VERIFY_ARE_EQUAL(directManipulationCompletedCount, 1);
			});
		}

		[TestMethod]
		public async Task VerifyContainerFromItemAfterScrollIntoView()
		{
			TestCleanupWrapper cleanup;

			// Regression coverage for:
			// [Calculator] [XAML]: Narrator focus is easily decoupled from Keyboard focus when moving into and out of flyouts
			//
			// The root cause of this bug was an issue with container virtualization in ModernCollectionBasePanel.
			// ListView->GetContainerFromIndex and ListView->GetContainerFromItem were giving inconsistent results when
			// called immediately after ListView->ScrollIntoView

			ListView listView = null;

			await RunOnUIThread(() =>
			{
				listView = new ListView();
				listView.Height = 200;

				for (int i = 0; i < 50; i++)
				{
					listView.Items.Add("Item " + i);
				}

				WindowHelper.WindowContent = listView;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				int index = 40;
				var item = listView.Items[index];

				// Calling ScrollIntoView should cause the container for the item to get realized:
				listView.ScrollIntoView(item);

				// We should be able to get the container using either the ContainerFromIndex or ContainerFromItem APIs.
				var containerFromIndex = (ListViewItem)listView.ContainerFromIndex(index);
				VERIFY_IS_NOT_NULL(containerFromIndex);

				var containerFromItem = (ListViewItem)listView.ContainerFromItem(item);
				VERIFY_IS_NOT_NULL(containerFromItem);

				VERIFY_IS_TRUE(containerFromIndex.Equals(containerFromItem));
			});
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithLocalTabNavigation()
		{
			string expectedFocusSequence = "[B][1][.1][2][.1][3][.1][A][.1][3][.1][2][.1][1][B]";

			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.Default, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseStackPanelAsItemsPanel, expectedFocusSequence);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithCycleTabNavigation()
		{
			string expectedFocusSequence = "[B][1][.1][2][.1][3][.1][1][.1][3][.1][2][.1][1][.1]";

			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.Default, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseStackPanelAsItemsPanel, expectedFocusSequence);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithOnceTabNavigation()
		{
			string expectedFocusSequence = "[B][1][.1][A][.1][1][B]";

			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.Default, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseStackPanelAsItemsPanel, expectedFocusSequence);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithNestedButtons()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseNestedButtons, "[B][SI][one][SI][one][SI][one][A][one][SI][one][SI][one][SI][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseNestedButtons, "[B][SI][one][SI][one][SI][one][SI][one][SI][one][SI][one][SI][one]");
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseNestedButtons, "[B][SI][one][A][one][SI][B]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithLocalTabNavigationHeaderAndFooter()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter, "[B][H][1][.1][2][.1][3][.1][F][A][F][.1][3][.1][2][.1][1][H][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][A][.1][3][.1][2][.1][1][H][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][F][A][F][.1][3][.1][2][.1][1][H][B]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithCycleTabNavigationHeaderAndFooter()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter, "[B][H][1][.1][2][.1][3][.1][F][H][F][.1][3][.1][2][.1][1][H][F]");
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][H][.1][3][.1][2][.1][1][H][.1]");
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][F][H][F][.1][3][.1][2][.1][1][H][F]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithOnceTabNavigationHeaderAndFooter()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter, "[B][H][1][.1][F][A][F][.1][1][H][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][1][.1][A][.1][1][H][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseStackPanelAsItemsPanel, "[B][H][1][.1][F][A][F][.1][1][H][B]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithGroupedItemsAndLocalTabNavigation()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseGroupedItems, "[B][1][11][12][2][21][22][A][22][21][2][12][11][1][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][11][12][21][22][A][B][11][B][A][22][21][12][11][B]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithGroupedItemsAndCycleTabNavigation()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseGroupedItems, "[B][1][11][12][2][21][22][1][22][21][2][12][11][1][22]");
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][11][12][21][22][11][12][21][12][11][22][21][12][11][22]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithGroupedItemsAndOnceTabNavigation()
		{
			string expectedFocusSequence = "[B][11][A][B][A][11][B]";

			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseGroupedItems, expectedFocusSequence);
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithGroupedItemsLocalTabNavigationHeaderAndFooter()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems, "[B][H][1][11][12][2][21][22][F][A][F][22][21][2][12][11][1][H][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][11][12][21][22][A][B][H][B][A][22][21][12][11][H][B]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithGroupedItemsCycleTabNavigationHeaderAndFooter()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems, "[B][H][1][11][12][2][21][22][F][H][F][22][21][2][12][11][1][H][F]");
			await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][11][12][21][22][H][11][12][11][H][22][21][12][11][H][22]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithGroupedItemsOnceTabNavigationHeaderAndFooter()
		{
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems, "[B][H][11][F][A][B][A][F][11][H][B]");
			await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][11][A][B][A][11][H][B]");
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithVirtualizedItemsAndLocalTabNavigation()
		{
			TestCleanupWrapper cleanup;

			string focusSequence = "";

			var rootLoadedEvent = new Event();
			var rootLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var rootGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");

			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewForTabNavigationXaml);
			Button beforeButton = null;
			Button afterButton = null;

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootPage;

				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Loaded raised.");
						rootLoadedEvent.Set();
					});

				rootGotFocusRegistration.Attach(
					rootPage,
					(s, args) =>
					{
						var originalSourceFE = args.OriginalSource as FrameworkElement;
						VERIFY_IS_NOT_NULL(originalSourceFE);

						object tag = originalSourceFE.Tag;

						if (tag != null)
						{
							focusSequence += "[" + tag + "]";
						}
					});

				beforeButton = (Button)rootPage.FindName("BeforeButton");
				VERIFY_IS_NOT_NULL(beforeButton);

				afterButton = (Button)rootPage.FindName("AfterButton");
				VERIFY_IS_NOT_NULL(afterButton);

				ListView mainVirtualizedListView = (ListView)rootPage.FindName("MainVirtualizedListView");
				VERIFY_IS_NOT_NULL(mainVirtualizedListView);

				mainVirtualizedListView.Visibility = Visibility.Visible;
			});

			LOG_OUTPUT("Waiting for content to load.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Giving focus to AfterButton.");
			await FocusTestHelper.EnsureFocus(afterButton, FocusState.Keyboard);

			LOG_OUTPUT("Pressing shift-tab twice.");
			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Pressing tab twice.");
			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Giving focus to the BeforeButton.");
			await FocusTestHelper.EnsureFocus(beforeButton, FocusState.Keyboard);

			LOG_OUTPUT("Pressing tab twice.");
			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Pressing shift-tab 5 times.");
			for (int tabOccurrence = 0; tabOccurrence < 5; tabOccurrence++)
			{
				await KeyboardHelper.ShiftTab();
				await WindowHelper.WaitForIdle();
			}

			string expectedFocusSequence = "[B][A][.1][15][.1][A][B][1][.1][1][B][A][.1][15]";

			LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
			LOG_OUTPUT("Actual focus sequence:   %s", focusSequence);
			VERIFY_ARE_EQUAL(expectedFocusSequence, focusSequence);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task VerifyTabOrderWithVirtualizedItemsAndCycleTabNavigation()
		{
			TestCleanupWrapper cleanup;

			string focusSequence = "";

			var rootLoadedEvent = new Event();
			var rootLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var rootGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");

			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewForTabNavigationXaml);
			Button beforeButton = null;
			Button afterButton = null;

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootPage;

				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Loaded raised.");
						rootLoadedEvent.Set();
					});

				rootGotFocusRegistration.Attach(
					rootPage,
					(s, args) =>
					{
						var originalSourceFE = args.OriginalSource as FrameworkElement;
						VERIFY_IS_NOT_NULL(originalSourceFE);

						object tag = originalSourceFE.Tag;

						if (tag != null)
						{
							focusSequence += "[" + tag + "]";
						}
					});

				beforeButton = (Button)rootPage.FindName("BeforeButton");
				VERIFY_IS_NOT_NULL(beforeButton);

				afterButton = (Button)rootPage.FindName("AfterButton");
				VERIFY_IS_NOT_NULL(afterButton);

				ListView mainVirtualizedListView = (ListView)rootPage.FindName("MainVirtualizedListView");
				VERIFY_IS_NOT_NULL(mainVirtualizedListView);

				mainVirtualizedListView.Visibility = Visibility.Visible;
				mainVirtualizedListView.TabNavigation = KeyboardNavigationMode.Cycle;
			});

			LOG_OUTPUT("Waiting for content to load.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Giving focus to AfterButton.");
			await FocusTestHelper.EnsureFocus(afterButton, FocusState.Keyboard);

			LOG_OUTPUT("Pressing shift-tab twice.");
			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Pressing tab 5 times.");
			for (int tabOccurrence = 0; tabOccurrence < 5; tabOccurrence++)
			{
				await KeyboardHelper.Tab();
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT("Pressing shift-tab 5 times.");
			for (int tabOccurrence = 0; tabOccurrence < 5; tabOccurrence++)
			{
				await KeyboardHelper.ShiftTab();
				await WindowHelper.WaitForIdle();
			}

			string expectedFocusSequence = "[B][A][.1][15][.1][1][.1][2][.1][2][.1][1][.1][15]";

			LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
			LOG_OUTPUT("Actual focus sequence:   %s", focusSequence);
			VERIFY_ARE_EQUAL(expectedFocusSequence, focusSequence);
		}

		private async Task VerifyTabOrder(
			KeyboardNavigationMode tabNavigation,
			VerifyTabOrderFlags flags,
			string expectedFocusSequence)
		{
			if (flags.HasFlag(VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel))
			{
				// UNO TODO: VirtualizingStackPanel doesn't exist.
				return;
			}

			TestCleanupWrapper cleanup;

			string focusSequence = "";

			CollectionViewSource cvs = null;
			List<object> itemsSource = null;

			var rootLoadedEvent = new Event();
			var rootLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var rootGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");

			Page rootPage = await LoadXamlFileOnUIThread<Page>(flags.HasFlag(VerifyTabOrderFlags.UseGroupedItems) ? GroupedListViewForTabNavigationXaml : ListViewForTabNavigationXaml);
			Button beforeButton = null;
			ListView mainListView = null;

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = rootPage;

				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Loaded raised.");
						rootLoadedEvent.Set();
					});

				rootGotFocusRegistration.Attach(
					rootPage,
					(s, args) =>
					{
						var originalSourceFE = args.OriginalSource as FrameworkElement;
						VERIFY_IS_NOT_NULL(originalSourceFE);

						object tag = originalSourceFE.Tag;

						if (tag == null)
						{
							var selectorItem = originalSourceFE as SelectorItem;

							if (selectorItem == null)
							{
								var listViewHeaderItem = (ListViewHeaderItem)originalSourceFE;
								VERIFY_IS_NOT_NULL(listViewHeaderItem);
								VERIFY_IS_NOT_NULL(listViewHeaderItem.Content);

								var groupHeader = (GroupedHeader)listViewHeaderItem.Content;
								VERIFY_IS_NOT_NULL(groupHeader);

								focusSequence += "[" + groupHeader.Header + "]";
							}
							else
							{
								focusSequence += "[SI]";
							}
						}
						else
						{
							focusSequence += "[" + tag + "]";
						}
					});

				beforeButton = (Button)rootPage.FindName("BeforeButton");
				VERIFY_IS_NOT_NULL(beforeButton);

				if (flags.HasFlag(VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel))
				{
					mainListView = (ListView)rootPage.FindName("MainListViewWithVirtualizingStackPanel");
				}
				else if (flags.HasFlag(VerifyTabOrderFlags.UseStackPanelAsItemsPanel))
				{
					mainListView = (ListView)rootPage.FindName("MainListViewWithStackPanel");
				}
				else if (flags.HasFlag(VerifyTabOrderFlags.UseNestedButtons))
				{
					mainListView = (ListView)rootPage.FindName("MainListViewWithNestedButtons");
				}
				else
				{
					mainListView = (ListView)rootPage.FindName("MainListView");
				}
				VERIFY_IS_NOT_NULL(mainListView);

				mainListView.Visibility = Visibility.Visible;
				mainListView.TabNavigation = tabNavigation;

				if (flags.HasFlag(VerifyTabOrderFlags.UseHeaderAndFooter))
				{
					((Button)mainListView.Header).Visibility = Visibility.Visible;

					// VirtualizingStackPanel does not support footer.
					if (!flags.HasFlag(VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel))
					{
						((Button)mainListView.Footer).Visibility = Visibility.Visible;
					}
				}

				if (flags.HasFlag(VerifyTabOrderFlags.UseGroupedItems))
				{
					cvs = (CollectionViewSource)((FrameworkElement)rootPage.Content).Resources["cvs"]; // UNO TODO: FindName doesn't see resources.
					VERIFY_IS_NOT_NULL(cvs);

					itemsSource = GetGroupedData(false /*getLargeDataSet*/);
					VERIFY_IS_NOT_NULL(itemsSource);

					cvs.Source = itemsSource;
					cvs.IsSourceGrouped = true;
				}
			});

			LOG_OUTPUT("Waiting for content to load.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Giving focus to the initial button.");
			await FocusTestHelper.EnsureFocus(beforeButton, FocusState.Keyboard);

			int tabCount = KeyboardNavigationMode.Once == tabNavigation ? 3 : 7;

			if (flags.HasFlag(VerifyTabOrderFlags.UseHeaderAndFooter))
			{
				tabCount++;

				if (!flags.HasFlag(VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel))
				{
					tabCount++;
				}
			}

			LOG_OUTPUT("Pressing tab %d times.", tabCount);
			for (int tabOccurrence = 0; tabOccurrence < tabCount; tabOccurrence++)
			{
				await KeyboardHelper.Tab();
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT("Pressing shift-tab %d times.", tabCount);
			for (int tabOccurrence = 0; tabOccurrence < tabCount; tabOccurrence++)
			{
				await KeyboardHelper.ShiftTab();
				await WindowHelper.WaitForIdle();
			}

			string logMessage = "Scenario: ItemsPanel=";

			if (flags.HasFlag(VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel))
			{
				logMessage += "VirtualizingStackPanel";
			}
			else if (flags.HasFlag(VerifyTabOrderFlags.UseStackPanelAsItemsPanel))
			{
				logMessage += "StackPanel";
			}
			else
			{
				logMessage += "ItemsWrapPanel";
			}

			if (flags.HasFlag(VerifyTabOrderFlags.UseHeaderAndFooter))
			{
				if (flags.HasFlag(VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel))
				{
					logMessage += ", with Header";
				}
				else
				{
					logMessage += ", with Header+Footer";
				}
			}

			if (flags.HasFlag(VerifyTabOrderFlags.UseGroupedItems))
			{
				logMessage += ", with groups";
			}

			LOG_OUTPUT(logMessage);
			LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
			LOG_OUTPUT("Actual focus sequence:   %s", focusSequence);
			VERIFY_ARE_EQUAL(expectedFocusSequence, focusSequence);
		}

		[TestMethod]
		public async Task VerifyFocusChangeAfterItemsReset()
		{
			await VerifyFocusChangeAfterListViewReset(false /*useItemsSource*/);
		}

		[TestMethod]
		public async Task VerifyFocusChangeAfterItemsSourceReset()
		{
			await VerifyFocusChangeAfterListViewReset(true /*useItemsSource*/);
		}

		[TestMethod]
		public async Task BringItemIntoViewWithLargeGlobalScale()
		{
			TestCleanupWrapper cleanup;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));
			await RunOnUIThread(() => WindowHelper.SetTestScaling(1.5f));

			try
			{
				var rootLoadedEvent = new Event();

				var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
				var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
				var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

				ScrollViewer scrollViewer = null;
				ListView outerListView = null;
				ListViewItem listViewItem = null;
				Page rootPage = await LoadXamlFileOnUIThread<Page>(NestedListViewBaseXaml);
				VERIFY_IS_NOT_NULL(rootPage);

				await RunOnUIThread(() =>
				{
					WindowHelper.WindowContent = rootPage;
					LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
					rootLoadedRegistration.Attach(
						rootPage,
						(s, e) =>
						{
							LOG_OUTPUT("Root FE Loaded handler.");
							rootLoadedEvent.Set();
						});

					outerListView = (ListView)rootPage.FindName("outerListView");
					VERIFY_IS_NOT_NULL(outerListView);
				});

				LOG_OUTPUT("Waiting for root FE Loaded event.");
				await rootLoadedEvent.WaitForDefault();
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(outerListView, 0);
					scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

					AttachScrollViewerLogging(viewChangingRegistration, viewChangedRegistration, scrollViewer);

					outerListView.Focus(FocusState.Keyboard);

					listViewItem = (ListViewItem)outerListView.ContainerFromIndex(0);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("Moving focus to first item.");
					listViewItem.Focus(FocusState.Keyboard);
				});

				await WindowHelper.WaitForIdle();

				LOG_OUTPUT("Tabbing 4 times to the 5th outer ListView item.");
				for (int i = 0; i < 4; i++)
				{
					await KeyboardHelper.Tab();
					await WindowHelper.WaitForIdle();

					await RunOnUIThread(() =>
					{
						LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
					});
				}

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
					VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 426.0);
					VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
					VERIFY_ARE_EQUAL(outerListView.SelectedIndex, -1);
				});
			}
			finally
			{
				await RunOnUIThread(() => WindowHelper.UnsetTestScaling());
			}
		}

		//
		// Private Methods
		//
		private async Task JumpItemIntoViewWithKeyboard(bool useRootCanvas)
		{
			TestCleanupWrapper cleanup;

			WindowHelper.SetWindowSizeOverride(new Size(400, 400));

			var rootLoadedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			ScrollViewer scrollViewer = null;
			ListView listView = null;
			ListViewItem listViewItem = null;
			Page rootPage = await LoadXamlFileOnUIThread<Page>(ListViewXaml);
			VERIFY_IS_NOT_NULL(rootPage);

			await RunOnUIThread(() =>
			{
				if (useRootCanvas)
				{
					LOG_OUTPUT("Inserting a Canvas as the root element.");
					rootPage.Width = 400.0;
					rootPage.Height = 400.0;

					var rootCanvas = new Canvas();
					rootCanvas.Width = 400.0;
					rootCanvas.Height = 400.0;
					rootCanvas.Children.Add(rootPage);

					WindowHelper.WindowContent = rootCanvas;

				}
				else
				{
					WindowHelper.WindowContent = rootPage;
				}

				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(
					rootPage,
					(s, e) =>
					{
						LOG_OUTPUT("Root FE Loaded handler.");
						rootLoadedEvent.Set();
					});

				listView = (ListView)rootPage.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);
			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				AttachScrollViewerLogging(viewChangingRegistration, viewChangedRegistration, scrollViewer);

				listViewItem = (ListViewItem)listView.ContainerFromIndex(0);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Moving focus to first item.");
				listViewItem.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving 3 pages down.");
			for (int i = 0; i < 3; i++)
			{
				await KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 784.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 25);
			});

			LOG_OUTPUT("Moving 3 pages up.");
			for (int i = 0; i < 3; i++)
			{
				await KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
				await WindowHelper.WaitForIdle();
				await WindowHelper.SynchronouslyTickUIThread(2);
			}

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 0);
			});

			LOG_OUTPUT("Moving to the end.");
			await KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 1952.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 52);
			});

			LOG_OUTPUT("Moving to the top.");
			await KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.SynchronouslyTickUIThread(2);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("ListView SelectedIndex is %d.", listView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(listView.SelectedIndex, 0);
			});
		}

		private async Task VerifyFocusChangeAfterListViewReset(bool useItemsSource)
		{
			TestCleanupWrapper cleanup;

			StackPanel stackPanel = null;
			Button beforeButton = null;
			Button afterButton = null;
			ListView listView = null;
			ObservableCollection<string> _observableVectorOfStrings = null;

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Building UI with ListView and Before/After buttons.");

				beforeButton = new Button();
				beforeButton.Content = "Before";

				afterButton = new Button();
				afterButton.Content = "After";

				listView = new ListView();

				if (useItemsSource)
				{
					_observableVectorOfStrings = new ObservableCollection<string>();
					_observableVectorOfStrings.Add("One");
					_observableVectorOfStrings.Add("Two");
					_observableVectorOfStrings.Add("Three");
					listView.ItemsSource = _observableVectorOfStrings;
				}
				else
				{
					for (int i = 0; i < 5; i++)
					{
						listView.Items.Add("Item " + i);
					}
				}

				stackPanel = new StackPanel();
				stackPanel.Children.Add(beforeButton);
				stackPanel.Children.Add(listView);
				stackPanel.Children.Add(afterButton);

				WindowHelper.WindowContent = stackPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Moving focus to Before button.");
				beforeButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				if (useItemsSource)
				{
					LOG_OUTPUT("Clearing ListView.ItemsSource collection.");
					_observableVectorOfStrings.Clear();
				}
				else
				{
					LOG_OUTPUT("Clearing ListView.Items collection.");
					listView.Items.Clear();
				}

				LOG_OUTPUT("Programmatically attempt to focus the next focusable element.");
				FindNextElementOptions options = new FindNextElementOptions();
				options.SearchRoot = WindowHelper.XamlRoot.Content;
				bool resultTryMoveFocus = FocusManager.TryMoveFocus(FocusNavigationDirection.Next, options);
				VERIFY_IS_TRUE(resultTryMoveFocus);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Verifying focus landed on After button.");
				var focusedElement = FocusManager.GetFocusedElement(WindowHelper.WindowContent.XamlRoot);
				VERIFY_IS_NOT_NULL(focusedElement);
				VERIFY_IS_TRUE(focusedElement.Equals(afterButton));
			});
		}

		private static async Task<T> LoadXamlFileOnUIThread<T>(string xaml) where T : class
		{
			T root = null;
			await RunOnUIThread(() => root = (T)XamlReader.Load(xaml));
			return root;
		}

		private static void ScrollIntoViewWithOptionalAnimation(ListView listView, object item, ScrollIntoViewAlignment alignment, bool disableAnimation)
		{
#if HAS_UNO
			listView.ScrollIntoViewWithOptionalAnimation(item, alignment, disableAnimation);
#else
			throw new NotSupportedException("ListViewBase.ScrollIntoViewWithOptionalAnimation is an internal WinUI API.");
#endif
		}

		// Logs ViewChanging/ViewChanged, like the WinUI test's inline handlers, and forwards to the optional callbacks.
		private static void AttachScrollViewerLogging(
			SafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>> viewChangingRegistration,
			SafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>> viewChangedRegistration,
			ScrollViewer scrollViewer,
			Action<ScrollViewerViewChangingEventArgs> onViewChanging = null,
			Action<ScrollViewerViewChangedEventArgs> onViewChanged = null)
		{
			viewChangingRegistration.Attach(scrollViewer, (sender, args) =>
			{
				LOG_OUTPUT("ViewChanging raised. NextView=(%f, %f, %f), FinalView=(%f, %f, %f), IsInertial=%d.",
					args.NextView.HorizontalOffset, args.NextView.VerticalOffset, args.NextView.ZoomFactor,
					args.FinalView.HorizontalOffset, args.FinalView.VerticalOffset, args.FinalView.ZoomFactor,
					args.IsInertial);
				onViewChanging?.Invoke(args);
			});

			viewChangedRegistration.Attach(scrollViewer, (sender, args) =>
			{
				LOG_OUTPUT("ViewChanged raised. View=(%f, %f, %f), IsIntermediate=%d.",
					scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor, args.IsIntermediate);
				onViewChanged?.Invoke(args);
			});
		}

		private static List<object> GetGroupedData(bool getLargeDataSet = true)
		{
			var groupedData = new List<object>();

			if (getLargeDataSet)
			{
				for (uint i = 0; i < 4; ++i)
				{
					GroupedHeader group = null;

					switch (i)
					{
						case 0:
							group = new GroupedHeader("Tennis");
							VERIFY_IS_NOT_NULL(group);

							group.Append("Roger Federer");
							group.Append("Rafael Nadal");
							group.Append("Novak Djokovic");
							group.Append("Andy Murray");
							group.Append("Grigor Dimitrov");
							break;

						case 1:
							group = new GroupedHeader("Soccer");
							VERIFY_IS_NOT_NULL(group);

							group.Append("Cristiano Ronaldo");
							group.Append("Lionel Messi");
							group.Append("Neymar");
							group.Append("Andres Iniesta");
							group.Append("Gareth Bale");
							group.Append("Xavi");
							group.Append("James Rodriguez");
							group.Append("Ronaldinho");
							group.Append("Arjen Robben");
							break;

						case 2:
							group = new GroupedHeader("Basketball");
							VERIFY_IS_NOT_NULL(group);

							group.Append("Allen Iverson");
							group.Append("Dwayne Wade");
							group.Append("LeBron James");
							group.Append("Kevin Durant");
							group.Append("Kobe Bryant");
							break;

						case 3:
							group = new GroupedHeader("Formula 1");
							VERIFY_IS_NOT_NULL(group);

							group.Append("Alain Prost");
							group.Append("Ayrton Senna");
							group.Append("Michael Schumacher");
							group.Append("Niki Lauda");
							group.Append("Sebastian Vettel");
							break;
					}

					groupedData.Add(group);
				}

				return groupedData;
			}

			GroupedHeader smallGroup = new GroupedHeader("1");
			smallGroup.Append("11");
			smallGroup.Append("12");
			groupedData.Add(smallGroup);

			smallGroup = new GroupedHeader("2");
			smallGroup.Append("21");
			smallGroup.Append("22");
			groupedData.Add(smallGroup);

			return groupedData;
		}
	}
}
