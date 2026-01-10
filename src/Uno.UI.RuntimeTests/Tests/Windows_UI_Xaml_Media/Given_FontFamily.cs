using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;
#if HAS_UNO
using Uno.UI;
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
	}
}
