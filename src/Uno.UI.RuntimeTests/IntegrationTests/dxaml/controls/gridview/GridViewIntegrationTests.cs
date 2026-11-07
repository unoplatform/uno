// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.MUX.Helpers;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Enterprise;

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class GridViewIntegrationTests : BaseDxamlTestClass
{
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
	public void TestCleanup()
	{
		WindowHelper.VerifyTestCleanup();
	}

	// WinUI's WaitForIdle also waits for DirectManipulation, which runs the animated view changes.
	private static async Task WaitForScrollViewerIdle(ScrollViewer scrollViewer)
	{
		await WindowHelper.WaitFor(() => !scrollViewer.IsInManipulation);
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task AnimateItemIntoViewWithGamepad()
	{
		WindowHelper.SetWindowSizeOverride(new global::Windows.Foundation.Size(400, 400));

		InputDevice device = InputDevice.Gamepad;

		var rootLoadedEvent = new Event();

		var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
		var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
		var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

		ScrollViewer scrollViewer = null;
		GridView gridView = null;
		GridViewItem gridViewItem = null;
		Grid rootGrid = null;

		await RunOnUIThread(() =>
		{
			rootGrid = (Grid)XamlReader.Load(HeaderedGridViewXaml);
			VERIFY_IS_NOT_NULL(rootGrid);

			WindowHelper.WindowContent = rootGrid;
			LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
			rootLoadedRegistration.Attach(rootGrid, (s, e) =>
			{
				LOG_OUTPUT("Root FE Loaded handler.");
				rootLoadedEvent.Set();
			});

			gridView = (GridView)rootGrid.FindName("gridView");
			VERIFY_IS_NOT_NULL(gridView);
		});

		LOG_OUTPUT("Waiting for root FE Loaded event.");
		await rootLoadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(gridView, 0);
			scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

			viewChangingRegistration.Attach(scrollViewer, (sender, args) =>
			{
				LogViewChanging(args);
			});

			viewChangedRegistration.Attach(scrollViewer, (sender, args) =>
			{
				LogViewChanged(scrollViewer, args);
			});

			gridView.Focus(FocusState.Keyboard);

			gridViewItem = (GridViewItem)gridView.ContainerFromIndex(0);
		});

		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Moving focus to first item.");
			gridViewItem.Focus(FocusState.Keyboard);
		});

		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Moving 3 items right, to the 7th item, with the gamepad.");
		for (int i = 0; i < 3; i++)
		{
			await CommonInputHelper.Right(device);
			await WindowHelper.WaitForIdle();
			await WindowHelper.SynchronouslyTickUIThread(2);
		}

		await WindowHelper.WaitForIdle();
		await WaitForScrollViewerIdle(scrollViewer);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
			LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
			VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 268.0);
			VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
			VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
			VERIFY_ARE_EQUAL(gridView.SelectedIndex, 6);
		});

		LOG_OUTPUT("Moving 3 times left, to the 1st item, with the gamepad.");
		for (int i = 0; i < 3; i++)
		{
			await CommonInputHelper.Left(device);
			await WindowHelper.WaitForIdle();
			await WindowHelper.SynchronouslyTickUIThread(2);
		}

		await WindowHelper.WaitForIdle();
		await WaitForScrollViewerIdle(scrollViewer);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
			LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
			VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 116.0);
			VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
			VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
			VERIFY_ARE_EQUAL(gridView.SelectedIndex, 0);
		});
	}

	// Navigates through items, header and footer, nested in ScrollViewer, with the gamepad left and right buttons.
	[TestMethod]
	public async Task AnimateGridViewInScrollViewerWithGamepad()
	{
		//KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind::WaitForIdleBeforeAndAfter);

		InputDevice device = InputDevice.Gamepad;

		WindowHelper.SetWindowSizeOverride(new global::Windows.Foundation.Size(400, 400));

		var rootLoadedEvent = new Event();
		var viewChangedEvent = new Event();

		var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
		var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
		var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

		ScrollViewer scrollViewer = null;
		GridView gridView = null;
		GridViewItem gridViewItem = null;
		Grid rootGrid = null;

		await RunOnUIThread(() =>
		{
			rootGrid = (Grid)XamlReader.Load(GridViewInScrollViewerXaml);
			VERIFY_IS_NOT_NULL(rootGrid);

			WindowHelper.WindowContent = rootGrid;
			LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
			rootLoadedRegistration.Attach(rootGrid, (s, e) =>
			{
				LOG_OUTPUT("Root FE Loaded handler.");
				rootLoadedEvent.Set();
			});

			scrollViewer = (ScrollViewer)rootGrid.FindName("scrollViewer");
			VERIFY_IS_NOT_NULL(scrollViewer);

			gridView = (GridView)rootGrid.FindName("gridView");
			VERIFY_IS_NOT_NULL(gridView);

			viewChangingRegistration.Attach(scrollViewer, (sender, args) =>
			{
				LogViewChanging(args);
			});

			viewChangedRegistration.Attach(scrollViewer, (sender, args) =>
			{
				LogViewChanged(scrollViewer, args);
				if (!args.IsIntermediate)
				{
					viewChangedEvent.Set();
				}
			});
		});

		LOG_OUTPUT("Waiting for root FE Loaded event.");
		await rootLoadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			gridView.Focus(FocusState.Keyboard);
			gridViewItem = (GridViewItem)gridView.ContainerFromIndex(0);
		});

		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Moving focus to first item.");
			gridViewItem.Focus(FocusState.Keyboard);
		});

		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Moving to last column fast and into the right footer.");
		for (int i = 0; i < 8; i++)
		{
			viewChangedEvent.Reset();
			await CommonInputHelper.Right(device);
		}

		await viewChangedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
			LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
			VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 2252.0);
			VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
			VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
			VERIFY_ARE_EQUAL(gridView.SelectedIndex, 14);
		});

		LOG_OUTPUT("Moving to first column fast and into left header.");
		for (int i = 0; i < 8; i++)
		{
			viewChangedEvent.Reset();
			await CommonInputHelper.Left(device);
		}

		await viewChangedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
			LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
			VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
			VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
			VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
			VERIFY_ARE_EQUAL(gridView.SelectedIndex, 0);
		});
	}

	[TestMethod]
	public async Task ValidateNavigationDoesNotHorizontallyWrapWithGamepad()
	{
		await ValidateNavigationDoesNotWrapWithGamepad(true /* testHorizontalWrapping */);
	}

	[TestMethod]
	public async Task ValidateNavigationDoesNotVerticallyWrapWithGamepad()
	{
		await ValidateNavigationDoesNotWrapWithGamepad(false /* testHorizontalWrapping */);
	}

	private async Task ValidateNavigationDoesNotWrapWithGamepad(bool testHorizontalWrapping)
	{
		InputDevice device = InputDevice.Gamepad;

		WindowHelper.SetWindowSizeOverride(new global::Windows.Foundation.Size(400, 400));

		var rootLoadedEvent = new Event();

		var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");

		GridView gridView = null;
		GridViewItem gridViewItem = null;
		Page rootPage = null;

		var selectionChangedEvent = new Event();
		var selectionChangedRegistration = CreateSafeEventRegistration<GridView, SelectionChangedEventHandler>("SelectionChanged");

		await RunOnUIThread(() =>
		{
			rootPage = (Page)XamlReader.Load(testHorizontalWrapping ? VerticalGridViewXaml : GridViewXaml);
			VERIFY_IS_NOT_NULL(rootPage);

			WindowHelper.WindowContent = rootPage;

			rootLoadedRegistration.Attach(rootPage, (s, e) =>
			{
				rootLoadedEvent.Set();
			});

			gridView = (GridView)rootPage.FindName("gridView");

			selectionChangedRegistration.Attach(gridView, (s, e) =>
			{
				LOG_OUTPUT("SelectedIndex changed to %d.", gridView.SelectedIndex);

				selectionChangedEvent.Set();
			});
		});

		await rootLoadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			gridViewItem = (GridViewItem)gridView.ContainerFromIndex(0);
			gridViewItem.Focus(FocusState.Keyboard);
		});

		await WindowHelper.WaitForIdle();

		if (testHorizontalWrapping)
		{
			LOG_OUTPUT("Move right three times. We should end up on the third item, not the fourth, as we shouldn't be wrapping.");
			await CommonInputHelper.Right(device);
			await selectionChangedEvent.WaitForDefault();
			await CommonInputHelper.Right(device);
			await selectionChangedEvent.WaitForDefault();
			selectionChangedEvent.Reset();
			await CommonInputHelper.Right(device);

		}
		else
		{
			LOG_OUTPUT("Move down twice. We should end up on the second item, not the third, as we shouldn't be wrapping.");
			await CommonInputHelper.Down(device);
			await selectionChangedEvent.WaitForDefault();
			selectionChangedEvent.Reset();
			await CommonInputHelper.Down(device);
		}

		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(gridView.SelectedIndex, testHorizontalWrapping ? 2 : 1);
			VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
		});

		if (testHorizontalWrapping)
		{
			LOG_OUTPUT("Move down once and then left three times. We should end up on the fourth item, not the third, as we shouldn't be wrapping.");
			await CommonInputHelper.Down(device);
			await selectionChangedEvent.WaitForDefault();
			await CommonInputHelper.Left(device);
			await selectionChangedEvent.WaitForDefault();
			await CommonInputHelper.Left(device);
			await selectionChangedEvent.WaitForDefault();
			selectionChangedEvent.Reset();
			await CommonInputHelper.Left(device);
		}
		else
		{
			LOG_OUTPUT("Move right once and then up twice. We should end up on the third item, not the second, as we shouldn't be wrapping.");
			await CommonInputHelper.Right(device);
			await selectionChangedEvent.WaitForDefault();
			await CommonInputHelper.Up(device);
			await selectionChangedEvent.WaitForDefault();
			selectionChangedEvent.Reset();
			await CommonInputHelper.Up(device);
		}

		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(gridView.SelectedIndex, testHorizontalWrapping ? 3 : 2);
			VERIFY_IS_FALSE(selectionChangedEvent.HasFired());
		});
	}

	[TestMethod]
	[Ignore("Needs TestServices.Utilities.ShrinkApplicationViewVisibleBounds to shrink ApplicationView.VisibleBounds, which has no Uno equivalent; the expected scroll offsets (296, 14) depend on it.")]
	public async Task BringItemIntoShrunkViewWithGamepad()
	{
		try
		{
			WindowHelper.SetWindowSizeOverride(new global::Windows.Foundation.Size(350, 400));

			//TestServices::Utilities->ShrinkApplicationViewVisibleBounds(true);

			var rootLoadedEvent = new Event();

			var rootLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var viewChangingRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangingEventArgs>>("ViewChanging");
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			ScrollViewer scrollViewer = null;
			GridView gridView = null;
			GridViewItem gridViewItem = null;
			Page rootPage = null;

			await RunOnUIThread(() =>
			{
				rootPage = (Page)XamlReader.Load(GridViewXaml);
				VERIFY_IS_NOT_NULL(rootPage);

				WindowHelper.WindowContent = rootPage;
				LOG_OUTPUT("Listening to root FrameworkElement.Loaded.");
				rootLoadedRegistration.Attach(rootPage, (s, e) =>
				{
					LOG_OUTPUT("Root FE Loaded handler.");
					rootLoadedEvent.Set();
				});

				gridView = (GridView)rootPage.FindName("gridView");
				VERIFY_IS_NOT_NULL(gridView);

			});

			LOG_OUTPUT("Waiting for root FE Loaded event.");
			await rootLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var controlTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(gridView, 0);
				scrollViewer = (ScrollViewer)controlTemplateRoot.FindName("ScrollViewer");

				viewChangingRegistration.Attach(scrollViewer, (sender, args) =>
				{
					LogViewChanging(args);
				});

				viewChangedRegistration.Attach(scrollViewer, (sender, args) =>
				{
					LogViewChanged(scrollViewer, args);
				});

				gridView.Focus(FocusState.Keyboard);

				gridViewItem = (GridViewItem)gridView.ContainerFromIndex(0);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Moving focus to first item.");
				gridViewItem.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving 4 items right, to the 9th item, with the gamepad.");
			for (int i = 0; i < 4; i++)
			{
				await KeyboardHelper.GamepadDpadRight();
				await WindowHelper.WaitForIdle();
			}

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 296.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(gridView.SelectedIndex, 8);
			});

			LOG_OUTPUT("Moving 3 times left, to the 3rd item, with the gamepad.");
			for (int i = 0; i < 3; i++)
			{
				await KeyboardHelper.GamepadDpadLeft();
				await WindowHelper.WaitForIdle();
			}

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 14.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(gridView.SelectedIndex, 2);
			});

			LOG_OUTPUT("Moving once to the left, to the 1st item, with the gamepad.");
			await KeyboardHelper.GamepadDpadLeft();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("ScrollViewer view is (x, y, z) = (%f, %f, %f).", scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ZoomFactor);
				LOG_OUTPUT("GridView SelectedIndex is %d.", gridView.SelectedIndex);
				VERIFY_ARE_EQUAL(scrollViewer.HorizontalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0.0);
				VERIFY_ARE_EQUAL(scrollViewer.ZoomFactor, 1.0f);
				VERIFY_ARE_EQUAL(gridView.SelectedIndex, 0);
			});
		}
		finally
		{
			WindowHelper.ResetWindowContentAndWaitForIdle();
			//TestServices::Utilities->ShrinkApplicationViewVisibleBounds(false);
		}
	}

	//
	// Private Methods
	//
	private static void LogViewChanging(ScrollViewerViewChangingEventArgs args)
		=> LOG_OUTPUT($"ViewChanging raised. NextView=({args.NextView.HorizontalOffset:F3}, {args.NextView.VerticalOffset:F3}, {args.NextView.ZoomFactor:F3}), FinalView=({args.FinalView.HorizontalOffset:F3}, {args.FinalView.VerticalOffset:F3}, {args.FinalView.ZoomFactor:F3}), IsInertial={args.IsInertial}.");

	private static void LogViewChanged(ScrollViewer scrollViewer, ScrollViewerViewChangedEventArgs args)
		=> LOG_OUTPUT($"ViewChanged raised. View=({scrollViewer.HorizontalOffset:F3}, {scrollViewer.VerticalOffset:F3}, {scrollViewer.ZoomFactor:F3}), IsIntermediate={args.IsIntermediate}.");

	// Rectangle colors cycle Blue, Green, Red, Yellow.
	private static string Rectangles(int count, int width, int height)
	{
		string[] colors = { "Blue", "Green", "Red", "Yellow" };

		return string.Join(
			"\n",
			Enumerable.Range(0, count).Select(i => $"<Rectangle Fill=\"{colors[i % colors.Length]}\" Width=\"{width}\" Height=\"{height}\"/>"));
	}

	private static string HeaderedGridViewXaml
		=> $$"""
		<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <Grid.Resources>
		    <Style TargetType="TextBox">
		      <Setter Property="Padding" Value="10,3,6,5"/>
		    </Style>
		  </Grid.Resources>
		  <GridView
		    x:Name="gridView"
		    Background="Magenta"
		    Height="380" Width="380"
		    Margin="10"
		    ScrollViewer.HorizontalScrollBarVisibility="Auto"
		    ScrollViewer.VerticalScrollBarVisibility="Disabled"
		    ScrollViewer.HorizontalScrollMode="Enabled"
		    ScrollViewer.IsHorizontalRailEnabled="True"
		    ScrollViewer.VerticalScrollMode="Disabled"
		    ScrollViewer.IsVerticalRailEnabled="False">
		    <GridView.ItemsPanel>
		      <ItemsPanelTemplate>
		        <ItemsWrapGrid Orientation="Vertical" />
		      </ItemsPanelTemplate>
		    </GridView.ItemsPanel>
		    <GridView.Header>
		      <!-- UNO TODO: Width pins the text-measured width of WinUI's Segoe UI, which the expected offsets depend on. -->
		      <StackPanel
		        Margin="10"
		        Width="116"
		        Background="Cyan"
		        VerticalAlignment="Center">
		        <TextBlock FontSize="15" Text="Header Top"/>
		        <TextBox FontSize="15" Text="Header Middle"/>
		        <TextBlock FontSize="15" Text="Header Bottom"/>
		      </StackPanel>
		    </GridView.Header>
		    <GridView.Footer>
		      <StackPanel
		        Margin="10"
		        Background="Violet"
		        VerticalAlignment="Center">
		        <TextBlock FontSize="15" Text="Footer Top"/>
		        <TextBox FontSize="15" Text="Footer Middle"/>
		        <TextBlock FontSize="15" Text="Footer Bottom"/>
		      </StackPanel>
		    </GridView.Footer>
		    {{Rectangles(56, 120, 120)}}
		  </GridView>
		</Grid>
		""";

	private static string GridViewInScrollViewerXaml
		=> $$"""
		<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <ScrollViewer x:Name="scrollViewer"
		      HorizontalScrollBarVisibility="Auto"
		      VerticalScrollBarVisibility="Disabled"
		      HorizontalScrollMode="Enabled"
		      VerticalScrollMode="Disabled"
		      Width="380"
		      Height="380"
		      Margin="10">
		    <StackPanel Orientation="Horizontal">
		      <Rectangle Fill="Cyan" Width="100" Height="100" VerticalAlignment="Center"/>
		      <GridView
		        x:Name="gridView"
		        Background="Magenta"
		        Height="300"
		        VerticalAlignment="Center"
		        ScrollViewer.HorizontalScrollBarVisibility="Auto"
		        ScrollViewer.VerticalScrollBarVisibility="Disabled"
		        ScrollViewer.HorizontalScrollMode="Enabled"
		        ScrollViewer.IsHorizontalRailEnabled="True"
		        ScrollViewer.VerticalScrollMode="Disabled"
		        ScrollViewer.IsVerticalRailEnabled="False">
		        <GridView.ItemsPanel>
		          <ItemsPanelTemplate>
		            <ItemsWrapGrid Orientation="Vertical"/>
		          </ItemsPanelTemplate>
		        </GridView.ItemsPanel>
		        {{Rectangles(16, 300, 120)}}
		      </GridView>
		      <Rectangle Fill="Violet" Width="100" Height="100" VerticalAlignment="Center"/>
		    </StackPanel>
		  </ScrollViewer>
		</Grid>
		""";

	private static string GridViewXaml => WrapGridViewPageXaml("Vertical");

	private static string VerticalGridViewXaml => WrapGridViewPageXaml("Horizontal");

	// GridView.xaml uses a vertical ItemsWrapGrid, VerticalGridView.xaml a horizontal one.
	private static string WrapGridViewPageXaml(string orientation)
		=> $$"""
		<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <GridView
		    x:Name="gridView"
		    Background="Magenta"
		    Height="380" Width="380"
		    Margin="10"
		    ScrollViewer.HorizontalScrollBarVisibility="Auto"
		    ScrollViewer.VerticalScrollBarVisibility="Disabled"
		    ScrollViewer.HorizontalScrollMode="Enabled"
		    ScrollViewer.IsHorizontalRailEnabled="True"
		    ScrollViewer.VerticalScrollMode="Disabled"
		    ScrollViewer.IsVerticalRailEnabled="False">
		    <GridView.ItemsPanel>
		      <ItemsPanelTemplate>
		        <ItemsWrapGrid Orientation="{{orientation}}" />
		      </ItemsPanelTemplate>
		    </GridView.ItemsPanel>
		    {{Rectangles(56, 120, 120)}}
		  </GridView>
		</Page>
		""";
}
