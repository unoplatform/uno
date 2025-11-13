using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Extensions;
using Uno.UI.Toolkit.DevTools.Input;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_RefreshContainer
	{
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Stretch_Child()
		{
			var grid = new Grid();
			var refreshContainer = new Microsoft.UI.Xaml.Controls.RefreshContainer();
			var child = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
			};
			var grandChild = new Border()
			{
				Background = new SolidColorBrush(Colors.Blue),
				Width = 10,
				Height = 10,
			};

			child.Child = grandChild;
			refreshContainer.Content = child;
			grid.Children.Add(refreshContainer);

			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grandChild);
			await WindowHelper.WaitForLoaded(refreshContainer);

			await WindowHelper.WaitForIdle();

			Assert.IsTrue(child.ActualWidth > 50);
			Assert.IsTrue(child.ActualHeight > 50);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Child_Empty_List()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var grid = new Grid() { Background = new SolidColorBrush(Colors.Red) };
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			var leftStrip = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			var rightStrip = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			var refreshContainer = new Microsoft.UI.Xaml.Controls.RefreshContainer();
			var listView = new ListView();
			refreshContainer.Content = listView;
			refreshContainer.RefreshRequested += OnRefreshRequested;
			grid.Children.Add(refreshContainer);
			grid.Children.Add(leftStrip);
			grid.Children.Add(rightStrip);
			Grid.SetColumnSpan(refreshContainer, 3);
			Grid.SetColumn(leftStrip, 0);
			Grid.SetColumn(rightStrip, 2);

			// The left and right strip are in place to ensure they cover
			// the sides of refresh container, where the refresh indicator
			// would potentially show up in case it was not positioned
			// correctly in the middle.

			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grid);

			await WindowHelper.WaitForIdle();

			var screenshotBefore = await TakeScreenshot(grid);

			await WindowHelper.WaitForIdle();
			var screenshotBeforeVerify = await TakeScreenshot(grid);
			await ImageAssert.AreEqualAsync(screenshotBefore, screenshotBeforeVerify);

			Deferral deferral = null;
			refreshContainer.RequestRefresh();

			await Task.Delay(200); // Artificial delay to allow the indicator to animate in
			var screenshotAfter = await TakeScreenshot(grid);
			await ImageAssert.AreNotEqualAsync(screenshotBefore, screenshotAfter);
			deferral.Complete();

			void OnRefreshRequested(
				Microsoft.UI.Xaml.Controls.RefreshContainer sender,
				Microsoft.UI.Xaml.Controls.RefreshRequestedEventArgs args)
			{
				deferral = args.GetDeferral(); // Keep refreshing
			}
		}

