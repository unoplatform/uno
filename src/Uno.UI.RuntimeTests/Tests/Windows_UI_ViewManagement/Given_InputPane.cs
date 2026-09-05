// These tests drive the InputPane.OccludedRect setter, which is Uno-internal:
// on native WinUI the property is read-only (the OS owns the input pane).
#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement;

[TestClass]
public class Given_InputPane
{
	// Validates the Skia bring-into-view path that InputPane.OccludedRect drives
	// (InputPane.skia.cs -> EnsureFocusedElementInViewPartial -> ScrollContentPresenter.Pad +
	// StartBringIntoView). This is the device-independent half of the WASM soft-keyboard fix:
	// the WASM head now feeds OccludedRect, and this asserts the shared consumer reacts to it.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_OccludedRect_Set_Then_Focused_TextBox_Scrolled_Into_View()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom" };
		var scrollViewer = new ScrollViewer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Content = new StackPanel
			{
				Children =
				{
					new Border { Height = 2000 },
					textBox,
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);

			// Bring the bottom TextBox fully into view, then focus it.
			scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var offsetBeforeOcclusion = scrollViewer.VerticalOffset;

			// Simulate the on-screen keyboard occluding the bottom half of the ScrollViewer,
			// where the focused TextBox now sits.
			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var occludedTop = scrollViewerTopLeft.Y + (scrollViewer.ActualHeight / 2);
			inputPane.OccludedRect = new Rect(scrollViewerTopLeft.X, occludedTop, scrollViewer.ActualWidth, scrollViewer.ActualHeight);

			// OccludedRect -> OnOccludedRectChanged schedules the pad + StartBringIntoView across
			// two dispatcher hops and a layout pass, so wait for the resulting scroll.
			await WindowHelper.WaitFor(
				() => scrollViewer.VerticalOffset > offsetBeforeOcclusion + 1,
				message: $"Focused TextBox was not scrolled above the keyboard (offset stayed at {offsetBeforeOcclusion}).");

			await WindowHelper.WaitFor(
				() => GetBottom(textBox) <= occludedTop + 0.5,
				message: $"Focused TextBox (bottom {GetBottom(textBox)}) was left below the keyboard top ({occludedTop}).");
		}
		finally
		{
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}

