#nullable enable

using System;
using System.Globalization;
using Microsoft.UI.Xaml.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_Matrix3D
{
	[TestMethod]
	public void When_Formatted_Through_IFormattable()
	{
		Matrix3D matrix = new(
			1.5, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			2, 3, 4, 1);

		Assert.AreEqual(
			"1.5,0.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,0.0,1.0,0.0,2.0,3.0,4.0,1.0",
			((IFormattable)matrix).ToString("F1", CultureInfo.InvariantCulture));
		Assert.AreEqual(
			"1.5,0,0,0,0,1,0,0,0,0,1,0,2,3,4,1",
			matrix.ToString(CultureInfo.InvariantCulture));
		Assert.AreEqual("Identity", ((IFormattable)Matrix3D.Identity).ToString("F1", CultureInfo.InvariantCulture));
	}
}
