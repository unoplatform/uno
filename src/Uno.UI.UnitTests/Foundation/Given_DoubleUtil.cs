#nullable enable

using DirectUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Foundation;

[UITestClass]
public class Given_DoubleUtil
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26175")]
	public void When_AreClose_WithinEpsilon()
	{
		Assert.IsTrue(DoubleUtil.AreClose(1.0, 1.0 + 1e-15));
		Assert.IsTrue(DoubleUtil.AreClose(0.1 + 0.2, 0.3));
		Assert.IsTrue(DoubleUtil.AreClose(1e6, 1e6 + 1e-10));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26175")]
	public void When_AreClose_OutsideEpsilon()
	{
		Assert.IsFalse(DoubleUtil.AreClose(1.0, 1.0 + 1e-9));
		Assert.IsFalse(DoubleUtil.AreClose(0.0, 1e-9));
		Assert.IsFalse(DoubleUtil.AreClose(1e6, 1e6 + 1e-6));
	}

	[TestMethod]
	public void When_AreClose_Infinity()
	{
		Assert.IsTrue(DoubleUtil.AreClose(double.PositiveInfinity, double.PositiveInfinity));
		Assert.IsFalse(DoubleUtil.AreClose(double.PositiveInfinity, double.NegativeInfinity));
		Assert.IsFalse(DoubleUtil.AreClose(double.PositiveInfinity, 1.0));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26175")]
	public void When_IsZero()
	{
		Assert.IsTrue(DoubleUtil.IsZero(0.0));
		Assert.IsTrue(DoubleUtil.IsZero(1e-17));
		Assert.IsTrue(DoubleUtil.IsZero(-1e-17));
		Assert.IsFalse(DoubleUtil.IsZero(1e-9));
		Assert.IsFalse(DoubleUtil.IsZero(-1e-9));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26175")]
	public void When_Comparing_CloseValues()
	{
		var sum = 0.1 + 0.2;

		Assert.IsFalse(DoubleUtil.GreaterThan(sum, 0.3));
		Assert.IsFalse(DoubleUtil.LessThan(0.3, sum));
		Assert.IsTrue(DoubleUtil.GreaterThanOrClose(0.3, sum));
		Assert.IsTrue(DoubleUtil.LessThanOrClose(sum, 0.3));
	}

	[TestMethod]
	public void When_Comparing_DistantValues()
	{
		Assert.IsTrue(DoubleUtil.GreaterThan(1.001, 1.0));
		Assert.IsTrue(DoubleUtil.LessThan(1.0, 1.001));
		Assert.IsFalse(DoubleUtil.GreaterThanOrClose(1.0, 1.001));
		Assert.IsFalse(DoubleUtil.LessThanOrClose(1.001, 1.0));
	}
}
