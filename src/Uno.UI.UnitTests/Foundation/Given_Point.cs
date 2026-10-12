#nullable enable

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Foundation;

[TestClass]
public class Given_Point
{
	[TestMethod]
	public void When_Formatted()
	{
		Point sut = new(1.5, 0.1);

		Assert.AreEqual("1.5,0.1", sut.ToString(CultureInfo.InvariantCulture));
		Assert.AreEqual("1,5;0,1", sut.ToString(new CultureInfo("de-DE")));
		Assert.AreEqual("1.50,0.10", ((IFormattable)sut).ToString("F2", CultureInfo.InvariantCulture));
		Assert.AreEqual("1.5,0.1", string.Format(CultureInfo.InvariantCulture, "{0}", sut));
	}
}