#if HAS_UNO
		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ListViewNestedAndSwipeUpAndDown_Then_ScrollList()
		{
			var sut = new RefreshContainer
			{
				Width = 100,
				Height = 300,
				Content = new ListView { ItemsSource = Enumerable.Range(0, 50).Select(i => new Border { Height = 50, Width = 100, Background = new SolidColorBrush(i % 2 is 0 ? Colors.Chartreuse : Colors.DeepPink) }) }
			};

			var rect = await UITestHelper.Load(sut);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			ImageAssert.HasPixels(await UITestHelper.ScreenShot(sut), ExpectedPixels.At(50, 1).Pixel(Colors.Chartreuse));

			// Make sure to abort any pending direct manip
			finger.Tap(rect.Location.Offset(-1, -1));

			// Slowly swipe up (scroll down - no inertia)
			finger.Drag(
				from: rect.GetCenter(),
				to: new(rect.GetCenter().X, rect.GetCenter().Y - 50),
				steps: 5,
				stepOffsetInMilliseconds: 100);

			await UITestHelper.WaitForIdle();

			ImageAssert.HasPixels(await UITestHelper.ScreenShot(sut), ExpectedPixels.At(50, 1).Pixel(Colors.DeepPink));

			// Slowly swipe down (scroll up - no inertia)
			finger.Drag(
				from: rect.GetCenter(),
				to: new(rect.GetCenter().X, rect.GetCenter().Y + 50),
				steps: 5,
				stepOffsetInMilliseconds: 100);

			await UITestHelper.WaitForIdle();

			ImageAssert.HasPixels(await UITestHelper.ScreenShot(sut), ExpectedPixels.At(50, 1).Pixel(Colors.Chartreuse));
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ListViewNestedAndSwipeDown_Then_RefreshRequested()
		{
			var sut = new RefreshContainer
			{
				Width = 100,
				Height = 300,
				Content = new ListView { ItemsSource = Enumerable.Range(0, 50).Select(i => new Border { Height = 50, Width = 100, Background = new SolidColorBrush(i % 2 is 0 ? Colors.Chartreuse : Colors.DeepPink) }) }
			};

			var requested = 0;
			sut.RefreshRequested += (snd, e) =>
			{
				requested++;
				e.GetDeferral().Complete();
			};

			var rect = await UITestHelper.Load(sut);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			// Make sure to abort any pending direct manip
			finger.Tap(rect.Location.Offset(-1, -1));

			// Slowly swipe down (scroll up - no inertia)
			finger.Drag(
				from: rect.GetCenter(),
				to: new(rect.GetCenter().X, rect.GetCenter().Y + 100),
				steps: 5,
				stepOffsetInMilliseconds: 100);

			await UITestHelper.WaitForIdle();

			Assert.AreEqual(1, requested);
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ListViewNestedAndFlickDown_Then_RefreshRequested()
		{
			var sut = new RefreshContainer
			{
				Width = 100,
				Height = 300,
				Content = new ListView { ItemsSource = Enumerable.Range(0, 50).Select(i => new Border { Height = 50, Width = 100, Background = new SolidColorBrush(i % 2 is 0 ? Colors.Chartreuse : Colors.DeepPink) }) }
			};

			var requested = 0;
			sut.RefreshRequested += (snd, e) =>
			{
				requested++;
				e.GetDeferral().Complete();
			};

			var rect = await UITestHelper.Load(sut);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			// Make sure to abort any pending direct manip
			finger.Tap(rect.Location.Offset(-1, -1));

			// Fast swipe down (scroll up - with inertia)
			finger.Drag(
				from: rect.GetCenter(),
				to: new(rect.GetCenter().X, rect.GetCenter().Y + 200),
				steps: 1,
				stepOffsetInMilliseconds: 1);

			await UITestHelper.WaitForIdle();

			Assert.AreEqual(1, requested);
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#else
		[Ignore("This test is flaky on CI, see #9080")]
#endif
		public async Task When_ListViewNestedAndSwipeUpThenFlickDown_Then_ScrollThenShowIndicatorButDoNotRequest()
		{
			var sut = new RefreshContainer
			{
				Width = 100,
				Height = 300,
				Content = new ListView
				{
					SelectionMode = ListViewSelectionMode.None,
					ItemsSource = Enumerable
						.Range(0, 50)
						.Select(i => new Border
						{
							Height = 50,
							Width = 100,
							Background = new SolidColorBrush(i % 2 is 0 ? Colors.Chartreuse : Colors.DeepPink),
							Child = new TextBlock { Text = i.ToString() },
						})
				}
			};

			var requested = 0;
			sut.RefreshRequested += (snd, e) =>
			{
				requested++;
				e.GetDeferral().Complete();
			};

			var rect = await UITestHelper.Load(sut);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			// Make sure to abort any pending direct manip
			finger.Tap(rect.Location.Offset(-1, -1));

			// Slowly swipe up (scroll down - no inertia)
			finger.Drag(
				from: rect.GetCenter(),
				to: new(rect.GetCenter().X, rect.GetCenter().Y - 95),
				steps: 5,
				stepOffsetInMilliseconds: 300);

			await UITestHelper.WaitForIdle();
			var intermediate = await UITestHelper.ScreenShot(sut);
			var intermediatePixel = intermediate[50, 1];
			Assert.IsTrue(intermediatePixel == Colors.DeepPink); // Check for the refresh container style

			// Fast swipe down (scroll up - with inertia)
			finger.Drag(
				from: rect.GetCenter(),
				to: new(rect.GetCenter().X, rect.GetCenter().Y + 600),
				steps: 1,
				stepOffsetInMilliseconds: 1);

			await UITestHelper.WaitForIdle();

			var result = await UITestHelper.ScreenShot(sut);
			var pixel = result[50, 1];
			Assert.IsTrue(pixel != Colors.Chartreuse && pixel != Colors.DeepPink); // Check for the refresh container style
			Assert.AreEqual(0, requested);
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ListViewNestedAndTapItem_Then_ItemSelected()
		{
			ListView lv;
			var sut = new RefreshContainer
			{
				Width = 100,
				Height = 300,
				Content = lv = new ListView { ItemsSource = Enumerable.Range(0, 50).Select(i => new Border { Height = 50, Width = 100, Background = new SolidColorBrush(i % 2 is 0 ? Colors.Chartreuse : Colors.DeepPink) }) }
			};

			var rect = await UITestHelper.Load(sut);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			// Make sure to abort any pending direct manip
			finger.Tap(rect.Location.Offset(-1, -1));

			Assert.AreEqual(-1, lv.SelectedIndex);

			// Tap
			finger.Press(rect.GetCenter());
			finger.Release(rect.GetCenter());

			await UITestHelper.WaitForIdle();

			Assert.AreNotEqual(-1, lv.SelectedIndex);
		}
#endif

		private Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
			=> UITestHelper.ScreenShot(SUT);
	}
}
