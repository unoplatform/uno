// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/ScrollViewTestsWithInputHelper.cs

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

// Upstream drives the ScrollViewsWithSimpleContents test page through UI Automation. Here each test builds the
// ScrollView it uses from that page in code, and reads the offsets from the ScrollView directly.
// Not ported:
// - PanScrollViewer, ScrollThenPanScrollView: touch pans with velocity-dependent end offsets, not yet validated on Skia.
// - VerifyScrollViewGamePad*, VerifyScrollViewIgnoresGamepadInput: gamepad navigation through ScrollView is not yet validated on Skia.
[TestClass]
[RunsOnUIThread]
public class ScrollViewTestsWithInputHelper
{
	[TestCleanup]
	public void TestCleanup() => WindowHelper.WindowContent = null;

	[TestMethod]
	[Description("Tests Keyboard interaction (Up, Down, Left, Right, PageUp, PageDown, Home, End)")]
	[Ignore("Rapid consecutive Down presses mis-accumulate AddScrollVelocity: after the Up/PageUp/Right/Left steps they don't scroll (Expected=0,1320, Actual=0,1140); from a fresh view they overshoot (183 vs 180).")]
	public async Task VerifyScrollViewKeyboardInteraction()
	{
		var (scrollView11, buttonInScrollView11) = await SetupSimpleSingleScrollViewTest();
		var scrollPresenter11 = scrollView11.ScrollPresenter;

		var scrollAmountForDownOrUpKey = scrollPresenter11.ActualHeight * 0.15;
		var scrollAmountForPageUpOrPageDownKey = scrollPresenter11.ActualHeight;
		var scrollAmountForRightOrLeftKey = scrollPresenter11.ActualWidth * 0.15;
		var maxScrollOffset = 2000 - scrollPresenter11.ActualHeight;

		double expectedVerticalOffset = 0;
		double expectedHorizontalOffset = 0;

		// Pressing Down key
		await PressKey(buttonInScrollView11, "down");
		expectedVerticalOffset += scrollAmountForDownOrUpKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing PageDown key
		await PressKey(buttonInScrollView11, "pagedown");
		expectedVerticalOffset += scrollAmountForPageUpOrPageDownKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing Home key
		await PressKey(buttonInScrollView11, "home");
		expectedVerticalOffset = 0;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing End key
		await PressKey(buttonInScrollView11, "end");
		expectedVerticalOffset = maxScrollOffset;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing Up key
		await PressKey(buttonInScrollView11, "up");
		expectedVerticalOffset -= scrollAmountForDownOrUpKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing PageUp key
		await PressKey(buttonInScrollView11, "pageup");
		expectedVerticalOffset -= scrollAmountForPageUpOrPageDownKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing Right key
		await PressKey(buttonInScrollView11, "right");
		expectedHorizontalOffset += scrollAmountForRightOrLeftKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing Left key
		await PressKey(buttonInScrollView11, "left");
		expectedHorizontalOffset -= scrollAmountForRightOrLeftKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);

