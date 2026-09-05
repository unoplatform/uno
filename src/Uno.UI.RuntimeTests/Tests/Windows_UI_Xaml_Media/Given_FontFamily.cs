using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
#if HAS_UNO
using Uno.UI;
#endif
#if __SKIA__
using SkiaSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	public class Given_FontFamily
	{
#if __WASM__

		[TestMethod]
		public void With_Pure_Name()
		{
			var fontName = "My happy font";
			var fontFamily = new FontFamily(fontName);
			Assert.AreEqual(fontName, fontFamily.CssFontName);
		}

		[TestMethod]
		public void With_Path_And_Hash()
		{
			var fontName = "My font name";
			var fontFamilyPath = $"/Assets/Data/Fonts/MyFont.ttf#{fontName}";
			var fontFamily = new FontFamily(fontFamilyPath);
			Assert.IsTrue(fontFamily.CssFontName.StartsWith("font"));
		}

		[TestMethod]
		public void With_Path_Without_Hash()
		{
			var fontName = "FontName";
			var fontFamilyPath = $"/Assets/Data/Fonts/{fontName}.ttf";
			var fontFamily = new FontFamily(fontFamilyPath);
			Assert.IsTrue(fontFamily.CssFontName.StartsWith("font"));
		}

		[TestMethod]
		public void With_Msappx()
		{
			var fontName = "Msappx font name";
			var fontFamilyPath = $"ms-appx:///Assets/Data/Fonts/MyFont.ttf#{fontName}";
			var fontFamily = new FontFamily(fontFamilyPath);
			Assert.IsTrue(fontFamily.CssFontName.StartsWith("font"));
		}

		[TestMethod]
		public void Without_Path_With_Hash()
		{
			var fontName = "Font name";
			var fontFamilyPath = $"SomeFont.woff2#{fontName}";
			var fontFamily = new FontFamily(fontFamilyPath);
			Assert.IsTrue(fontFamily.CssFontName.StartsWith("font"));
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public void Symbol_Fonts_Fallback()
		{
			var fontFamily = new FontFamily("Segoe Fluent Icons");
			Assert.AreEqual(FeatureConfiguration.Font.SymbolsFont, fontFamily.Source);
			fontFamily = new FontFamily("Segoe UI Symbol");
			Assert.AreEqual(FeatureConfiguration.Font.SymbolsFont, fontFamily.Source);
			fontFamily = new FontFamily("Segoe MDL2 Assets");
			Assert.AreEqual(FeatureConfiguration.Font.SymbolsFont, fontFamily.Source);
			fontFamily = new FontFamily("Symbols");
			Assert.AreEqual(FeatureConfiguration.Font.SymbolsFont, fontFamily.Source);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Non_Existing_Font_Path_Does_Not_Break_Layout()
		{
			try
			{
				var outerStack = new StackPanel { Spacing = 20 };
				var fontFamilyPath = new Uri("ms-appx:///Assets/Data/Fonts/SomeDefinitelyNotRealFont.ttf#IDontExist");
				var textBlock = new TextBlock
				{
					Text = "Hello World",
					FontFamily = new FontFamily(fontFamilyPath.AbsoluteUri),
					TextAlignment = TextAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center
				};
				var rootGrid = new Grid();
				rootGrid.Children.Add(textBlock);
				outerStack.Children.Add(rootGrid);

				TestServices.WindowHelper.WindowContent = outerStack;

				// A non-existing font path must fall back to the default font instead of breaking layout.
				await TestServices.WindowHelper.WaitForLoaded(outerStack);

#if HAS_RENDER_TARGET_BITMAP
				var textBlockDuplicate = new TextBlock
				{
					Text = "Hello World",
					TextAlignment = TextAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center
				};
				var rootGridDuplicate = new Grid();
				rootGridDuplicate.Children.Add(textBlockDuplicate);
				outerStack.Children.Add(rootGridDuplicate);
				await TestServices.WindowHelper.WaitForLoaded(rootGridDuplicate);

				rootGrid.Width = 100;
				rootGrid.Height = 100;
				rootGridDuplicate.Width = 100;
				rootGridDuplicate.Height = 100;

				await TestServices.WindowHelper.WaitForIdle();

				var screenshotRootGrid = await UITestHelper.ScreenShot(rootGrid);
				var screenshotRootGridDuplicate = await UITestHelper.ScreenShot(rootGridDuplicate);
				await ImageAssert.AreSimilarAsync(screenshotRootGrid, screenshotRootGridDuplicate);
#endif
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Missing_Font_Falls_Back_To_MsAppx_Default()
		{
			var original = FeatureConfiguration.Font.DefaultTextFontFamily;
			try
			{
				var defaultFontUri = "ms-appx:///Assets/Fonts/OpenSans/OpenSans.ttf";
				FeatureConfiguration.Font.DefaultTextFontFamily = defaultFontUri;

				var probe = new TextBlock();

				// Load and cache the ms-appx default font so the fallback can resolve it synchronously.
				var defaultTypeface = (await FontDetailsCache
					.GetFont(defaultFontUri, (float)probe.FontSize, probe.FontWeight, probe.FontStretch, probe.FontStyle)
					.loadedTask).SKFont.Typeface;

				// Guards against the asset not being packaged, which would make the assertion meaningless.
				Assert.AreNotEqual(SKTypeface.FromFamilyName(null).FamilyName, defaultTypeface.FamilyName,
					"The ms-appx default font should have loaded as a distinct typeface.");

				// A non-existing font must fall back to the ms-appx default font, not the system font.
				var fallbackTypeface = (await FontDetailsCache
					.GetFont("ms-appx:///Assets/Fonts/ThisFontDoesNotExist.ttf#Nope", (float)probe.FontSize, probe.FontWeight, probe.FontStretch, probe.FontStyle)
					.loadedTask).SKFont.Typeface;

				Assert.AreEqual(defaultTypeface.FamilyName, fallbackTypeface.FamilyName,
					"A missing font should fall back to the ms-appx default font, not the system font.");
			}
			finally
			{
				FeatureConfiguration.Font.DefaultTextFontFamily = original;
			}
		}
#endif
	}
}
