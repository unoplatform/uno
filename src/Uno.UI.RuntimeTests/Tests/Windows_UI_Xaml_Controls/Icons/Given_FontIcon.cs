using System;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
public class Given_FontIcon
{
	[TestMethod]
	public void When_Defaults()
	{
		var fontIcon = new FontIcon();
		Assert.AreEqual("", fontIcon.Glyph);
		Assert.AreEqual(20.0, fontIcon.FontSize);
		Assert.IsTrue(fontIcon.FontFamily.Source.Contains("Icons", StringComparison.OrdinalIgnoreCase));
		Assert.AreEqual(FontWeights.Normal.Weight, fontIcon.FontWeight.Weight);
		Assert.AreEqual(FontStyle.Normal, fontIcon.FontStyle);
		Assert.IsTrue(fontIcon.IsTextScaleFactorEnabled);
	}
}
