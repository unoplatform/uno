#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_StringUtil_FormatMsg
{
	[TestMethod]
	[DataRow("%1 %2 time picker", new[] { "Pickup", "10:30" }, "Pickup 10:30 time picker", DisplayName = "TimePicker resource pattern")]
	[DataRow("%1 %2 date picker", new[] { "Delivery", "May 17 2026" }, "Delivery May 17 2026 date picker", DisplayName = "DatePicker resource pattern")]
	[DataRow("%2 then %1", new[] { "a", "b" }, "b then a", DisplayName = "Reverse order")]
	[DataRow("%1%1%1", new[] { "x" }, "xxx", DisplayName = "Repeated placeholder")]
	[DataRow("no placeholders", new string[0], "no placeholders", DisplayName = "No placeholders")]
	[DataRow("%%1 %1", new[] { "x" }, "%%1 x", DisplayName = "%% literal preserved, single %1 substituted")]
	[DataRow("%5", new[] { "only-one" }, "%5", DisplayName = "Out-of-range left alone")]
	[DataRow("%0 invalid", new[] { "x" }, "%0 invalid", DisplayName = "%0 is not a placeholder")]
	[DataRow("trailing %", new[] { "x" }, "trailing %", DisplayName = "Trailing percent")]
	[DataRow("%a %b", new[] { "x" }, "%a %b", DisplayName = "Non-digit after percent")]
	public void GivenFormat_WhenSubstituting_ThenItProducesExpectedOutput(string format, string[] args, string expected)
	{
		var result = StringUtil.FormatMsg(format, args);
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void GivenNoSubstitution_WhenSubstituting_ThenSameInstanceIsReturned()
	{
		var format = "no placeholders here";
		var result = StringUtil.FormatMsg(format, "ignored");
		// Optimization: no %n found → no allocation.
		Assert.AreSame(format, result);
	}

	[TestMethod]
	public void GivenEmptyFormat_WhenSubstituting_ThenReturnsEmpty()
	{
		var result = StringUtil.FormatMsg(string.Empty, "ignored");
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void GivenNullArg_WhenSubstituting_ThenTreatedAsEmpty()
	{
		var result = StringUtil.FormatMsg("%1 %2", null, "v");
		Assert.AreEqual(" v", result);
	}
}
