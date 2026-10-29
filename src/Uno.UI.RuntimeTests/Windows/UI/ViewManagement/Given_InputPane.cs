// These tests drive the InputPane.OccludedRect setter, which is Uno-internal:
// on native WinUI the property is read-only (the OS owns the input pane).
#if HAS_UNO
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.ViewManagement;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement;

[TestClass]
public class Given_InputPane
{
	// WinUI keeps the focused element this far from the input pane (ExtraPixelsForBringIntoView).
	private const double BringIntoViewPadding = 20;

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_OccludedRect_Set_Then_Focused_TextBox_Scrolled_Into_View()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom" };
		var scrollViewer = new ScrollViewer
		{
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

			// The ScrollViewer is at its end, so only the root can move the TextBox above the keyboard.
			scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = OccludeBelow(inputPane, WindowHeight / 2);

			await WaitForAboveOcclusion(textBox, occludedTop);
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Focused_TextBox_Has_Content_Below_Then_Scrolled_Above_Occlusion()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "middle" };
		var scrollViewer = new ScrollViewer
		{
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

			// Park the TextBox in the lower part of the viewport, with content below it.
			scrollViewer.ChangeView(null, 1240 - (0.55 * scrollViewer.ActualHeight), null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = WindowHeight / 2;
			Assert.IsTrue(GetBottom(textBox) > occludedTop, "Test setup: the TextBox must start below the keyboard top.");

			OccludeBelow(inputPane, occludedTop);

			await WaitForAboveOcclusion(textBox, occludedTop);
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	// Mirrors a details side panel whose own ScrollViewer starts just above the keyboard: no scroll inside the
	// panel can clear the focused field, so the whole content has to move up.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Field_In_Panel_Starting_Near_Occlusion_Then_Brought_Above_It()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "panel field" };
		var panel = new ScrollViewer
		{
			Content = new StackPanel
			{
				Children =
				{
					textBox,
					new Border { Height = 2000 },
				},
			},
		};
		var header = new Border();
		var root = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(WindowHeight * 0.55) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
			},
			Children = { header, panel },
		};
		Grid.SetRow(panel, 1);

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(root);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			// Only a sliver of the panel stays visible above the keyboard, less than the field height.
			var occludedTop = OccludeBelow(inputPane, GetTop(panel) + 20);

			await WaitForAboveOcclusion(textBox, occludedTop);
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Field_Not_In_ScrollViewer_Then_Brought_Above_Occlusion()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom", VerticalAlignment = VerticalAlignment.Bottom };
		var root = new Grid { Children = { textBox } };

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(root);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = OccludeBelow(inputPane, WindowHeight * 0.6);

