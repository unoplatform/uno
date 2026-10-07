#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_StringUtil_FormatString
{
	[TestMethod]
	public void GivenCppPlaceholders_WhenFormatting_ThenTheyAreSubstituted()
	{
		var result = StringUtil.FormatString("Sorted by %1!s! ascending.", "Name");
		Assert.AreEqual("Sorted by Name ascending.", result);
	}

	[TestMethod]
	public void GivenLiteralBraces_WhenFormatting_ThenTheyArePreserved()
	{
		var result = StringUtil.FormatString("{%1!s!} {0} }{", "x");
		Assert.AreEqual("{x} {0} }{", result);
	}

	[TestMethod]
	public void GivenOutOfRangePlaceholder_WhenFormatting_ThenReturnsEmpty()
	{
		var result = StringUtil.FormatString("%1!s! and %2!s!", "only-one");
		Assert.AreEqual(string.Empty, result);
	}
}
