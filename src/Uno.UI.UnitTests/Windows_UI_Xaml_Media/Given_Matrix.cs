#nullable enable

using System.Globalization;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_Matrix
{
	[TestMethod]
	public void When_Identity_ToString()
	{
		Assert.AreEqual("Identity", Matrix.Identity.ToString());
		Assert.AreEqual("Identity", Matrix.Identity.ToString(CultureInfo.InvariantCulture));
	}

	[TestMethod]
	public void When_ToString_InvariantCulture()
	{
		Matrix matrix = new(1.5, 0, 0, 2, 3, 4);

		Assert.AreEqual("1.5,0,0,2,3,4", matrix.ToString(CultureInfo.InvariantCulture));
	}

	[TestMethod]
	public void When_ToString_Comma_Decimal_Separator()
	{
		Matrix matrix = new(1.5, 0, 0, 2, 3, 4);

		Assert.AreEqual("1,5;0;0;2;3;4", matrix.ToString(new CultureInfo("de-DE")));
	}
}
