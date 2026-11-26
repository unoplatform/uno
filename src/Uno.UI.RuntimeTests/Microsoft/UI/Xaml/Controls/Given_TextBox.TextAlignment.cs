using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

using Colors = Microsoft.UI.Colors;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_TextBox
	{
		// Ink positions are physical x in a 340 px LeftToRight host; every control is 300 px wide at x = 0.
		public enum InkSide { Left, Center, Right }

		private const string LatinText = "Hello world.";
		private const string ArabicText = "سلام عليكم";
		private const string HebrewText = "שלום עולם";

		[TestMethod]
		public void When_New_TextBox_Reports_DetectFromContent()
		{
			var SUT = new TextBox();

			Assert.AreEqual(TextAlignment.DetectFromContent, SUT.TextAlignment);
			Assert.AreEqual(DependencyProperty.UnsetValue, SUT.ReadLocalValue(TextBox.TextAlignmentProperty));
		}

		[TestMethod]
		// WinUI registers Left and reports Left after ClearValue. Uno keeps DetectFromContent until Left means the
		// start in a RightToLeft TextBox, as Uno draws Left on the physical left today.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_TextBox_TextAlignment_Cleared_Reports_DetectFromContent()
		{
			var SUT = new TextBox { TextAlignment = TextAlignment.Right };
			SUT.ClearValue(TextBox.TextAlignmentProperty);

			Assert.AreEqual(TextAlignment.DetectFromContent, SUT.TextAlignment);
			Assert.AreEqual(TextAlignment.DetectFromContent, TextBox.TextAlignmentProperty.GetMetadata(typeof(TextBox)).DefaultValue);
		}

		[TestMethod]
		public void When_New_RichEditBox_Reports_DetectFromContent()
		{
			Assert.AreEqual(TextAlignment.DetectFromContent, new RichEditBox().TextAlignment);
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, InkSide.Left)]
		[DataRow(LatinText, FlowDirection.RightToLeft, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.RightToLeft, InkSide.Right)]
		public async Task When_TextBox_TextAlignment_Default_Text_Detects(string text, FlowDirection flowDirection, InkSide expected)
		{
			var SUT = new TextBox { Text = text, FlowDirection = flowDirection };

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, InkSide.Left)]
		[DataRow(LatinText, FlowDirection.RightToLeft, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.RightToLeft, InkSide.Right)]
		public async Task When_TextBox_TextAlignment_DetectFromContent_Text_Detects(string text, FlowDirection flowDirection, InkSide expected)
		{
			var SUT = new TextBox { Text = text, FlowDirection = flowDirection, TextAlignment = TextAlignment.DetectFromContent };

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(FlowDirection.LeftToRight)]
		[DataRow(FlowDirection.RightToLeft)]
		public async Task When_TextBox_TextAlignment_DetectFromContent_From_Style_Text_Detects(FlowDirection flowDirection)
		{
			var root = (Grid)XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<Grid.Resources>
						<Style TargetType="TextBox" BasedOn="{StaticResource DefaultTextBoxStyle}">
							<Setter Property="TextAlignment" Value="DetectFromContent" />
						</Style>
					</Grid.Resources>
					<TextBox />
				</Grid>
				""");
			var SUT = (TextBox)root.Children[0];
			SUT.Text = ArabicText;
			SUT.FlowDirection = flowDirection;
			SUT.Width = 300;
			SUT.FontSize = 20;

			var side = await GetInkSide(root, ArabicText, () => SUT.Foreground = new SolidColorBrush(Colors.Red));

			Assert.AreEqual(TextAlignment.DetectFromContent, SUT.TextAlignment);
			Assert.AreEqual(DependencyProperty.UnsetValue, SUT.ReadLocalValue(TextBox.TextAlignmentProperty));
			Assert.AreEqual(InkSide.Right, side);
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, TextAlignment.Left, InkSide.Left)]
		[DataRow(LatinText, TextAlignment.Center, InkSide.Center)]
		[DataRow(LatinText, TextAlignment.Right, InkSide.Right)]
		[DataRow(ArabicText, TextAlignment.Left, InkSide.Left)]
		[DataRow(ArabicText, TextAlignment.Center, InkSide.Center)]
		[DataRow(ArabicText, TextAlignment.Right, InkSide.Right)]
		[DataRow(HebrewText, TextAlignment.Left, InkSide.Left)]
		[DataRow(HebrewText, TextAlignment.Center, InkSide.Center)]
		[DataRow(HebrewText, TextAlignment.Right, InkSide.Right)]
		public async Task When_TextBox_TextAlignment_Set_Text_Follows(string text, TextAlignment alignment, InkSide expected)
		{
			var SUT = new TextBox { Text = text, TextAlignment = alignment };

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// WinUI reports Left after ClearValue, and Left is the start of a RightToLeft TextBox there. Uno keeps
		// DetectFromContent until it draws Left at the start too, so its cleared TextBox keeps detecting.
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, false, InkSide.Left)]
		[DataRow(LatinText, FlowDirection.RightToLeft, false, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, false, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, true, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, false, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, true, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.LeftToRight, false, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.RightToLeft, false, InkSide.Right)]
		public async Task When_TextBox_TextAlignment_Cleared_Text_Detects(string text, FlowDirection flowDirection, bool clearWhenLoaded, InkSide expected)
		{
			var SUT = new TextBox { Text = text, FlowDirection = flowDirection, TextAlignment = TextAlignment.Left };
			if (!clearWhenLoaded)
			{
				SUT.ClearValue(TextBox.TextAlignmentProperty);
			}

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text, clearWhenLoaded ? () => SUT.ClearValue(TextBox.TextAlignmentProperty) : null));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, false)]
		[DataRow(LatinText, FlowDirection.LeftToRight, true)]
		[DataRow(LatinText, FlowDirection.RightToLeft, false)]
		[DataRow(LatinText, FlowDirection.RightToLeft, true)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, false)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, true)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, false)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, true)]
		[DataRow(HebrewText, FlowDirection.LeftToRight, false)]
		[DataRow(HebrewText, FlowDirection.RightToLeft, false)]
		public async Task When_TextBox_TextAlignment_Default_Placeholder_Side(string text, FlowDirection flowDirection, bool explicitDetectFromContent)
		{
			var SUT = new TextBox { PlaceholderText = text, FlowDirection = flowDirection };
			if (explicitDetectFromContent)
			{
				SUT.TextAlignment = TextAlignment.DetectFromContent;
			}

			Assert.AreEqual(await GetExpectedPlaceholderSide(text), await GetPlaceholderInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// WinUI reports Left after ClearValue (see When_TextBox_TextAlignment_Cleared_Text_Detects).
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight)]
		[DataRow(LatinText, FlowDirection.RightToLeft)]
		[DataRow(ArabicText, FlowDirection.LeftToRight)]
		[DataRow(ArabicText, FlowDirection.RightToLeft)]
		public async Task When_TextBox_TextAlignment_Cleared_Placeholder_Side(string text, FlowDirection flowDirection)
		{
			var SUT = new TextBox { PlaceholderText = text, FlowDirection = flowDirection, TextAlignment = TextAlignment.Right };
			SUT.ClearValue(TextBox.TextAlignmentProperty);

			Assert.AreEqual(await GetExpectedPlaceholderSide(text), await GetPlaceholderInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow(FlowDirection.LeftToRight)]
		[DataRow(FlowDirection.RightToLeft)]
		public async Task When_PasswordBox_Masked_Bullets_Same_For_Any_Script(FlowDirection flowDirection)
			=> AssertSameForEveryScript(await MeasureMaskedPasswords(flowDirection, withCaret: false));

		[TestMethod]
		[RequiresScaling(1f)]
		// A screenshot of a focused WinUI PasswordBox shows no caret.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		[DataRow(FlowDirection.LeftToRight)]
		[DataRow(FlowDirection.RightToLeft)]
		public async Task When_PasswordBox_Masked_Caret_Same_For_Any_Script(FlowDirection flowDirection)
			=> AssertSameForEveryScript(await MeasureMaskedPasswords(flowDirection, withCaret: true));

		// A masked PasswordBox must not show which script its password is in: same length, same drawing.
		private static async Task<List<(int min, int max, int caret)>> MeasureMaskedPasswords(FlowDirection flowDirection, bool withCaret)
		{
			var results = new List<(int min, int max, int caret)>();
			foreach (var password in new[] { "abcdefgh", "سلامعليك", "שלוםעולם" })
			{
				var SUT = new PasswordBox
				{
					Password = password,
					FlowDirection = flowDirection,
					Width = 300,
					FontSize = 20,
					Foreground = new SolidColorBrush(Colors.Red),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
				};
				var host = new Grid { Width = 340, Height = 80, Background = new SolidColorBrush(Colors.White), FlowDirection = FlowDirection.LeftToRight };
				host.Children.Add(SUT);
				try
				{
					await UITestHelper.Load(host);
					await UITestHelper.WaitForRender();
					var (min, max) = GetRedInkRange(await UITestHelper.ScreenShot(host));
					Assert.IsTrue(max > min, $"No bullets for password {results.Count}.");

					var caret = -1;
					if (withCaret)
					{
						SUT.Focus(FocusState.Programmatic);
						caret = await FindCaretColumn(host, SUT, min - 15, max + 15);
					}

					results.Add((min, max, caret));
				}
				finally
				{
					WindowHelper.WindowContent = null;
				}
			}

			return results;
		}

		private static void AssertSameForEveryScript(List<(int min, int max, int caret)> results)
		{
			var summary = string.Join(", ", results.Select(r => $"ink {r.min}..{r.max} caret {r.caret}"));
			foreach (var result in results)
			{
				Assert.IsTrue(
					Math.Abs(result.min - results[0].min) <= 2 && Math.Abs(result.max - results[0].max) <= 2 && Math.Abs(result.caret - results[0].caret) <= 2,
					$"Latin, Arabic and Hebrew passwords of the same length are drawn differently: {summary}.");
			}
		}

		// The caret is the one column, near the text, inked over most of the box's inner height; it blinks, so look a few times.
		private static async Task<int> FindCaretColumn(Grid host, Control box, int fromX, int toX)
		{
			var bounds = box.TransformToVisual(host).TransformBounds(new Rect(0, 0, box.ActualWidth, box.ActualHeight));
			var top = (int)bounds.Top + 4;
			var bottom = (int)bounds.Bottom - 4;
			for (var attempt = 0; attempt < 20; attempt++)
			{
				await WindowHelper.WaitForIdle();
				var bitmap = await UITestHelper.ScreenShot(host);
				for (var x = Math.Max(fromX, 0); x <= Math.Min(toX, bitmap.Width - 1); x++)
				{
					var inked = 0;
					for (var y = top; y < Math.Min(bottom, bitmap.Height); y++)
					{
						var pixel = bitmap.GetPixel(x, y);
						if (pixel.A > 128 && (pixel.R < 160 || pixel.G < 160 || pixel.B < 160))
						{
							inked++;
						}
					}

					if (inked >= (bottom - top) / 2)
					{
						return x;
					}
				}

				await Task.Delay(100);
			}

			Assert.Fail($"No caret between x = {fromX} and {toX}.");
			return -1;
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.RightToLeft, InkSide.Right)]
		public async Task When_PasswordBox_Revealed_Text_Side(string text, FlowDirection flowDirection, InkSide expected)
		{
			var SUT = new PasswordBox { Password = text, PasswordRevealMode = PasswordRevealMode.Visible, FlowDirection = flowDirection };

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// WinUI aligns a PasswordBox's text to its start; Uno's detects from the content, as it did before.
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.RightToLeft, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(HebrewText, FlowDirection.LeftToRight, InkSide.Right)]
		public async Task When_PasswordBox_Revealed_Text_Detects(string text, FlowDirection flowDirection, InkSide expected)
		{
			var SUT = new PasswordBox { Password = text, PasswordRevealMode = PasswordRevealMode.Visible, FlowDirection = flowDirection };

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text));
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic or Hebrew glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, InkSide.Left)]
		[DataRow(LatinText, FlowDirection.RightToLeft, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, InkSide.Right)]
		public async Task When_AutoSuggestBox_Text_Detects(string text, FlowDirection flowDirection, InkSide expected)
		{
			var SUT = new AutoSuggestBox { Text = text, FlowDirection = flowDirection };

			Assert.AreEqual(expected, await GetTextInkSide(SUT, text));
			Assert.AreEqual(TextAlignment.DetectFromContent, FindDescendants<TextBox>(SUT).Single().TextAlignment);
		}

		[TestMethod]
		public async Task When_Editable_ComboBox_TextBox_Reports_DetectFromContent()
		{
			var SUT = new ComboBox { IsEditable = true };
			await UITestHelper.Load(SUT);

			Assert.AreEqual(TextAlignment.DetectFromContent, FindDescendants<TextBox>(SUT).Single().TextAlignment);
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// WinUI only draws the editable text while the ComboBox is being edited with keyboard focus.
		// tvOS has no system font with Arabic glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaTvOS)]
		[DataRow(LatinText, FlowDirection.LeftToRight, InkSide.Left)]
		[DataRow(LatinText, FlowDirection.RightToLeft, InkSide.Left)]
		[DataRow(ArabicText, FlowDirection.LeftToRight, InkSide.Right)]
		[DataRow(ArabicText, FlowDirection.RightToLeft, InkSide.Right)]
		public async Task When_Editable_ComboBox_Text_Detects(string text, FlowDirection flowDirection, InkSide expected)
		{
			var SUT = new ComboBox { IsEditable = true, FlowDirection = flowDirection };

			// The editable TextBox only takes space while the ComboBox has focus. Give it the size ComboBox gives it
			// then (EnsureTextBoxIsEnabled), rather than depend on window focus.
			var side = await GetTextInkSide(SUT, text, () =>
			{
				var textBox = FindDescendants<TextBox>(SUT).Single();
				textBox.Width = double.NaN;
				textBox.Height = double.NaN;
				textBox.Text = text;
			});

			Assert.AreEqual(expected, side);
		}

		[TestMethod]
		[RequiresScaling(1f)]
		// tvOS has no system font with Arabic glyphs.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
		public async Task When_NumberBox_TextBox_Stays_Left()
		{
			// LeftToRight only: WinUI draws a RightToLeft NumberBox's Left at its start.
			var SUT = new NumberBox { PlaceholderText = ArabicText };

			Assert.AreEqual(InkSide.Left, await GetPlaceholderInkSide(SUT, ArabicText));
			Assert.AreEqual(TextAlignment.Left, FindDescendants<TextBox>(SUT).Single().TextAlignment);
		}

		// The placeholder is a TextBlock bound to the TextBox's TextAlignment, so it lands where a TextBlock with
		// DetectFromContent does: on the left where the text engine reads DetectFromContent as Left, else detected.
		private static async Task<InkSide> GetExpectedPlaceholderSide(string text)
		{
			if (text == LatinText)
			{
				return InkSide.Left;
			}

			var reference = new TextBlock { Text = text, Width = 300, TextAlignment = TextAlignment.DetectFromContent };
			var side = await GetInkSide(reference, text, () => reference.Foreground = new SolidColorBrush(Colors.Red));
			Assert.AreNotEqual(InkSide.Center, side);
			return side;
		}

		private static Task<InkSide> GetTextInkSide(Control control, string text, Action afterLoad = null)
			=> GetInkSide(control, text, () =>
			{
				// A ComboBox's template gives its TextBox its own foreground.
				var textControl = control is ComboBox ? FindDescendants<TextBox>(control).Single() : control;
				textControl.Foreground = new SolidColorBrush(Colors.Red);
				afterLoad?.Invoke();
			});

		private static Task<InkSide> GetPlaceholderInkSide(Control control, string text)
			=> GetInkSide(control, text, () =>
			{
				var placeholder = FindDescendants<TextBlock>(control).Single(b => b.Name == "PlaceholderTextContentPresenter" && b.Text == text);
				placeholder.Foreground = new SolidColorBrush(Colors.Red);
			});

		// Where the ink lies within the box that holds the text: the control itself, or the TextBox in its template.
		private static async Task<InkSide> GetInkSide(FrameworkElement element, string text, Action colorInk)
		{
			await AssertGlyphsAvailable(text);

			element.Width = 300;
			element.HorizontalAlignment = HorizontalAlignment.Left;
			element.VerticalAlignment = VerticalAlignment.Top;
			if (element is Control control)
			{
				control.FontSize = 20;
			}
			else if (element is TextBlock textBlock)
			{
				textBlock.FontSize = 20;
			}

			var host = new Grid { Width = 340, Height = 80, Background = new SolidColorBrush(Colors.White), FlowDirection = FlowDirection.LeftToRight };
			host.Children.Add(element);
			try
			{
				await UITestHelper.Load(host);
				colorInk();
				await WindowHelper.WaitForIdle();
				await UITestHelper.WaitForRender();

				var (left, right) = GetTextArea(element is TextBox or PasswordBox or TextBlock ? element : FindDescendants<TextBox>(element).Single(), host);
				var bitmap = await UITestHelper.ScreenShot(host);
				var (min, max) = GetRedInkRange(bitmap);
				Assert.IsTrue(max > min, $"No ink for '{text}'.");

				// Both edges, so that a run in the wrong place can't pass for centered.
				var fromLeft = min - left;
				var fromRight = right - max;
				return (fromLeft, fromRight) switch
				{
					_ when fromLeft < 20 && fromRight > 60 => InkSide.Left,
					_ when fromRight < 20 && fromLeft > 60 => InkSide.Right,
					_ when Math.Abs(fromLeft - fromRight) < 20 => InkSide.Center,
					_ => throw new AssertFailedException($"Ink {min}..{max} for '{text}' is neither left, centered nor right in {left}..{right}."),
				};
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// The box's physical x range inside its border and padding, which are at the start of a RightToLeft box.
		private static (double left, double right) GetTextArea(FrameworkElement box, UIElement host)
		{
			var bounds = box.TransformToVisual(host).TransformBounds(new Rect(0, 0, box.ActualWidth, box.ActualHeight));
			if (box is not Control control)
			{
				return (bounds.Left, bounds.Right);
			}

			var start = control.Padding.Left + control.BorderThickness.Left;
			var end = control.Padding.Right + control.BorderThickness.Right;
			return control.FlowDirection == FlowDirection.RightToLeft
				? (bounds.Left + end, bounds.Right - start)
				: (bounds.Left + start, bounds.Right - end);
		}

		private static (int min, int max) GetRedInkRange(RawBitmap bitmap)
		{
			int min = int.MaxValue, max = -1;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					// Anti-aliased red over white: strong red, weak green and blue.
					if (pixel.A > 128 && pixel.R > 150 && pixel.G < 120 && pixel.B < 120)
					{
						min = Math.Min(min, x);
						max = Math.Max(max, x);
					}
				}
			}

			return (min, max);
		}

		// The sides are the point of these tests, and they only mean something when the script's glyphs are drawn.
		// A text made of unassigned code points (U+0378) is drawn with the missing-glyph box; the same width means
		// no installed font has the script's glyphs either.
		private static async Task AssertGlyphsAvailable(string text)
		{
			if (text == LatinText)
			{
				return;
			}

			var real = new TextBlock { Text = text, FontSize = 20 };
			var missing = new TextBlock { Text = new string(text.Select(c => c == ' ' ? ' ' : '\u0378').ToArray()), FontSize = 20 };
			await UITestHelper.Load(new StackPanel { Children = { real, missing } });
			try
			{
				// The browser fetches its fallback font on demand, and lays the text out again once it is there.
				var deadline = DateTime.UtcNow.AddSeconds(10);
				while (Math.Abs(real.ActualWidth - missing.ActualWidth) <= 1)
				{
					if (DateTime.UtcNow > deadline)
					{
						Assert.Inconclusive($"No font with glyphs for '{text}' on this platform.");
					}

					await Task.Delay(50);
					await WindowHelper.WaitForIdle();
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
			where T : DependencyObject
		{
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is T match)
				{
					yield return match;
				}

				foreach (var descendant in FindDescendants<T>(child))
				{
					yield return descendant;
				}
			}
		}
	}
}
