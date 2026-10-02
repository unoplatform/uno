// These tests drive the InputPane.OccludedRect setter, which is Uno-internal:
// on native WinUI the property is read-only (the OS owns the input pane).
#if HAS_UNO
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Toolkit.DevTools.Input;
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
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
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
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
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