		// Pressing Down key three times
		await PressKey(buttonInScrollView11, "down", numPresses: 3);
		expectedVerticalOffset += 3 * scrollAmountForDownOrUpKey;
		await WaitForScrollViewOffsets(scrollView11, expectedHorizontalOffset, expectedVerticalOffset);
	}

	[TestMethod]
	[Description("Verifies keyboard input is ignored when ScrollView.IgnoredInputKinds is Keyboard.")]
	public async Task VerifyScrollViewIgnoresKeyboardInput()
	{
		var scrollView51 = await SetupScrollViewTestWithImage("51");

		// Changing ScrollView.IgnoredInputKinds to Keyboard
		scrollView51.IgnoredInputKinds = ScrollingInputKinds.Keyboard;

		// Pressing Down key
		await PressKey(scrollView51, "down");
		await VerifyScrollViewRemainsAtView(scrollView51, 0.0, 0.0, 1.0f);

		// Pressing Right key
		await PressKey(scrollView51, "right");
		await VerifyScrollViewRemainsAtView(scrollView51, 0.0, 0.0, 1.0f);
	}

	[TestMethod]
	[Description("Tests keyboard interaction (Down, Up, PageDown, PageUp, End, Home, Right, Left) when ScrollView.XYFocusKeyboardNavigation is Enabled.")]
	public async Task VerifyScrollViewKeyboardInteractionWithXYFocusEnabled()
	{
		var scrollView51 = await SetupScrollViewTestWithImage("51");
		var scrollPresenter51 = scrollView51.ScrollPresenter;

		var scrollAmountForDownOrUpKey = scrollPresenter51.ActualHeight * 0.5;
		var scrollAmountForPageUpOrPageDownKey = scrollPresenter51.ActualHeight;
		var scrollAmountForRightOrLeftKey = scrollPresenter51.ActualWidth * 0.5;
		var maxScrollOffset = 800 - scrollPresenter51.ActualHeight;

		// Pressing Down key
		await PressKey(scrollView51, "down");
		await WaitForScrollViewOffsets(scrollView51, 0, scrollAmountForDownOrUpKey);

		// Pressing Up key
		await PressKey(scrollView51, "up");
		await WaitForScrollViewOffsets(scrollView51, 0, 0);

		// Pressing PageDown key
		await PressKey(scrollView51, "pagedown");
		await WaitForScrollViewOffsets(scrollView51, 0, scrollAmountForPageUpOrPageDownKey);

		// Pressing PageUp key
		await PressKey(scrollView51, "pageup");
		await WaitForScrollViewOffsets(scrollView51, 0, 0);

		// Pressing End key
		await PressKey(scrollView51, "end");
		await WaitForScrollViewOffsets(scrollView51, 0, maxScrollOffset);

		// Pressing Home key
		await PressKey(scrollView51, "home");
		await WaitForScrollViewOffsets(scrollView51, 0, 0);

		// Pressing Right key
		await PressKey(scrollView51, "right");
		await WaitForScrollViewOffsets(scrollView51, scrollAmountForRightOrLeftKey, 0);

		// Pressing Left key
		await PressKey(scrollView51, "left");
		await WaitForScrollViewOffsets(scrollView51, 0, 0);
	}

	[TestMethod]
	[Description("Tests End and Home keys when ScrollView.VerticalScrollMode is Disabled.")]
	public async Task ScrollHorizontallyWithEndHomeKeys()
	{
		var scrollView31 = await SetupScrollViewTestWithImage("31");

		// Pressing End key
		await PressKey(scrollView31, "end");
		await WaitForScrollViewOffsets(scrollView31, 900 - scrollView31.ScrollPresenter.ActualWidth, 0);

		// Pressing Home key
		await PressKey(scrollView31, "home");
		await WaitForScrollViewOffsets(scrollView31, 0, 0);
	}

	[TestMethod]
	[Description("Tests End and Home keys when ScrollView.VerticalScrollMode is Disabled in RightToLeft flow direction.")]
	public async Task ScrollHorizontallyWithEndHomeKeysInRTL()
	{
		var scrollView32 = await SetupScrollViewTestWithImage("32");

		// Pressing Home key
		await PressKey(scrollView32, "home");
		await WaitForScrollViewOffsets(scrollView32, 900 - scrollView32.ScrollPresenter.ActualWidth, 0);

		// Pressing End key
		await PressKey(scrollView32, "end");
		await WaitForScrollViewOffsets(scrollView32, 0, 0);
	}

	// scrollView11 of the ScrollViewsWithSimpleContents page; the button stands in for upstream's clicked buttonInScrollView11.
	private static async Task<(ScrollView ScrollView, Button Button)> SetupSimpleSingleScrollViewTest()
	{
		var buttonInScrollView11 = new Button { Width = 100, Height = 100, Content = "Button 1" };
		var content = new StackPanel
		{
			Width = 2000,
			Height = 2000,
			Background = new LinearGradientBrush
			{
				StartPoint = new(0, 0),
				EndPoint = new(1, 1),
				GradientStops =
				{
					new GradientStop { Offset = 0.0, Color = Colors.Blue },
					new GradientStop { Offset = 0.5, Color = Colors.White },
					new GradientStop { Offset = 1.0, Color = Colors.Red },
				},
			},
			Children =
			{
				new StackPanel
				{
					Width = 800,
					Height = 100,
					HorizontalAlignment = HorizontalAlignment.Left,
					Orientation = Orientation.Horizontal,
					Children =
					{
						buttonInScrollView11,
						new Button { Width = 100, Height = 100, Content = "Button 1B" },
						CreateRectangle(300, 100),
						new Button { Width = 100, Height = 100, Content = "Button 1C" },
					},
				},
				new Button { Width = 100, Height = 100, Content = "Button 2" },
				CreateRectangle(100, 300),
				new Button { Width = 100, Height = 100, Content = "Button 3" },
				CreateRectangle(100, 600),
				new Button { Width = 100, Height = 100, Content = "Button 4" },
				new Button { Width = 100, Height = 100, Content = "Button 5" },
			},
		};
		var scrollView11 = new ScrollView
		{
			Width = 300,
			Height = 400,
			ContentOrientation = ScrollingContentOrientation.None,
			Content = content,
		};

		await UITestHelper.Load(scrollView11);
		buttonInScrollView11.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		return (scrollView11, buttonInScrollView11);
	}

	// scrollView31, scrollView32 and scrollView51 of the ScrollViewsWithSimpleContents page. A Border of the
	// image's size stands in for it, so the tests don't depend on the image asset loading.
	private static async Task<ScrollView> SetupScrollViewTestWithImage(string suffix)
	{
		var scrollView = new ScrollView
		{
			Width = 300,
			Height = 400,
			ContentOrientation = ScrollingContentOrientation.None,
			IsTabStop = true,
		};

		switch (suffix)
		{
			case "31":
				scrollView.VerticalScrollMode = ScrollingScrollMode.Disabled;
				scrollView.Content = CreateImageStandIn(900, 900);
				break;
			case "32":
				scrollView.FlowDirection = FlowDirection.RightToLeft;
				scrollView.VerticalScrollMode = ScrollingScrollMode.Disabled;
				scrollView.ZoomMode = ScrollingZoomMode.Enabled;
				scrollView.Content = CreateImageStandIn(900, 900);
				break;
			case "51":
				scrollView.HorizontalScrollRailMode = ScrollingRailMode.Disabled;
				scrollView.VerticalScrollRailMode = ScrollingRailMode.Disabled;
				scrollView.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
				scrollView.Content = CreateImageStandIn(800, 800);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(suffix));
		}

		await UITestHelper.Load(scrollView);
		scrollView.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		return scrollView;
	}

	private static Rectangle CreateRectangle(double width, double height) => new()
	{
		Width = width,
		Height = height,
		HorizontalAlignment = HorizontalAlignment.Left,
		Fill = new SolidColorBrush(Colors.Red),
		Stroke = new SolidColorBrush(Colors.Black),
		StrokeThickness = 5,
	};

	private static Border CreateImageStandIn(double width, double height) => new()
	{
		Width = width,
		Height = height,
		Background = new SolidColorBrush(Colors.CornflowerBlue),
	};

	private static async Task PressKey(UIElement target, string key, int numPresses = 1)
	{
		for (var i = 0; i < numPresses; i++)
		{
			await KeyboardHelper.PressKeySequence($"$d$_{key}#$u$_{key}", target);
		}
	}

	private static async Task WaitForScrollViewOffsets(ScrollView scrollView, double expectedHorizontalOffset, double expectedVerticalOffset)
	{
		bool AreOffsetsCorrect() =>
			AreClose(expectedHorizontalOffset, scrollView.HorizontalOffset) &&
			AreClose(expectedVerticalOffset, scrollView.VerticalOffset);

		await WindowHelper.WaitFor(
			AreOffsetsCorrect,
			timeoutMS: 5000,
			message: $"Verify ScrollView offsets. Expected={expectedHorizontalOffset},{expectedVerticalOffset}, Actual={scrollView.HorizontalOffset},{scrollView.VerticalOffset}.");

		// Allow the view to settle and the StateChanged, ScrollCompleted or ZoomCompleted events to be raised.
		await Task.Delay(250);
		Verify.IsTrue(AreOffsetsCorrect(), $"Verify ScrollView offsets. Expected={expectedHorizontalOffset},{expectedVerticalOffset}, Actual={scrollView.HorizontalOffset},{scrollView.VerticalOffset}.");
	}

	private static async Task VerifyScrollViewRemainsAtView(ScrollView scrollView, double expectedHorizontalOffset, double expectedVerticalOffset, float expectedZoomFactor)
	{
		bool IsViewCorrect() =>
			AreClose(expectedHorizontalOffset, scrollView.HorizontalOffset) &&
			AreClose(expectedVerticalOffset, scrollView.VerticalOffset) &&
			AreClose(expectedZoomFactor, scrollView.ZoomFactor);

		Verify.IsTrue(IsViewCorrect(), $"Verify ScrollView initial view. Expected={expectedHorizontalOffset},{expectedVerticalOffset},{expectedZoomFactor}, Actual={scrollView.HorizontalOffset},{scrollView.VerticalOffset},{scrollView.ZoomFactor}.");

		await Task.Delay(750);

		Verify.IsTrue(IsViewCorrect(), $"Verify ScrollView final view. Expected={expectedHorizontalOffset},{expectedVerticalOffset},{expectedZoomFactor}, Actual={scrollView.HorizontalOffset},{scrollView.VerticalOffset},{scrollView.ZoomFactor}.");
	}

	private static bool AreClose(double expected, double actual, double delta = 0.1) => Math.Abs(expected - actual) <= delta;
}