			await WaitForAboveOcclusion(textBox, occludedTop);
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Focus_Moves_While_Occluded_Then_New_Field_Brought_Above_Occlusion()
	{
		var first = new TextBox { Height = 40, PlaceholderText = "first" };
		var second = new TextBox { Height = 40, PlaceholderText = "second" };
		var root = new Grid
		{
			Children =
			{
				new StackPanel
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					Children = { first, new Border { Height = 100 }, second },
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(root);
			Assert.IsTrue(first.Focus(FocusState.Programmatic), "First TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			// Tall enough that revealing the first field leaves the second one behind the keyboard.
			var occludedTop = OccludeBelow(inputPane, WindowHeight * 0.5);
			await WaitForAboveOcclusion(first, occludedTop);
			Assert.IsTrue(GetBottom(second) > occludedTop, "Test setup: the second TextBox must still be behind the keyboard.");

			Assert.IsTrue(second.Focus(FocusState.Programmatic), "Second TextBox failed to take focus.");

			await WaitForAboveOcclusion(second, occludedTop);
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	// Moving the content must keep hit-testing aligned with what is drawn, or a tap lands on another element.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Content_Moved_Above_Occlusion_Then_Tap_Hits_Rendered_Element()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "field" };
		var button = new Button { Content = "Tap me", Height = 40 };
		var clicks = 0;
		button.Click += (_, _) => clicks++;
		var root = new Grid
		{
			Children =
			{
				new StackPanel
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					Children = { button, textBox },
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(root);
			var buttonTopBefore = GetTop(button);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = OccludeBelow(inputPane, WindowHeight * 0.6);
			await WaitForAboveOcclusion(textBox, occludedTop);
			Assert.IsTrue(GetTop(button) < buttonTopBefore - 1, "Test setup: the content must have moved up.");

			var buttonTopLeft = button.TransformToVisual(null).TransformPoint(default);
			using var finger = InputInjector.TryCreate()?.GetFinger() ?? throw new System.InvalidOperationException("Failed to create finger");
			finger.Tap(new Point(buttonTopLeft.X + button.ActualWidth / 2, buttonTopLeft.Y + button.ActualHeight / 2));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, clicks, "A tap at the button's rendered position must hit the button.");
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	// The content stays moved while a field higher up takes focus, so the suggestion list has to be placed in window
	// coordinates: in content coordinates it sees room above that is actually off screen, and opens over the field.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_AutoSuggestBox_In_Moved_Content_Then_Suggestions_Fit_Visible_Area()
	{
		var autoSuggestBox = new AutoSuggestBox { MaxSuggestionListHeight = 2000 };
		var bottomField = new TextBox { Height = 40, PlaceholderText = "bottom" };
		var root = new Grid
		{
			Children =
			{
				new StackPanel
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					Children = { autoSuggestBox, new Border { Height = 260 }, bottomField },
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(root);
			Assert.IsTrue(bottomField.Focus(FocusState.Programmatic), "Bottom TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = OccludeBelow(inputPane, WindowHeight * 0.6);
			await WaitForAboveOcclusion(bottomField, occludedTop);

			var textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "AutoSuggestBox failed to take focus.");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(GetTop(textBox) > 0 && GetBottom(textBox) < occludedTop, "Test setup: the AutoSuggestBox must be visible above the keyboard.");

			// Setting the items once the template is applied refreshes and places the list, as typing would.
			autoSuggestBox.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Suggestion {i}").ToList();
			autoSuggestBox.IsSuggestionListOpen = true;
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			await WindowHelper.WaitFor(() => popup.IsOpen && popup.Child is FrameworkElement { ActualHeight: > 100 }, message: "The suggestion list did not open with its items.");
			await WindowHelper.WaitForIdle();

			var list = (FrameworkElement)popup.Child;
			var coversField = GetBottom(list) > GetTop(textBox) + 0.5 && GetTop(list) < GetBottom(textBox) - 0.5;
			Assert.IsFalse(coversField, $"The suggestion list ({GetTop(list):F1}-{GetBottom(list):F1}) covers the field being typed in ({GetTop(textBox):F1}-{GetBottom(textBox):F1}).");
			Assert.IsTrue(GetTop(list) >= -0.5, $"The suggestion list (top {GetTop(list):F1}) was placed above the top of the window.");
			Assert.IsTrue(GetBottom(list) <= occludedTop + 0.5, $"The suggestion list (bottom {GetBottom(list):F1}) was placed behind the keyboard ({occludedTop:F1}).");
		}
		finally
		{
			VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ToList().ForEach(p => p.IsOpen = false);
			await ClearOcclusion(inputPane);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaIOS)] // iOS CI: the content does not scroll back once the occlusion clears
	public async Task When_OccludedRect_Cleared_Then_Content_Restored()
	{
		var appPadding = new Thickness(10, 20, 30, 40);
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom" };
		var scrollViewer = new ScrollViewer
		{
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
			var textBoxTopBefore = GetTop(textBox);

			var occludedTop = OccludeBelow(inputPane, WindowHeight / 2);
			await WaitForAboveOcclusion(textBox, occludedTop);

			await ClearOcclusion(inputPane, clearContent: false);

			await WindowHelper.WaitFor(
				() => System.Math.Abs(GetTop(textBox) - textBoxTopBefore) < 0.5,
				message: $"The TextBox did not return to its position once the keyboard hid (top {GetTop(textBox)}, was {textBoxTopBefore}).");
			Assert.AreEqual(appPadding, scrollViewer.Padding, "The app-set ScrollViewer.Padding must be left untouched.");
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	// A sign-in card centered in a full-window ScrollViewer, far above the keyboard, must not move.
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

			var cardTopBefore = GetTop(card);
			var occludedTop = WindowHeight * 0.75;
			Assert.IsTrue(GetBottom(textBox) + BringIntoViewPadding < occludedTop, "Test setup: the focused TextBox must start above the keyboard top.");

			OccludeBelow(inputPane, occludedTop);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(cardTopBefore, GetTop(card), 0.5, $"Centered card moved by {cardTopBefore - GetTop(card):F1}px although nothing was occluded.");
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	// An app that handles InputPane.Showing and sets EnsuredFocusedElementInView owns the adjustment,
	// so the framework must leave the layout alone entirely.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaAndroid)] // Android CI: the baseline is captured with the layout already shifted
	public async Task When_App_Ensured_Focused_Element_In_View_Then_Layout_Untouched()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom", VerticalAlignment = VerticalAlignment.Bottom };
		var root = new Grid { Children = { textBox } };

		var inputPane = InputPane.GetForCurrentView();
		TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> handler =
			(_, args) => args.EnsuredFocusedElementInView = true;
		inputPane.Showing += handler;
		inputPane.Hiding += handler;
		try
		{
			await UITestHelper.Load(root);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();
			var textBoxTopBefore = GetTop(textBox);

			OccludeBelow(inputPane, WindowHeight * 0.6);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(textBoxTopBefore, GetTop(textBox), 0.5, "The layout must be left untouched when the app handled the adjustment.");
		}
		finally
		{
			inputPane.Showing -= handler;
			inputPane.Hiding -= handler;
			await ClearOcclusion(inputPane);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Occluded_Field_In_Viewport_Sized_Content_Then_Scrolled_Above_Occlusion()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom", VerticalAlignment = VerticalAlignment.Bottom };
		var scrollViewer = new ScrollViewer
		{
			Content = new Grid { Children = { textBox } },
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = WindowHeight * 0.6;
			Assert.IsTrue(GetBottom(textBox) > occludedTop, "Test setup: the TextBox must start behind the keyboard.");

			OccludeBelow(inputPane, occludedTop);

			await WaitForAboveOcclusion(textBox, occludedTop);
		}
		finally
		{
			await ClearOcclusion(inputPane);
		}
	}

	// The keyboard came up for a field at the top, then a flyout opens from a button that the keyboard covers.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Flyout_Opened_While_Occluded_Then_Placed_Above_Occlusion()
	{
		var page = BuildBottomFlyoutPage(listMaxHeight: WindowHeight * 0.25);

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(page.Root);
			Assert.IsTrue(page.TopField.Focus(FocusState.Programmatic), "Top TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var occludedTop = OccludeBelow(inputPane, WindowHeight * 0.55);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(GetTop(page.Button) > occludedTop, "Test setup: the button must be behind the keyboard.");

			page.Flyout.ShowAt(page.Button);
			var presenter = await WaitForFlyoutPresenter(page.Flyout);
			Assert.IsTrue(presenter.ActualHeight + 20 < occludedTop, $"Test setup: the flyout ({presenter.ActualHeight:F1}px) must fit above the keyboard ({occludedTop:F1}).");

			await WaitForFlyoutAboveOcclusion(presenter, occludedTop);
		}
		finally
		{
			await CloseFlyoutAndClearOcclusion(page.Flyout, inputPane);
		}
	}

	// The flyout is open next to its button at the bottom, and the keyboard comes up for the field inside it.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Flyout_Field_Focused_Then_Flyout_Moved_Above_Occlusion()
	{
		var page = BuildBottomFlyoutPage(listMaxHeight: WindowHeight * 0.25);

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(page.Root);

			page.Flyout.ShowAt(page.Button);
			var presenter = await WaitForFlyoutPresenter(page.Flyout);
			Assert.IsTrue(page.FlyoutField.Focus(FocusState.Programmatic), "Flyout TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();
			var presenterTopBefore = GetTop(presenter);

			var occludedTop = WindowHeight * 0.55;
			Assert.IsTrue(GetBottom(presenter) > occludedTop, $"Test setup: the flyout (bottom {GetBottom(presenter):F1}) must start behind the keyboard ({occludedTop:F1}).");
			Assert.IsTrue(presenter.ActualHeight + 20 < occludedTop, $"Test setup: the flyout ({presenter.ActualHeight:F1}px) must fit above the keyboard ({occludedTop:F1}).");

			OccludeBelow(inputPane, occludedTop);

			await WaitForFlyoutAboveOcclusion(presenter, occludedTop);
			Assert.IsTrue(page.Flyout.IsOpen, "The flyout must stay open while the keyboard shows.");

			await ClearOcclusion(inputPane, clearContent: false);

			await WaitFor(
				() => System.Math.Abs(GetTop(presenter) - presenterTopBefore) < 0.5,
				() => $"The flyout did not return next to its button once the keyboard hid (top {GetTop(presenter):F1}, was {presenterTopBefore:F1}).");
		}
		finally
		{
			await CloseFlyoutAndClearOcclusion(page.Flyout, inputPane);
		}
	}

	// A flyout taller than the area above the keyboard is shrunk next to its button, so its content scrolls, and gets its
	// own size back once the keyboard hides.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[DataRow(true)]
	[DataRow(false)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Flyout_Taller_Than_Area_Above_Occlusion_Then_Resized_To_Fit(bool occludeBeforeOpening)
	{
		var page = BuildBottomFlyoutPage(listMaxHeight: WindowHeight * 0.6);

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(page.Root);

			page.Flyout.ShowAt(page.Button);
			var presenter = await WaitForFlyoutPresenter(page.Flyout);
			var naturalHeight = presenter.ActualHeight;
			var naturalTop = GetTop(presenter);

			var occludedTop = WindowHeight * 0.55;
			AssertTallerThanAreaAboveOcclusion(page, presenter, occludedTop);

			if (occludeBeforeOpening)
			{
				page.Flyout.Hide();
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(page.TopField.Focus(FocusState.Programmatic), "Top TextBox failed to take focus.");
				await WindowHelper.WaitForIdle();

				OccludeBelow(inputPane, occludedTop);
				await WindowHelper.WaitForIdle();

				page.Flyout.ShowAt(page.Button);
				presenter = await WaitForFlyoutPresenter(page.Flyout);
			}
			else
			{
				Assert.IsTrue(page.FlyoutField.Focus(FocusState.Programmatic), "Flyout TextBox failed to take focus.");
				await WindowHelper.WaitForIdle();

				OccludeBelow(inputPane, occludedTop);
			}

			await WaitForShrunkAboveOcclusion(presenter, occludedTop, naturalHeight);

			await ClearOcclusion(inputPane, clearContent: false);

			await WaitFor(
				() => System.Math.Abs(presenter.ActualHeight - naturalHeight) < 0.5 && System.Math.Abs(GetTop(presenter) - naturalTop) < 0.5,
				() => $"The flyout (top {GetTop(presenter):F1}, height {presenter.ActualHeight:F1}) did not get its size back next to its button once the keyboard hid (top {naturalTop:F1}, height {naturalHeight:F1}).");
		}
		finally
		{
			await CloseFlyoutAndClearOcclusion(page.Flyout, inputPane);
		}
	}

	// The keyboard stays up while a shrunk flyout closes and opens again: it must be shrunk again, not open at its own height.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Resized_Flyout_Reopened_While_Occluded_Then_Resized_Again()
	{
		var page = BuildBottomFlyoutPage(listMaxHeight: WindowHeight * 0.6);

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(page.Root);

			page.Flyout.ShowAt(page.Button);
			var presenter = await WaitForFlyoutPresenter(page.Flyout);
			var naturalHeight = presenter.ActualHeight;
			var occludedTop = WindowHeight * 0.55;
			AssertTallerThanAreaAboveOcclusion(page, presenter, occludedTop);

			Assert.IsTrue(page.FlyoutField.Focus(FocusState.Programmatic), "Flyout TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();
			OccludeBelow(inputPane, occludedTop);
			await WaitForShrunkAboveOcclusion(presenter, occludedTop, naturalHeight);

			page.Flyout.Hide();
			await WindowHelper.WaitFor(() => !page.Flyout.IsOpen, message: "The flyout did not close.");
			Assert.IsTrue(page.TopField.Focus(FocusState.Programmatic), "Top TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			page.Flyout.ShowAt(page.Button);
			presenter = await WaitForFlyoutPresenter(page.Flyout);

			await WaitForShrunkAboveOcclusion(presenter, occludedTop, naturalHeight);
		}
		finally
		{
			await CloseFlyoutAndClearOcclusion(page.Flyout, inputPane);
		}
	}

	// Only the height is forced while the keyboard is up: content that gets wider (e.g. results widening while typing)
	// must widen the presenter rather than be clipped to its former width.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Resized_Flyout_Content_Widens_Then_Presenter_Widens()
	{
		var widening = new Border { Width = 160, Height = 20 };
		var page = BuildBottomFlyoutPage(listMaxHeight: WindowHeight * 0.6, contentWidth: null, extraContent: widening);

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(page.Root);

			page.Flyout.ShowAt(page.Button);
			var presenter = await WaitForFlyoutPresenter(page.Flyout);
			var naturalHeight = presenter.ActualHeight;
			var occludedTop = WindowHeight * 0.55;
			AssertTallerThanAreaAboveOcclusion(page, presenter, occludedTop);

			Assert.IsTrue(page.FlyoutField.Focus(FocusState.Programmatic), "Flyout TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();
			OccludeBelow(inputPane, occludedTop);
			await WaitForShrunkAboveOcclusion(presenter, occludedTop, naturalHeight);
			var widthBefore = presenter.ActualWidth;

			widening.Width = 360;

			await WaitFor(
				() => presenter.ActualWidth > widthBefore + 100 && LayoutInformation.GetLayoutSlot(presenter).Width >= presenter.ActualWidth - 0.5,
				() => $"The flyout (width {presenter.ActualWidth:F1}, was {widthBefore:F1}) was not widened inside its slot (width {LayoutInformation.GetLayoutSlot(presenter).Width:F1}).");
			await WaitForShrunkAboveOcclusion(presenter, occludedTop, naturalHeight);
		}
		finally
		{
			await CloseFlyoutAndClearOcclusion(page.Flyout, inputPane);
		}
	}

	// CalendarDatePicker opts out of the presenter resizing (DisablePresenterResizing), like in WinUI.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_CalendarDatePicker_Flyout_Does_Not_Fit_Then_Not_Resized()
	{
		var picker = new CalendarDatePicker { VerticalAlignment = VerticalAlignment.Bottom };
		var root = new Grid { Padding = new Thickness(16), Children = { picker } };

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(root);

			var occludedTop = OccludeBelow(inputPane, WindowHeight * 0.3);
			await WindowHelper.WaitForIdle();

			picker.IsCalendarOpen = true;
			FrameworkElement presenter = null;
			await WindowHelper.WaitFor(
				() => (presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Select(p => p.Child).OfType<FlyoutPresenter>().FirstOrDefault()) is { ActualHeight: > 0 },
				message: "The calendar flyout did not open.");
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(presenter.ActualHeight > occludedTop, $"Test setup: the calendar ({presenter.ActualHeight:F1}px) must be taller than the area above the keyboard ({occludedTop:F1}).");
			Assert.AreEqual(DependencyProperty.UnsetValue, presenter.ReadLocalValue(FrameworkElement.HeightProperty), "The calendar flyout presenter must not be given a height.");
			Assert.AreEqual(DependencyProperty.UnsetValue, presenter.ReadLocalValue(FrameworkElement.WidthProperty), "The calendar flyout presenter must not be given a width.");
		}
		finally
		{
			picker.IsCalendarOpen = false;
			VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ToList().ForEach(p => p.IsOpen = false);
			await ClearOcclusion(inputPane);
		}
	}

	private static (Grid Root, TextBox TopField, Button Button, Flyout Flyout, TextBox FlyoutField) BuildBottomFlyoutPage(
		double listMaxHeight,
		double? contentWidth = 320,
		UIElement extraContent = null)
	{
		var flyoutField = new TextBox { PlaceholderText = "Search..." };
		var content = new StackPanel
		{
			Spacing = 8,
			Children =
			{
				flyoutField,
				new ListView
				{
					MaxHeight = listMaxHeight,
					ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList(),
				},
			},
		};
		if (contentWidth is { } width)
		{
			content.Width = width;
		}
		if (extraContent is not null)
		{
			content.Children.Insert(1, extraContent);
		}

		var flyout = new Flyout
		{
			Placement = FlyoutPlacementMode.Top,
			Content = content,
		};
		var button = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Content = "Open flyout (anchored near the bottom)",
			Flyout = flyout,
		};
		var topField = new TextBox { Height = 40, PlaceholderText = "top", VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid
		{
			Padding = new Thickness(16),
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = GridLength.Auto },
			},
			Children = { topField, button },
		};
		Grid.SetRow(button, 1);

		return (root, topField, button, flyout, flyoutField);
	}

	private static async Task<FrameworkElement> WaitForFlyoutPresenter(Flyout flyout)
	{
		await WindowHelper.WaitFor(() => flyout.IsOpen && flyout.GetPresenter() is { IsLoaded: true, ActualHeight: > 0 }, message: "The flyout did not open.");
		await WindowHelper.WaitForIdle();

		return flyout.GetPresenter();
	}

	// Like WinUI, the flyout must sit right above the keyboard rather than move to the top of the window.
	private static async Task WaitForFlyoutAboveOcclusion(FrameworkElement presenter, double occludedTop)
	{
		await WaitFor(
			() => GetBottom(presenter) <= occludedTop + 0.5 && GetTop(presenter) >= -0.5,
			() => $"The flyout (top {GetTop(presenter):F1}, bottom {GetBottom(presenter):F1}) was not placed above the keyboard top ({occludedTop:F1}).");

		Assert.IsTrue(
			GetBottom(presenter) >= occludedTop - BringIntoViewPadding,
			$"The flyout (bottom {GetBottom(presenter):F1}) was moved way above the keyboard top ({occludedTop:F1}).");
	}

	// Like WinUI's ResizeToFit, a flyout that fits nowhere above the keyboard is shrunk there and its content scrolls.
	private static async Task WaitForShrunkAboveOcclusion(FrameworkElement presenter, double occludedTop, double naturalHeight)
	{
		await WaitForFlyoutAboveOcclusion(presenter, occludedTop);

		Assert.IsTrue(presenter.ActualHeight < naturalHeight - 1, $"The flyout (height {presenter.ActualHeight:F1}) was not shrunk from its own height ({naturalHeight:F1}).");

		var scrollViewer = (ScrollViewer)((Control)presenter).GetTemplateChild("ScrollViewer");
		await WaitFor(
			() => scrollViewer.ExtentHeight > scrollViewer.ViewportHeight + 1,
			() => $"The flyout content must scroll once shrunk (extent {scrollViewer.ExtentHeight:F1}, viewport {scrollViewer.ViewportHeight:F1}).");
	}

	private static void AssertTallerThanAreaAboveOcclusion((Grid Root, TextBox TopField, Button Button, Flyout Flyout, TextBox FlyoutField) page, FrameworkElement presenter, double occludedTop)
	{
		Assert.IsTrue(GetBottom(presenter) <= GetTop(page.Button), "Test setup: the flyout must open above its button while nothing is occluded.");
		Assert.IsTrue(presenter.ActualHeight > occludedTop + 20, $"Test setup: the flyout ({presenter.ActualHeight:F1}px) must be taller than the area above the keyboard ({occludedTop:F1}).");
	}

	// Builds the failure message once the wait timed out, so it shows where the flyout ended up rather than where it started.
	private static async Task WaitFor(System.Func<bool> condition, System.Func<string> message)
	{
		try
		{
			await WindowHelper.WaitFor(condition);
		}
		catch (AssertFailedException)
		{
			Assert.Fail(message());
		}
	}

	private static async Task CloseFlyoutAndClearOcclusion(Flyout flyout, InputPane inputPane)
	{
		flyout.Hide();
		VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ToList().ForEach(p => p.IsOpen = false);
		await ClearOcclusion(inputPane);
	}

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
			Content = new Grid { Children = { card } },
		};

		return (scrollViewer, card, textBox);
	}

	private static double WindowHeight => WindowHelper.XamlRoot.Size.Height;

	private static double OccludeBelow(InputPane inputPane, double occludedTop)
	{
		var size = WindowHelper.XamlRoot.Size;
		inputPane.OccludedRect = new Rect(0, occludedTop, size.Width, size.Height - occludedTop);
		return occludedTop;
	}

	private static async Task ClearOcclusion(InputPane inputPane, bool clearContent = true)
	{
		inputPane.OccludedRect = new Rect(0, 0, 0, 0);
		await WindowHelper.WaitForIdle();

		if (clearContent)
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The element must end fully visible above the keyboard, without being moved further than the
	// bring-into-view padding requires.
	private static async Task WaitForAboveOcclusion(FrameworkElement element, double occludedTop)
	{
		await WindowHelper.WaitFor(
			() => GetBottom(element) <= occludedTop + 0.5 && GetTop(element) >= -0.5,
			message: $"{element} (top {GetTop(element)}, bottom {GetBottom(element)}) was not brought above the keyboard top ({occludedTop}).");

		Assert.IsTrue(
			GetBottom(element) >= occludedTop - element.ActualHeight - BringIntoViewPadding - 1,
			$"{element} (bottom {GetBottom(element)}) was moved way above the keyboard top ({occludedTop}).");
	}

	private static double GetTop(FrameworkElement element)
		=> element.TransformToVisual(null).TransformPoint(default).Y;

	private static double GetBottom(FrameworkElement element)
		=> GetTop(element) + element.ActualHeight;
}
#endif
