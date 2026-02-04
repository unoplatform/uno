using System;
using System.Linq;
using System.Threading.Tasks;
using Uno.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using System.Collections.Generic;
using System.Drawing;
using SamplesApp.UITests;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Extensions;
using Combinatorial.MSTest;
using Uno.UI.Helpers;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Toolkit.DevTools.Input;

#if __SKIA__
using Microsoft.UI.Xaml.Data;
using SkiaSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
#endif

using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_TextBlock
	{
#if __ANDROID__
		[Ignore("Visually looks good, but fails :(")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		[TestMethod]
		[DataRow((ushort)400, FontStyle.Italic, FontStretch.Condensed, "ms-appx:///Assets/Fonts/OpenSans/OpenSans_Condensed-MediumItalic.ttf")]
		[DataRow((ushort)400, FontStyle.Normal, FontStretch.SemiCondensed, "ms-appx:///Assets/Fonts/OpenSans/OpenSans_SemiCondensed-Regular.ttf")]
		[DataRow((ushort)600, FontStyle.Normal, FontStretch.SemiCondensed, "ms-appx:///Assets/Fonts/OpenSans/OpenSans_SemiCondensed-SemiBold.ttf")]
		[DataRow((ushort)700, FontStyle.Normal, FontStretch.Normal, "ms-appx:///Assets/Fonts/OpenSans/OpenSans-Bold.ttf")]
		[DataRow((ushort)400, FontStyle.Normal, FontStretch.Normal, "ms-appx:///Assets/Fonts/OpenSans/OpenSans-Regular.ttf")]
		[DataRow((ushort)600, FontStyle.Normal, FontStretch.SemiCondensed, "ms-appx:///Assets/Fonts/OpenSans/OpenSans_SemiCondensed-SemiBold.ttf#Open Sans")]
		public async Task When_Font_Has_Manifest(ushort weight, FontStyle style, FontStretch stretch, string ttfFile)
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new TextBlock
			{
				Text = "Hello World!",
				FontSize = 18,
				FontStyle = style,
				FontStretch = stretch,
				FontWeight = new FontWeight(weight),
				FontFamily = new FontFamily("ms-appx:///Assets/Fonts/OpenSans/OpenSans.ttf"),
			};
			var SUTContainer = new Border()
			{
				Width = 200,
				Height = 100,
				Child = SUT
			};

			var expectedTB = new TextBlock
			{
				Text = "Hello World!",
				FontSize = 18,
				FontFamily = new FontFamily(ttfFile)
			};
			var expectedTBContainer = new Border()
			{
				Width = 200,
				Height = 100,
				Child = expectedTB
			};

			var differentTtf = "ms-appx:///Assets/Fonts/OpenSans/OpenSans-Bold.ttf";
			if (ttfFile == differentTtf)
			{
				differentTtf = "ms-appx:///Assets/Fonts/OpenSans/OpenSans-Regular.ttf";
			}

			var differentTB = new TextBlock
			{
				Text = "Hello World!",
				FontSize = 18,
				FontFamily = new FontFamily(differentTtf),
			};
			var differentTBContainer = new Border()
			{
				Width = 200,
				Height = 100,
				Child = differentTB
			};

			var sp = new StackPanel()
			{
				Children =
				{
					SUTContainer,
					expectedTBContainer,
					differentTBContainer,
				},
			};

			await UITestHelper.Load(sp);
			var actual = await UITestHelper.ScreenShot(SUTContainer);
			var expected = await UITestHelper.ScreenShot(expectedTBContainer);
			var different = await UITestHelper.ScreenShot(differentTBContainer);
			await ImageAssert.AreEqualAsync(actual, expected);
			await ImageAssert.AreNotEqualAsync(actual, different);
		}

#if __SKIA__
		[TestMethod]
		// It looks like CI might not have any installed fonts with Chinese characters which could cause the test to fail
		[Ignore("Fails on CI")]
		public async Task Check_FontFallback()
		{
			var SUT = new TextBlock { Text = "示例文本", FontSize = 24 };
			var skFont = FontDetailsCache.GetFont(SUT.FontFamily?.Source, (float)SUT.FontSize, SUT.FontWeight, SUT.FontStretch, SUT.FontStyle).details.SKFont;
			Assert.IsFalse(skFont.ContainsGlyph(SUT.Text[0]));

			var fallbackFont = SKFontManager.Default.MatchCharacter(SUT.Text[0]);

			Assert.IsTrue(fallbackFont.ContainsGlyph(SUT.Text[0]));

			var expected = new TextBlock { Text = "示例文本", FontSize = 24, FontFamily = new FontFamily(fallbackFont.FamilyName) };

			await UITestHelper.Load(SUT);
			var screenshot1 = await UITestHelper.ScreenShot(SUT);

			await UITestHelper.Load(expected);
			var screenshot2 = await UITestHelper.ScreenShot(expected);

			Assert.AreEqual(screenshot2.Width, screenshot1.Width);
			Assert.AreEqual(screenshot2.Height, screenshot1.Height);

			await ImageAssert.AreSimilarAsync(screenshot1, screenshot2, imperceptibilityThreshold: 0.15);
		}

		[TestMethod]
		// Different platforms will resolve SKFontManager.Default.MatchCharacter to different fonts
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task Check_FontFallback_Shaping()
		{
			var SUT = new TextBlock
			{
				Text = "اللغة العربية",
				FontSize = 24,
				LineHeight = 34,
			};

			var skFont = FontDetailsCache.GetFont(SUT.FontFamily?.Source, (float)SUT.FontSize, SUT.FontWeight, SUT.FontStretch, SUT.FontStyle).details.SKFont;
			Assert.IsFalse(skFont.ContainsGlyph(SUT.Text[0]));

			var fallbackFont = SKFontManager.Default.MatchCharacter(SUT.Text[0]);

			Assert.IsTrue(fallbackFont.ContainsGlyph(SUT.Text[0]));

			var expected = new TextBlock
			{
				Text = "اللغة العربية",
				FontSize = 24,
				FontFamily = new FontFamily(fallbackFont.FamilyName),
				LineHeight = 34,
			};

			await UITestHelper.Load(SUT);
			var screenshot1 = await UITestHelper.ScreenShot(SUT);

			await UITestHelper.Load(expected);
			var screenshot2 = await UITestHelper.ScreenShot(expected);

			// we tolerate a 2 pixels difference between the bitmaps due to font differences
			await ImageAssert.AreSimilarAsync(screenshot1, screenshot2, imperceptibilityThreshold: 0.18, resolutionTolerance: 2);
		}
#endif

		[TestMethod]
		public async Task Check_TextDecorations_Binding()
		{
			var SUT = new TextDecorationsBinding();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(TextDecorations.Strikethrough, SUT.textBlock1.TextDecorations);
			Assert.AreEqual(TextDecorations.Underline, SUT.textBlock2.TextDecorations);
			Assert.AreEqual(TextDecorations.Strikethrough, SUT.textBlock3.TextDecorations);
			Assert.AreEqual(TextDecorations.None, SUT.textBlock4.TextDecorations);
			Assert.AreEqual(TextDecorations.Strikethrough, SUT.textBlock5.TextDecorations);
			Assert.AreEqual(TextDecorations.None, SUT.textBlock6.TextDecorations);
		}

		[TestMethod]
		public async Task When_NewLine_After_Tab()
		{
			var SUT = new TextBlock
			{
				Text = "\t\r",
				FontFamily = new FontFamily("ms-appx:///Assets/Fonts/CascadiaCode-Regular.ttf"),
				Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red)
			};

			await UITestHelper.Load(SUT);

#if __SKIA__
			var segments = ((Run)SUT.Inlines.Single()).Segments;
			Assert.AreEqual(2, segments.Count);
			Assert.IsTrue(segments[0].IsTab);
			Assert.AreEqual("\r", segments[1].Text.ToString());
#endif

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var screenshot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.DoesNotHaveColorInRectangle(screenshot, new Rectangle(0, 0, screenshot.Width, screenshot.Height), Microsoft.UI.Colors.Red, tolerance: 15);
		}

		[TestMethod]
		public void Check_ActualWidth_After_Measure()
		{
			var SUT = new TextBlock { Text = "Some text" };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.IsTrue(SUT.DesiredSize.Width > 0);
			Assert.IsTrue(SUT.DesiredSize.Height > 0);

			// For simplicity, currently we don't insist on a specific value here. The exact details of text measurement are highly
			// platform-specific, and additionally on UWP the ActualWidth and DesiredSize.Width are not exactly the same, a subtlety Uno
			// currently doesn't try to replicate.
			Assert.IsTrue(SUT.ActualWidth > 0);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		[TestMethod]
		public void Check_ActualWidth_After_Measure_Collapsed()
		{
			var SUT = new TextBlock { Text = "Some text", Visibility = Visibility.Collapsed };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.AreEqual(0, SUT.DesiredSize.Width);
			Assert.AreEqual(0, SUT.DesiredSize.Height);

			Assert.AreEqual(0, SUT.ActualWidth);
			Assert.AreEqual(0, SUT.ActualHeight);
		}

		[TestMethod]
		public void Check_Text_When_Having_Inline_Text_In_Span()
		{
			var SUT = new InlineTextInSpan();
			var panel = (StackPanel)SUT.Content;
			var span = (Span)((TextBlock)panel.Children.Single()).Inlines.Single();
			var inlines = span.Inlines;
			Assert.AreEqual(3, inlines.Count);
			Assert.AreEqual("Where ", ((Run)inlines[0]).Text);
			Assert.AreEqual("did", ((Run)((Italic)inlines[1]).Inlines.Single()).Text);
			Assert.AreEqual(" my text go?", ((Run)inlines[2]).Text);
		}

		[TestMethod]
		public void When_Null_FontFamily()
		{
			var SUT = new TextBlock { Text = "Some text", FontFamily = null };
			WindowHelper.WindowContent = SUT;
			SUT.Measure(new Size(1000, 1000));
		}

		[TestMethod]
		public async Task Check_Single_Character_Run_With_Wrapping_Constrained()
		{
			var SUT = new TextBlock
			{
				Inlines = {
					new Run { Text = "", FontSize = 16, CharacterSpacing = 18 }
				},
				TextWrapping = TextWrapping.Wrap,
				FontSize = 16,
				CharacterSpacing = 18
			};

			WindowHelper.WindowContent = new Border
			{
				Width = 10,
				Height = 10,
				Child = SUT
			};

			await WindowHelper.WaitForIdle();

			Assert.AreNotEqual(0, SUT.DesiredSize.Width);
			Assert.AreNotEqual(0, SUT.DesiredSize.Height);

			Assert.AreNotEqual(0, SUT.ActualWidth);
			Assert.AreNotEqual(0, SUT.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Multiline_Wrapping_LongWord_Then_Space_Then_Word()
		{
			var SUT = new TextBlock
			{
				Width = 150,
				TextWrapping = TextWrapping.Wrap,
				Text = "abcdefghijklmnopqrstuvwxyzabcdefg"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text += " a";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(height, SUT.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Multiline_Wrapping_LeadingSpaces()
		{
			var SUT = new TextBlock
			{
				Width = 150,
				TextWrapping = TextWrapping.Wrap,
				Text = "initial"
			};

			WindowHelper.WindowContent = new Border
			{
				Child = SUT,
				BorderBrush = new SolidColorBrush(Colors.Pink),
				BorderThickness = new Thickness(1)
			};

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text = new string(' ', 120);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(height, SUT.ActualHeight);
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("Fails")]
#endif
		public async Task When_Multiline_Wrapping_Text_Ends_In_Too_Many_Spaces()
		{
			var SUT = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				Text = "hello world"
			};

			WindowHelper.WindowContent = new Border
			{
				Width = 150,
				Child = SUT
			};

			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text = "mmmmmmmmm               ";
			await WindowHelper.WaitForIdle();

			// Trailing space shouldn't wrap
			Assert.AreEqual(height, SUT.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("Only skia handled trailing newlines correctly for now.")]
#endif
		public async Task When_Text_Ends_In_CarriageReturn()
		{
			var SUT0 = new TextBlock();
			var SUT1 = new TextBlock();

			SUT0.Text = "text";
			SUT1.Text = "text\r";

			var sp = new StackPanel
			{
				Children =
				{
					new Border
					{
						Child = SUT0,
						BorderBrush = new SolidColorBrush(Colors.Chartreuse)
					},
					new Border
					{
						Child = SUT1,
						BorderBrush = new SolidColorBrush(Colors.Pink)
					}
				}
			};

			WindowHelper.WindowContent = sp;
			await WindowHelper.WaitForIdle();

			SUT1.ActualHeight.Should().BeGreaterThan(SUT0.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("Only skia handled trailing newlines correctly for now.")]
#endif
		public async Task When_Text_Ends_In_CarriageReturn2()
		{
			var SUT0 = new TextBlock();
			var SUT1 = new TextBlock();
			var SUT2 = new TextBlock();
			var SUT3 = new TextBlock();

			SUT1.Text = "\r";
			SUT2.Text = "\r\r";
			SUT3.Text = "\r\r\r";

			var sp = new StackPanel
			{
				Children =
				{
					new Border
					{
						Child = SUT0,
						BorderBrush = new SolidColorBrush(Colors.Chartreuse)
					},
					new Border
					{
						Child = SUT1,
						BorderBrush = new SolidColorBrush(Colors.Pink)
					},
					new Border
					{
						Child = SUT2,
						BorderBrush = new SolidColorBrush(Colors.Brown)
					},
					new Border
					{
						Child = SUT3,
						BorderBrush = new SolidColorBrush(Colors.Yellow)
					}
				}
			};

			WindowHelper.WindowContent = sp;
			await WindowHelper.WaitForIdle();

			SUT1.ActualHeight.Should().BeGreaterThan(SUT0.ActualHeight);
			SUT2.ActualHeight.Should().BeGreaterThan(SUT1.ActualHeight);
			SUT3.ActualHeight.Should().BeGreaterThan(SUT2.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("Only skia handled trailing newlines correctly for now.")]
#endif
		public async Task When_Text_Ends_In_Return()
		{
			var SUT = new TextBlock { Text = "hello world" };

			WindowHelper.WindowContent = new Border { Child = SUT };

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var height = SUT.ActualHeight;

			SUT.Text += "\r";
			await WindowHelper.WaitForIdle();

			SUT.ActualHeight.Should().BeGreaterThan(height * 1.5);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("Only skia handled trailing newlines correctly for now.")]
#endif
		public async Task When_Text_Ends_In_LineBreak()
		{
			var SUT0 = new TextBlock();
			var SUT1 = new TextBlock();

			SUT0.Text = "text";
			SUT1.Inlines.Add(new Run { Text = "text" });
			SUT1.Inlines.Add(new LineBreak());

			var sp = new StackPanel
			{
				Children =
				{
					new Border
					{
						Child = SUT0,
						BorderBrush = new SolidColorBrush(Colors.Chartreuse)
					},
					new Border
					{
						Child = SUT1,
						BorderBrush = new SolidColorBrush(Colors.Pink)
					}
				}
			};

			WindowHelper.WindowContent = sp;
			await WindowHelper.WaitForIdle();

			SUT1.ActualHeight.Should().BeGreaterThan(SUT0.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("Only skia handled trailing newlines correctly for now.")]
#endif
		public async Task When_Text_Ends_In_LineBreak2()
		{
			var SUT0 = new TextBlock();
			var SUT1 = new TextBlock();
			var SUT2 = new TextBlock();
			var SUT3 = new TextBlock();

			SUT1.Inlines.Add(new LineBreak());

			SUT2.Inlines.Add(new LineBreak());
			SUT2.Inlines.Add(new LineBreak());

			SUT3.Inlines.Add(new LineBreak());
			SUT3.Inlines.Add(new LineBreak());
			SUT3.Inlines.Add(new LineBreak());

			var sp = new StackPanel
			{
				Children =
				{
					new Border
					{
						Child = SUT0,
						BorderBrush = new SolidColorBrush(Colors.Chartreuse)
					},
					new Border
					{
						Child = SUT1,
						BorderBrush = new SolidColorBrush(Colors.Pink)
					},
					new Border
					{
						Child = SUT2,
						BorderBrush = new SolidColorBrush(Colors.Brown)
					},
					new Border
					{
						Child = SUT3,
						BorderBrush = new SolidColorBrush(Colors.Yellow)
					}
				}
			};

			WindowHelper.WindowContent = sp;
			await WindowHelper.WaitForIdle();

			SUT1.ActualHeight.Should().BeGreaterThan(SUT0.ActualHeight);
			SUT2.ActualHeight.Should().BeGreaterThan(SUT1.ActualHeight);
			SUT3.ActualHeight.Should().BeGreaterThan(SUT2.ActualHeight);
		}

		[TestMethod]
		public void When_Inlines_XamlRoot()
		{
			var SUT = new InlineTextInSpan();
			WindowHelper.WindowContent = SUT;
			var panel = (StackPanel)SUT.Content;
			var span = (Span)((TextBlock)panel.Children.Single()).Inlines.Single();
			var inlines = span.Inlines;
			foreach (var inline in inlines)
			{
				Assert.AreEqual(SUT.XamlRoot, inline.XamlRoot);
			}
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_Inlines_Transitively_Change()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}
			var SUT = new TextBlock();

			await UITestHelper.Load(SUT, tb => tb.IsLoaded);

			var span = new Span();
			SUT.Inlines.Add(span);
			span.Inlines.Add(new Run() { Text = "text" });

			await UITestHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(SUT);
			ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), ((SolidColorBrush)Uno.UI.Xaml.Media.DefaultBrushes.TextForegroundBrush).Color);
		}
#endif

#if __WASM__
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19380")]
		public async Task When_Changing_Text_Through_Inlines()
		{
			var SUT = new TextBlock { Text = "Initial Text" };
			await Uno.UI.RuntimeTests.Helpers.UITestHelper.Load(SUT);
			var width = Uno.UI.Xaml.WindowManagerInterop.GetClientViewSize(SUT.HtmlId).clientSize.Width;

			SUT.Inlines.Clear();
			SUT.Inlines.Add(new Run { Text = "Updated Text" });

			await Uno.UI.RuntimeTests.Helpers.UITestHelper.WaitForIdle();

			Uno.UI.Xaml.WindowManagerInterop.GetClientViewSize(SUT.HtmlId).clientSize.Width.Should().BeApproximately(width, precision: width * 0.4);
		}
#endif

		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		[DataRow("ms-appx:///Assets/Fonts/CascadiaCode-Regular.ttf")]
		[DataRow("ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/Roboto-Regular.ttf")]
		public async Task When_FontFamily_Changed(string font)
		{
			var SUT = new TextBlock { Text = "abcd" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var size = new Size(1000, 1000);
			SUT.Measure(size);

			var originalSize = SUT.DesiredSize;

			Assert.AreNotEqual(0, SUT.DesiredSize.Width);
			Assert.AreNotEqual(0, SUT.DesiredSize.Height);

			SUT.FontFamily = new FontFamily(font);

			int counter = 3;

			do
			{
				await WindowHelper.WaitForIdle();
				await Task.Delay(100);

				SUT.InvalidateMeasure();
			}
			while (SUT.DesiredSize == originalSize && counter-- > 0);

			Assert.AreNotEqual(originalSize, SUT.DesiredSize);
		}

		[TestMethod]
#if !__ANDROID__
		[Ignore("Android-only test for AndroidAssets backward compatibility")]
#endif
		public async Task When_FontFamily_In_AndroidAsset()
		{
			var SUT = new TextBlock { Text = "\xE102\xE102\xE102\xE102\xE102" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var size = new Size(1000, 1000);
			SUT.Measure(size);

			var originalSize = SUT.DesiredSize;

			Assert.AreNotEqual(0, SUT.DesiredSize.Width);
			Assert.AreNotEqual(0, SUT.DesiredSize.Height);

			SUT.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/SymbolsRuntimeTest02.ttf#SymbolsRuntimeTest02");

			for (int i = 0; i < 3; i++)
			{
				await WindowHelper.WaitForIdle();
				await Task.Delay(100);
				SUT.InvalidateMeasure();

				if (SUT.DesiredSize != originalSize) break;
			}

			Assert.AreNotEqual(originalSize, SUT.DesiredSize);
		}

		[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_SolidColorBrush_With_Opacity()
		{
			var SUT = new TextBlock
			{
				Text = "••••••••",
				FontSize = 24,
				Foreground = new SolidColorBrush(Colors.Red) { Opacity = 0.5 },
			};

			await UITestHelper.Load(SUT);
			var bitmap = await UITestHelper.ScreenShot(SUT);

			ImageAssert.HasColorInRectangle(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), Colors.Red.WithOpacity(.5));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextWrapping_Changed()
		{
			var SUT = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				Text = "This is Long Text! This is Long Text! This is Long Text! This is Long Text! This is Long Text! This is Long Text! ",
			};
			StackPanel panel = new StackPanel
			{
				Width = 100,
				Children =
				{
					SUT
				}
			};
			await UITestHelper.Load(panel);
			var height1 = SUT.ActualHeight;
			SUT.TextWrapping = TextWrapping.NoWrap;
			await Task.Delay(500);
			var height2 = SUT.ActualHeight;
			Assert.AreNotEqual(height1, height2);
		}

		[TestMethod]
#if !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_Text_Wrapped_At_LineBreak()
		{
			var tb1 = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				Text = "Line1 Line2\r\nLine3",
				Width = 40,
				Foreground = new SolidColorBrush(Colors.Red)
			};
			var tb2 = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				Text = "Line1 Line2 Line3",
				Width = 40,
				Foreground = new SolidColorBrush(Colors.Red)
			};

			await UITestHelper.Load(new StackPanel { Children = { tb1, tb2 } });
			Assert.AreEqual(tb1.ActualHeight, tb2.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Empty_TextBlock_Measure()
		{
			var container = new Grid()
			{
				Height = 200,
				Width = 200,
			};
			var SUT = new TextBlock { Text = "" };
			container.Children.Add(SUT);
			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitFor(() => SUT.DesiredSize != default);

#if !__WASM__ // Disabled due to #14231
			Assert.AreEqual(0, SUT.DesiredSize.Width);
#endif
			Assert.IsTrue(SUT.DesiredSize.Height > 0);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/289")]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("Layout logic forces DesiredSize to be smaller than availableSize, which prevents us from fixing the behaviour to match wasm and skia.")]
#endif
		[DataRow(TextTrimming.None)]
#if __WASM__
		[DataRow(TextTrimming.Clip)]
		[DataRow(TextTrimming.CharacterEllipsis)]
		[DataRow(TextTrimming.WordEllipsis)]
#endif
		public async Task When_Text_Does_Not_Fit(TextTrimming trimming)
		{
			var lv = new ListView()
			{
				Width = 200
			};
			ScrollViewer.SetHorizontalScrollBarVisibility(lv, ScrollBarVisibility.Visible);
			ScrollViewer.SetHorizontalScrollMode(lv, ScrollMode.Enabled);
			var SUT = new TextBlock
			{
				Text = "text that is a lot longer than the given bounds",
				TextTrimming = trimming
			};
			lv.Items.Add(SUT);
			await UITestHelper.Load(lv);

			if (trimming is TextTrimming.None)
			{
				lv.FindFirstDescendant<ScrollViewer>().ScrollableWidth.Should().BeGreaterThan(50);
			}
			else
			{
				// Not necessarily zero because of measuring inaccuracies
				lv.FindFirstDescendant<ScrollViewer>().ScrollableWidth.Should().BeLessThan(5);
			}
		}

#if !__APPLE_UIKIT__ // Line height is not supported on iOS
		[TestMethod]
		public async Task When_Empty_TextBlock_LineHeight_Override()
		{
			var container = new Grid()
			{
				Height = 200,
				Width = 200,
			};
			var SUT = new TextBlock { Text = "", LineHeight = 100 };
			container.Children.Add(SUT);
			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitFor(() => SUT.DesiredSize != default);

#if !__WASM__ // Disabled due to #14231
			Assert.AreEqual(0, SUT.DesiredSize.Width);
#endif
			Assert.AreEqual(100, SUT.DesiredSize.Height);
		}
#endif

		[TestMethod]
		public async Task When_Empty_TextBlocks_Stacked()
		{
			var container = new StackPanel();
			for (int i = 0; i < 3; i++)
			{
				container.Children.Add(new TextBlock { Text = "" });
			}

			container.Children.Add(new TextBlock { Text = "Some text" });

			for (int i = 0; i < 3; i++)
			{
				container.Children.Add(new TextBlock { Text = "" });
			}

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			foreach (var child in container.Children)
			{
				await WindowHelper.WaitFor(() => child.DesiredSize != default);
			}

			// Get the transform of the top left of the container
			var previousTransform = container.TransformToVisual(null);
			var previousOrigin = previousTransform.TransformPoint(new Point(0, 0));

			for (int i = 1; i < container.Children.Count; i++)
			{
				// Get the same for SUT
				var textBlockTransform = container.Children[i].TransformToVisual(null);
				var textBlockOrigin = textBlockTransform.TransformPoint(new Point(0, 0));

				Assert.AreEqual(previousOrigin.X, textBlockOrigin.X);
				Assert.IsTrue(previousOrigin.Y < textBlockOrigin.Y);

				previousOrigin = textBlockOrigin;
			}
		}

#if __SKIA__
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno.hotdesign/issues/4327")]
		public async Task When_Bound_To_TextBox_Text()
		{
			var textblock = new TextBlock { Foreground = new SolidColorBrush(Colors.Red) };
			var textbox = new TextBox { Text = "0", Foreground = new SolidColorBrush(Colors.Red) };

			textblock.SetBinding(TextBlock.TextProperty,
				new Binding { Source = textbox, Path = new PropertyPath("Text"), });

			await UITestHelper.Load(new StackPanel
			{
				textbox,
				textblock
			});

			for (int i = 0; i < 5; i++)
			{
				textbox.Text = (int.TryParse(textbox.Text, out var v) ? v + 2 : 0).ToString();
				await UITestHelper.WaitForIdle();
				var tb1 = await UITestHelper.ScreenShot(textbox.FindFirstChild<TextBlock>());
				var tb2 = await UITestHelper.ScreenShot(textblock);
				for (int y = 0; y < 20; y++)
				{
					for (int x = 0; x < 20; x++)
					{
						Assert.AreEqual(tb1.GetPixel(x, y), tb2.GetPixel(x, y));
					}
				}
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextTrimming()
		{
			var sut = new TextBlock
			{
				Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
				TextTrimming = TextTrimming.Clip,
			};
			var container = new Border
			{
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Colors.Red),
				Width = 100,
				Child = sut,
			};

			var states = new List<bool>();
			sut.IsTextTrimmedChanged += (s, e) => states.Add(sut.IsTextTrimmed);

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitForIdle(); // necessary on ios, since the container finished loading before the text is drawn

			Assert.IsTrue(sut.IsTextTrimmed, "IsTextTrimmed should be trimmed.");
			Assert.IsTrue(states.Count == 1 && states[0] == true, $"IsTextTrimmedChanged should only proc once for IsTextTrimmed=true. states: {(string.Join(", ", states) is string { Length: > 0 } tmp ? tmp : "(-empty-)")}");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextTrimmingNone()
		{
			var sut = new TextBlock
			{
				Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
				TextTrimming = TextTrimming.None,
			};
			var container = new Border
			{
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Colors.Red),
				Width = 100,
				Child = sut,
			};

			var states = new List<bool>();
			sut.IsTextTrimmedChanged += (s, e) => states.Add(sut.IsTextTrimmed);

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitForIdle(); // necessary on ios, since the container finished loading before the text is drawn

			Assert.IsFalse(sut.IsTextTrimmed, "IsTextTrimmed should not be trimmed.");
			Assert.AreEqual(0, states.Count, $"IsTextTrimmedChanged should not proc at all. states: {(string.Join(", ", states) is string { Length: > 0 } tmp ? tmp : "(-empty-)")}");
		}

		[TestMethod]
		public async Task When_Padding()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive("RenderTargetBitmap is not supported on this platform");
			}

			var SUT = new TextBlock
			{
				Text = "text",
				Padding = new Thickness(50),
				Foreground = new SolidColorBrush(Colors.Red),
			};
			await UITestHelper.Load(SUT);
			var screenshot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.DoesNotHaveColorInRectangle(screenshot, new Rectangle(0, 0, 50, 50), Colors.Red);
		}

		[TestMethod]
		public async Task When_Text_Contains_Tabs_Does_Not_Throw()
		{
			// This used to throw an exception when tab characters appeared at the end of an inline segment boundary
			// due to an infinite loop in the tab handling logic.
			await UITestHelper.Load(new TextBlock
			{
				Text = """
				       public class Program { // http://www.github.com/
				       	public static void Main(string[] args) {
				       		Console.WriteLine("Hello, World!");
				       	}
				       
				       	/*
				       	 * Things to Try:
				       	 * - Hover over the word 'Hit'
				       	 * - Hit F1 and Search for 'TestAction'
				       	 * - Press Ctrl+Enter
				       	 * - After using Ctrl+Enter, hit F5
				       	 * - Hit Ctrl+L
				       	 * - Hit Ctrl+U
				       	 * - Hit Ctrl+W
				       	 * - Type the letter 'c'
				       	 * - Type the word 'boo'
				       	 * - Type 'foreach' to see Snippet.
				       	 */
				       }
				       """
			});
		}

#if HAS_RENDER_TARGET_BITMAP
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21322")]
		public async Task When_Text_Set_By_Style_Setter()
		{
			var style = new Style(typeof(TextBlock));
			const string text = "Hello from style";
			style.Setters.Add(new Setter(TextBlock.TextProperty, text));
			var container = new StackPanel() { Spacing = 8, Padding = new Thickness(4) };
			var SUT = new TextBlock
			{
				Foreground = new SolidColorBrush(Colors.Red),
				Style = style
			};
			var duplicate = new TextBlock
			{
				Foreground = new SolidColorBrush(Colors.Red),
				Text = text
			};
			container.Children.Add(SUT);
			container.Children.Add(duplicate);

			await UITestHelper.Load(container);
			Assert.AreEqual("Hello from style", SUT.Text);

			// Verify the text is actually rendered
			var screenshot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.HasColorInRectangle(screenshot, new Rectangle(0, 0, screenshot.Width, screenshot.Height), Colors.Red);

			// Verify both TextBlocks render the same
			var screenshot2 = await UITestHelper.ScreenShot(duplicate);
			await ImageAssert.AreSimilarAsync(screenshot, screenshot2);
		}
#endif


#if __SKIA__
		[TestMethod]
		public async Task When_RenderTransform_Rearrange()
		{
			var sut = new TextBlock()
			{
				Text = "AsdAsd",
				RenderTransform = new CompositeTransform { ScaleX = 2, ScaleY = 2 },
			};

			await UITestHelper.Load(sut);

			var a = sut.Visual.TransformMatrix;

			sut.InvalidateArrange();
			await UITestHelper.WaitForIdle();

			var b = sut.Visual.TransformMatrix;

			Assert.AreEqual(a, b, "Visual.TransformMatrix should remain unchanged after re-arrange.");
		}
#endif

#if HAS_UNO // GetMouse is not available on WinUI
		#region IsTextSelectionEnabled

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_PointerDrag()
		{
			var SUT = new TextBlock
			{
				Text = "Hello world",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.MoveTo(bounds.GetCenter() with { X = bounds.Right });
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(SUT);

			// compare vertical slices to see if they have highlighted text in them or not
			for (var i = 0; i < 5; i++)
			{
				ImageAssert.DoesNotHaveColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
			// skip 5 for relaxed tolerance
			for (var i = 6; i < 10; i++)
			{
				ImageAssert.HasColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_TappedMouse_Then_ClearSelection()
		{
			var sut = new TextBlock
			{
				Text = "hello uno",
				IsTextSelectionEnabled = true,
			};

			var bounds = await UITestHelper.Load(sut);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			sut.SelectAll();
			Assert.AreEqual(sut.Text, sut.SelectedText);

			mouse.MoveTo(bounds.GetCenter());
			mouse.Press();
			mouse.Release();

			Assert.AreEqual("", sut.SelectedText);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_TappedFinger_Then_ClearSelection()
		{
			var sut = new TextBlock
			{
				Text = "hello uno",
				IsTextSelectionEnabled = true,
			};

			var bounds = await UITestHelper.Load(sut);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			sut.SelectAll();
			Assert.AreEqual(sut.Text, sut.SelectedText);

			finger.Press(bounds.GetCenter());
			finger.Release();

			Assert.AreEqual("", sut.SelectedText);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_DoubleTapped()
		{
			var SUT = new TextBlock
			{
				Text = "hello uno",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(SUT);

			// compare vertical slices to see if they have highlighted text in them or not
			for (var i = 0; i < 5; i++)
			{
				ImageAssert.HasColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
			// skip 5 for relaxed tolerance
			for (var i = 6; i < 10; i++)
			{
				ImageAssert.DoesNotHaveColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_Chunking_DoubleTapped()
		{
			var SUT = new TextBlock
			{
				Text = "Hello_world",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			// Double click within Hello. We should only find Hello selected without "_" or "world"
			mouse.MoveTo(new Point(bounds.X + bounds.Width / 4, bounds.GetCenter().Y));
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(SUT);

			// compare vertical slices to see if they have highlighted text in them or not
			for (var i = 0; i < 5; i++)
			{
				ImageAssert.HasColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
			// skip 5 for relaxed tolerance
			for (var i = 6; i < 10; i++)
			{
				ImageAssert.DoesNotHaveColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_Wrapping_DoubleTapped()
		{
			var SUT = new TextBlock
			{
				Width = 50,
				TextWrapping = TextWrapping.Wrap,
				Text = "Awordthatislongerthananentireline",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var bitmap = await UITestHelper.ScreenShot(SUT);

			ImageAssert.DoesNotHaveColorInRectangle(
				bitmap,
				new Rectangle(new System.Drawing.Point(), bitmap.Size),
				SUT.SelectionHighlightColor.Color);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			bitmap = await UITestHelper.ScreenShot(SUT);

			// first line selected
			ImageAssert.HasColorInRectangle(
				bitmap,
				new Rectangle(0, 0, bitmap.Width, bitmap.Height / 6),
				SUT.SelectionHighlightColor.Color);
			// last line selected
			ImageAssert.HasColorInRectangle(
				bitmap,
				new Rectangle(0, bitmap.Height * 5 / 6, bitmap.Width, bitmap.Height / 6),
				SUT.SelectionHighlightColor.Color);
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Requires authorization to access to the clipboard on WASM.")]
#endif
		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_IsTextSelectionEnabled_SurrogatePair_Copy()
		{
#if __SKIA__
			if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>())
			{
				Assert.Inconclusive("Platform does not support clipboard operations.");
			}
#endif

			var SUT = new TextBlock
			{
				Text = "🚫 Hello world",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Hello ", await Clipboard.GetContent()!.GetTextAsync());
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Requires authorization to access to the clipboard on WASM.")]
#endif
		// Clipboard is currently not available on skia-WASM
		// Flaky on Skia.iOS uno-private#795
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaIOS)]
		public async Task When_IsTextSelectionEnabled_CRLF()
		{
#if __SKIA__
			if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>())
			{
				Assert.Inconclusive("Platform does not support clipboard operations.");
			}
#endif

			var delayToAvoidDoubleTap = 600;
			var SUT = new TextBlock
			{
				Text = "FirstLine\r\n Second",
				IsTextSelectionEnabled = true,
				FontFamily = "Arial" // to remain consistent between platforms with different default fonts
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// move to the center of the first line
			mouse.MoveTo(SUT.GetAbsoluteBounds().Location + new Point(SUT.ActualWidth / 2, 5));
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await Task.Delay(delayToAvoidDoubleTap);

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("FirstLine", await Clipboard.GetContent()!.GetTextAsync());

			// move to the center of the second line
			mouse.MoveTo(SUT.GetAbsoluteBounds().Location + new Point(SUT.ActualWidth / 2, 25));
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await Task.Delay(delayToAvoidDoubleTap);

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Second", await Clipboard.GetContent()!.GetTextAsync());

			// move to the start of the second line
			mouse.MoveTo(SUT.GetAbsoluteBounds().Location + new Point(0, 25));
			await WindowHelper.WaitForIdle();

			// double tap
			mouse.Press();
			mouse.Release();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(" ", await Clipboard.GetContent()!.GetTextAsync());
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !__SKIA__
		[Ignore("The context menu is only implemented on skia.")]
#endif
		[CombinatorialData]
		public async Task When_TextBlock_RightTapped(bool isTextSelectionEnabled)
		{
			using var _ = Disposable.Create(() => VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot));
			var SUT = new TextBlock
			{
				Text = "Hello world",
				IsTextSelectionEnabled = isTextSelectionEnabled,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(isTextSelectionEnabled ? 1 : 0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_IsTextSelectionEnabled_Keyboard_SelectAll_Copy()
		{
#if __SKIA__
			if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>())
			{
				Assert.Inconclusive("Platform does not support clipboard operations.");
			}
#endif

			var SUT = new TextBlock
			{
				Text = "Hello world",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var mod = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;
			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.A, mod));
			await WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(SUT);

			// compare vertical slices to see if they have highlighted text in them or not
			for (var i = 0; i < 10; i++)
			{
				ImageAssert.HasColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}

			SUT.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.C, mod));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT.Text, await Clipboard.GetContent()!.GetTextAsync());
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_ContextMenu_SelectAll()
		{
			var SUT = new TextBlock
			{
				Text = "Hello world",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await WindowHelper.WaitForIdle();

			mouse.PressRight();
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			mouse.MoveBy(10, 10); // should be over first menu item now
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(SUT);

			// compare vertical slices to see if they have highlighted text in them or not
			for (var i = 0; i < 10; i++)
			{
				ImageAssert.HasColorInRectangle(
					bitmap,
					new Rectangle(bitmap.Width * i / 10, 0, bitmap.Width / 10, bitmap.Height),
					SUT.SelectionHighlightColor.Color);
			}
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !__SKIA__
		[Ignore("The context menu is only implemented on skia.")]
#endif
		// Clipboard is currently not available on skia-WASM
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_IsTextSelectionEnabled_ContextMenu_Copy()
		{
#if __SKIA__
			if (!Uno.Foundation.Extensibility.ApiExtensibility.IsRegistered<Uno.ApplicationModel.DataTransfer.IClipboardExtension>())
			{
				Assert.Inconclusive("Platform does not support clipboard operations.");
			}
#endif

			var SUT = new TextBlock
			{
				Text = "Hello world",
				IsTextSelectionEnabled = true,
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = SUT.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			mouse.Press();
			mouse.MoveTo(bounds.GetCenter() with { X = bounds.Right });
			mouse.Release();
			await WindowHelper.WaitForIdle();

			mouse.PressRight(bounds.GetCenter());
			mouse.ReleaseRight();
			await WindowHelper.WaitForIdle();

			mouse.MoveBy(10, 10); // should be over first menu item now
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("world", await Clipboard.GetContent()!.GetTextAsync());
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_TouchScroll_Then_DoesNotSelectText()
		{
			TextBlock sut;
			var root = new ScrollViewer
			{
				Width = 150,
				Height = 300,
				IsScrollInertiaEnabled = false,
				Content = sut = new TextBlock
				{
					Text = Enumerable.Range(0, 4096).Select(i => $"Hello uno #{i:D4}!").JoinBy(" "),
					TextWrapping = TextWrapping.WrapWholeWords,
					IsTextSelectionEnabled = true,
				}
			};

			var bounds = await UITestHelper.Load(root);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Drag(
				from: bounds.GetCenter(),
				to: new(bounds.GetCenter().X, bounds.GetCenter().Y - 300));

			Assert.AreEqual("", sut.SelectedText);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_IsTextSelectionEnabled_TouchScroll_Then_DoesNotAlterSelection()
		{
			TextBlock sut;
			var root = new ScrollViewer
			{
				Width = 150,
				Height = 300,
				IsScrollInertiaEnabled = false,
				Content = sut = new TextBlock
				{
					Text = Enumerable.Range(0, 4096).Select(i => $"Hello uno #{i:D4}!").JoinBy(" "),
					TextWrapping = TextWrapping.WrapWholeWords,
					IsTextSelectionEnabled = true,
				}
			};

			var bounds = await UITestHelper.Load(root);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using (var mouse = injector.GetMouse()) // We use mouse to select text as currently we do not support selection using touch
			{
				// Drag horizontally to select some text
				mouse.Drag(
					from: new(bounds.X + 5, bounds.GetCenter().Y),
					to: new(bounds.Right - 5, bounds.GetCenter().Y));
			}

			var selectedText = sut.SelectedText;
			Assert.AreNotEqual("", sut.SelectedText);

			// Scroll vertically
			using (var finger = injector.GetFinger())
			{
				finger.Drag(
					from: bounds.GetCenter(),
					to: new(bounds.GetCenter().X, bounds.GetCenter().Y - 300));
			}
			Assert.AreEqual(selectedText, sut.SelectedText);
		}

#if __SKIA__
		[TestMethod]
		public async Task When_Focus_Changes_Selection_Is_Not_Shown()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive("RenderTargetBitmap is not supported on this platform");
			}

			var SUT = new TextBlock { Text = "Some text", IsTextSelectionEnabled = true };
			var focusBtn = new Button();
			await UITestHelper.Load(new StackPanel { Children = { SUT, focusBtn } });

			SUT.Focus(FocusState.Programmatic);
			SUT.SelectAll();
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForRender();
			var screenshot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.HasColorInRectangle(screenshot, new Rectangle(0, 0, screenshot.Width, screenshot.Height), ((SolidColorBrush)Uno.UI.Xaml.Media.DefaultBrushes.SelectionHighlightColor).Color);

			focusBtn.Focus(FocusState.Programmatic);
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForRender();
			var screenshot2 = await UITestHelper.ScreenShot(SUT);
			ImageAssert.DoesNotHaveColorInRectangle(screenshot2, new Rectangle(0, 0, screenshot2.Width, screenshot2.Height), ((SolidColorBrush)Uno.UI.Xaml.Media.DefaultBrushes.SelectionHighlightColor).Color);
		}
#endif

		#endregion
#endif

#if __SKIA__
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21264")]
		[DataRow("L")]
		[DataRow("R")]
		[DataRow("LL")]
		[DataRow("LR")]
		[DataRow("RL")]
		[DataRow("RR")]
		[DataRow("LLL")]
		[DataRow("LLR")]
		[DataRow("LRL")]
		[DataRow("LRR")]
		[DataRow("RLL")]
		[DataRow("RLR")]
		[DataRow("RRL")]
		[DataRow("RRR")]
		public async Task When_Layered_FlowDirection(string setup)
		{
			if (string.IsNullOrEmpty(setup) || setup.Any(c => c is not ('L' or 'R')))
			{
				throw new ArgumentException("setup must be a non-empty string containing only 'L' and 'R' characters");
			}

			FrameworkElement root = null;
			var textblock = new TextBlock { Text = "Asd" };

			// assign FlowDirection from in-most, and box each layer with border
			// LLR: Border R > Border L > TextBlock L
			foreach (var c in setup)
			{
				root = root is null ? textblock : new Border { Child = root };
				root.FlowDirection = Parse(c);
			}

			await UITestHelper.Load(root);

			Assert.AreEqual(1, textblock.Visual.TotalMatrix.M11);

			FlowDirection Parse(char c) => c switch
			{
				'L' => FlowDirection.LeftToRight,
				'R' => FlowDirection.RightToLeft,
				_ => throw new InvalidOperationException()
			};
		}
#endif
	}
}