	// The regression OP hit with a TextBox mid-content (e.g. inside a side panel): the pad used to be
	// reduced by the space below the focused element, leaving it partially behind the keyboard.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Focused_TextBox_Has_Content_Below_Then_Scrolled_Above_Occlusion()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "middle" };
		var scrollViewer = new ScrollViewer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Content = new StackPanel
			{
				Children =
				{
					new Border { Height = 1200 },
					textBox,
					new Border { Height = 800 },
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);

			// Position the TextBox inside the viewport with a large gap below it (~45% of the
			// viewport), then occlude the bottom half: the gap is what the old padding formula
			// subtracted, leaving the TextBox behind the keyboard.
			var viewportHeight = scrollViewer.ActualHeight;
			scrollViewer.ChangeView(null, 1240 - (0.55 * viewportHeight), null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var occludedTop = scrollViewerTopLeft.Y + (viewportHeight / 2);
			Assert.IsTrue(GetBottom(textBox) > occludedTop, "Test setup: the TextBox must start below the keyboard top.");

			inputPane.OccludedRect = new Rect(scrollViewerTopLeft.X, occludedTop, scrollViewer.ActualWidth, viewportHeight);

			await WindowHelper.WaitFor(
				() => GetBottom(textBox) <= occludedTop + 0.5,
				message: $"Focused TextBox (bottom {GetBottom(textBox)}) was left below the keyboard top ({occludedTop}).");

			// The scroll must stay minimal: flush with the keyboard top, not over-scrolled.
			Assert.IsTrue(
				GetBottom(textBox) > occludedTop - textBox.ActualHeight - 40,
				$"Focused TextBox (bottom {GetBottom(textBox)}) was over-scrolled way above the keyboard top ({occludedTop}).");
		}
		finally
		{
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}

	// The occlusion pad is applied through ScrollViewer.Padding; clearing the occlusion used to
	// reset it to zero, silently wiping an app-set Padding.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_OccludedRect_Cleared_Then_ScrollViewer_Padding_Restored()
	{
		var appPadding = new Thickness(10, 20, 30, 40);
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom" };
		var scrollViewer = new ScrollViewer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Padding = appPadding,
			Content = new StackPanel
			{
				Children =
				{
					new Border { Height = 2000 },
					textBox,
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);

			scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var occludedTop = scrollViewerTopLeft.Y + (scrollViewer.ActualHeight / 2);
			inputPane.OccludedRect = new Rect(scrollViewerTopLeft.X, occludedTop, scrollViewer.ActualWidth, scrollViewer.ActualHeight);

			// The pad adds to the app padding instead of replacing it.
			await WindowHelper.WaitFor(
				() => scrollViewer.Padding.Bottom > appPadding.Bottom,
				message: "The occlusion pad was not applied on top of the app padding.");
			Assert.AreEqual(appPadding.Left, scrollViewer.Padding.Left);
			Assert.AreEqual(appPadding.Top, scrollViewer.Padding.Top);
			Assert.AreEqual(appPadding.Right, scrollViewer.Padding.Right);

			inputPane.OccludedRect = new Rect(0, 0, 0, 0);

			await WindowHelper.WaitFor(
				() => scrollViewer.Padding == appPadding,
				message: $"ScrollViewer.Padding was not restored to the app value (got {scrollViewer.Padding}).");
		}
		finally
		{
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}

	// The occlusion pad shrinks the viewport so BringIntoView can land the focused element above the
	// keyboard. It must not re-arrange content that sizes itself to that viewport: a sign-in card
	// centered in a Grid used to jump up by half the keyboard height even though it was never occluded.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Focused_Element_Not_Occluded_Then_Centered_Content_Is_Not_Moved()
	{
		var (scrollViewer, card, textBox) = BuildCenteredCardPage();

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var viewportHeight = scrollViewer.ActualHeight;
			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var occludedTop = scrollViewerTopLeft.Y + (viewportHeight * 0.6);
			var occlusionHeight = viewportHeight * 0.4;

			var cardTopBefore = GetTop(card);
			Assert.IsTrue(GetBottom(textBox) < occludedTop, "Test setup: the focused TextBox must start above the keyboard top.");

			inputPane.OccludedRect = new Rect(scrollViewerTopLeft.X, occludedTop, scrollViewer.ActualWidth, occlusionHeight);

			// Guards against a vacuous pass: wait until the occlusion path actually padded the presenter.
			await WindowHelper.WaitFor(
				() => scrollViewer.Padding.Bottom > 0,
				message: "The occlusion pad was never applied.");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(cardTopBefore, GetTop(card), 0.5, $"Centered card moved by {cardTopBefore - GetTop(card):F1}px although nothing was occluded.");

			// The pad becomes scrollable extent, so the area behind the keyboard stays reachable.
			Assert.AreEqual(viewportHeight, scrollViewer.ExtentHeight, 0.5, "The occluded area was squeezed out of the content instead of becoming scrollable extent.");
			Assert.IsTrue(scrollViewer.ViewportHeight < viewportHeight, "The viewport must still shrink so BringIntoView stops above the keyboard.");

			inputPane.OccludedRect = new Rect(0, 0, 0, 0);

			await WindowHelper.WaitFor(
				() => scrollViewer.Padding.Bottom == 0,
				message: "The occlusion pad was not restored.");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(cardTopBefore, GetTop(card), 0.5, "Centered card did not return to its original position.");
		}
		finally
		{
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}

	// An app that handles InputPane.Showing and sets EnsuredFocusedElementInView owns the adjustment,
	// so the framework must leave the layout alone entirely.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_App_Ensured_Focused_Element_In_View_Then_No_Pad_Is_Applied()
	{
		var (scrollViewer, card, textBox) = BuildCenteredCardPage();

		var inputPane = InputPane.GetForCurrentView();
		TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> handler =
			(_, args) => args.EnsuredFocusedElementInView = true;
		inputPane.Showing += handler;
		inputPane.Hiding += handler;
		try
		{
			await UITestHelper.Load(scrollViewer);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var viewportHeight = scrollViewer.ActualHeight;
			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var cardTopBefore = GetTop(card);

			inputPane.OccludedRect = new Rect(
				scrollViewerTopLeft.X,
				scrollViewerTopLeft.Y + (viewportHeight * 0.6),
				scrollViewer.ActualWidth,
				viewportHeight * 0.4);

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0d, scrollViewer.Padding.Bottom, 0.5, "No occlusion pad must be applied when the app handled the adjustment.");
			Assert.AreEqual(cardTopBefore, GetTop(card), 0.5, "The layout must be left untouched when the app handled the adjustment.");
		}
		finally
		{
			inputPane.Showing -= handler;
			inputPane.Hiding -= handler;
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}

	// The bring-into-view guarantee must survive arranging against the un-occluded height: content that
	// sizes itself to the viewport now keeps a field behind the keyboard instead of being squeezed above
	// it, so the scroll has to do the work. This is the geometry the taller-than-viewport tests never hit.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Occluded_Field_In_Viewport_Sized_Content_Then_Scrolled_Above_Occlusion()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom", VerticalAlignment = VerticalAlignment.Bottom };
		var scrollViewer = new ScrollViewer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Content = new Grid { Children = { textBox } },
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var viewportHeight = scrollViewer.ActualHeight;
			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var occludedTop = scrollViewerTopLeft.Y + (viewportHeight * 0.6);

			Assert.IsTrue(GetBottom(textBox) > occludedTop, "Test setup: the TextBox must start behind the keyboard.");

			inputPane.OccludedRect = new Rect(scrollViewerTopLeft.X, occludedTop, scrollViewer.ActualWidth, viewportHeight * 0.4);

			await WindowHelper.WaitFor(
				() => GetBottom(textBox) <= occludedTop + 0.5,
				message: $"Focused TextBox (bottom {GetBottom(textBox)}) was left below the keyboard top ({occludedTop}).");

			// The scroll must stay minimal: flush with the keyboard top, not over-scrolled.
			Assert.IsTrue(
				GetBottom(textBox) > occludedTop - textBox.ActualHeight - 40,
				$"Focused TextBox (bottom {GetBottom(textBox)}) was over-scrolled way above the keyboard top ({occludedTop}).");
		}
		finally
		{
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}

	// A sign-in card centered in a Grid that fills a full-window ScrollViewer, with the focused field
	// nowhere near the keyboard.
	private static (ScrollViewer ScrollViewer, Border Card, TextBox TextBox) BuildCenteredCardPage()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "user" };
		var card = new Border
		{
			Width = 300,
			Height = 200,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Child = textBox,
		};
		var scrollViewer = new ScrollViewer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Content = new Grid { Children = { card } },
		};

		return (scrollViewer, card, textBox);
	}

	private static double GetTop(FrameworkElement element)
		=> element.TransformToVisual(null).TransformPoint(default).Y;

	private static double GetBottom(FrameworkElement element)
		=> element.TransformToVisual(null).TransformPoint(default).Y + element.ActualHeight;
}
#endif
