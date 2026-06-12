#if __WASM__
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	public class Given_FontFamily
	{
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
	}
}
#endif
