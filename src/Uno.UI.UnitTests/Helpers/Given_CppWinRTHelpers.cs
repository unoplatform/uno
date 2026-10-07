#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_CppWinRTHelpers
{
	[TestMethod]
	[DataRow(0.0001, "1e-04")]
	[DataRow(1e16, "1e+16")]
	[DataRow(123456789012345680.0, "123456789012345680")]
	[DataRow(0.001, "0.001")]
	[DataRow(123.456, "123.456")]
	[DataRow(-2.5e-300, "-2.5e-300")]
	[DataRow(1e100, "1e+100")]
	[DataRow(0.0, "0")]
	public void GivenDouble_WhenToHString_ThenMatchesToChars(double value, string expected)
		=> Assert.AreEqual(expected, CppWinRTHelpers.ToHString(value));

	[TestMethod]
	[DataRow(1.5f, "1.5")]
	[DataRow(0.1f, "0.1")]
	[DataRow(1e10f, "1e+10")]
	public void GivenSingle_WhenToHString_ThenMatchesToChars(float value, string expected)
		=> Assert.AreEqual(expected, CppWinRTHelpers.ToHString(value));

	[TestMethod]
	public void GivenNonFiniteDouble_WhenToHString_ThenMatchesMsvc()
	{
		Assert.AreEqual("inf", CppWinRTHelpers.ToHString(double.PositiveInfinity));
		Assert.AreEqual("-inf", CppWinRTHelpers.ToHString(double.NegativeInfinity));
		Assert.AreEqual("-nan(ind)", CppWinRTHelpers.ToHString(double.NaN));
		Assert.AreEqual("nan", CppWinRTHelpers.ToHString(BitConverter.UInt64BitsToDouble(0x7FF8_0000_0000_0000ul)));
	}

	[TestMethod]
	public void GivenNegativeZero_WhenToHString_ThenKeepsSign()
		=> Assert.AreEqual("-0", CppWinRTHelpers.ToHString(-0.0));

	[TestMethod]
	public void GivenWeakReference_WhenGet_ThenMatchesWeakRefGet()
	{
		WeakReference<object>? empty = null;
		Assert.IsNull(empty.Get());

		object target = new();
		WeakReference<object> live = new(target);
		Assert.AreSame(target, live.Get());
	}
}
