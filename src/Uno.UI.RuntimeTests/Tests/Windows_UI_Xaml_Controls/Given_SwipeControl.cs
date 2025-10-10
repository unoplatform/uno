using System;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.Extensions;
using Uno.Testing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_SwipeControl : SampleControlUITestBase
{
	[TestMethod]
	[CombinatorialData]
	[RunsOnUIThread]
	[InjectedPointer(Windows.Devices.Input.PointerDeviceType.Touch)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // Requires pointer injection
	public async Task When_Reveal_Mode(EdgeTransitionLocation itemsLocation)
	{
		// Create a SwipeControl with SwipeItems in Reveal mode, based on the paramter
		var swipeControl = new SwipeControl();
		swipeControl.Content = "Hi";
		swipeControl.Width = 200;
		swipeControl.Height = 200;
		swipeControl.Background = new SolidColorBrush(Colors.White);

		var swipeItems = new SwipeItems
		{
			Mode = SwipeMode.Reveal
		};
		var starIcon = new FontIconSource() { Glyph = "\uE735" };
		swipeItems.Add(new SwipeItem { Text = "Item1", Background = new SolidColorBrush(Colors.Red), Foreground = new SolidColorBrush(Colors.Green), IconSource = starIcon });
		swipeItems.Add(new SwipeItem { Text = "Item2", Background = new SolidColorBrush(Colors.Blue), Foreground = new SolidColorBrush(Colors.Pink), IconSource = starIcon });
		switch (itemsLocation)
		{
			case EdgeTransitionLocation.Left:
				swipeControl.LeftItems = swipeItems;
				break;
			case EdgeTransitionLocation.Top:
				swipeControl.TopItems = swipeItems;
				break;
			case EdgeTransitionLocation.Right:
				swipeControl.RightItems = swipeItems;
				break;
			case EdgeTransitionLocation.Bottom:
				swipeControl.BottomItems = swipeItems;
				break;
		}
		TestServices.WindowHelper.WindowContent = swipeControl;
		await TestServices.WindowHelper.WaitForLoaded(swipeControl);

		var screenshotBefore = await UITestHelper.ScreenShot(swipeControl);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var finger = injector.GetFinger();

		var rect = swipeControl.GetAbsoluteBoundsRect();
		finger.Tap(rect.Location.Offset(-1, -1)); // Cancel any input

		var targetCoords = itemsLocation switch
		{
			EdgeTransitionLocation.Left => new Point((float)rect.Right - 1, (float)rect.GetCenter().Y),
			EdgeTransitionLocation.Top => new Point((float)rect.GetCenter().X, (float)rect.Bottom - 1),
			EdgeTransitionLocation.Right => new Point((float)rect.X + 1, (float)rect.GetCenter().Y),
			EdgeTransitionLocation.Bottom => new Point((float)rect.GetCenter().X, (float)rect.Y + 1),
			_ => throw new NotSupportedException("Unknown EdgeTransitionLocation"),
		};

		finger.Drag(
			from: rect.GetCenter(),
			to: targetCoords,
			steps: 5,
			stepOffsetInMilliseconds: 100);

		finger.Release();

		await UITestHelper.WaitForIdle();

		await Task.Delay(500); // Allow time for the animation to complete

		var screenshotAfter = await UITestHelper.ScreenShot(swipeControl);

		var swipeStackPanelChild = (StackPanel)VisualTreeUtils.FindVisualChildByName(swipeControl, "SwipeContentStackPanel");

		var itemNonStretchDimension = itemsLocation is EdgeTransitionLocation.Left or EdgeTransitionLocation.Right
			? swipeStackPanelChild.ActualWidth / 2
			: swipeStackPanelChild.ActualHeight / 2;

		Assert.IsGreaterThan(0, itemNonStretchDimension);

		var firstItemRect = itemsLocation switch
		{
			EdgeTransitionLocation.Left => new Rect(0, 0, itemNonStretchDimension, swipeControl.ActualHeight),
			EdgeTransitionLocation.Top => new Rect(0, 0, swipeControl.ActualWidth, itemNonStretchDimension),
			EdgeTransitionLocation.Right => new Rect(swipeControl.ActualWidth - 2 * itemNonStretchDimension, 0, itemNonStretchDimension, swipeControl.ActualHeight),
			EdgeTransitionLocation.Bottom => new Rect(0, swipeControl.ActualHeight - 2 * itemNonStretchDimension, swipeControl.ActualWidth, itemNonStretchDimension),
			_ => throw new NotSupportedException("Unknown EdgeTransitionLocation"),
		};

		var secondItemRect = itemsLocation switch
		{
			EdgeTransitionLocation.Left => new Rect(itemNonStretchDimension, 0, itemNonStretchDimension, swipeControl.ActualHeight),
			EdgeTransitionLocation.Top => new Rect(0, itemNonStretchDimension, swipeControl.ActualWidth, itemNonStretchDimension),
			EdgeTransitionLocation.Right => new Rect(swipeControl.ActualWidth - itemNonStretchDimension, 0, itemNonStretchDimension, swipeControl.ActualHeight),
			EdgeTransitionLocation.Bottom => new Rect(0, swipeControl.ActualHeight - itemNonStretchDimension, swipeControl.ActualWidth, itemNonStretchDimension),
			_ => throw new NotSupportedException("Unknown EdgeTransitionLocation"),
		};

		ImageAssert.HasColorAt(screenshotAfter, new(firstItemRect.X + 4, firstItemRect.Y + 4), Colors.Red);
		ImageAssert.HasColorAt(screenshotAfter, new(secondItemRect.X + 4, secondItemRect.Y + 4), Colors.Blue);

		var drawingRectangleFirst = new System.Drawing.Rectangle((int)firstItemRect.X, (int)firstItemRect.Y, (int)firstItemRect.Width, (int)firstItemRect.Height);
		var drawingRectangleSecond = new System.Drawing.Rectangle((int)secondItemRect.X, (int)secondItemRect.Y, (int)secondItemRect.Width, (int)secondItemRect.Height);
		ImageAssert.HasColorInRectangle(screenshotAfter, drawingRectangleFirst, Colors.Green);
		ImageAssert.HasColorInRectangle(screenshotAfter, drawingRectangleSecond, Colors.Pink);
	}
}
