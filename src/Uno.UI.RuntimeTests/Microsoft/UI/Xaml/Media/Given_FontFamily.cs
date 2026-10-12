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
