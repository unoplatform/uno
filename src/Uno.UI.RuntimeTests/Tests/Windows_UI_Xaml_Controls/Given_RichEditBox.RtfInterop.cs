#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		private const string StandardListRtf = @"{\rtf1\ansi\deff0"
			+ @"{\fonttbl{\f0\fnil Segoe UI;}}"
			+ @"{\*\listtable{\list\listtemplateid1\listhybrid"
			+ @"{\listlevel\levelnfc0\levelnfcn0\leveljc0\leveljcn0\levelfollow0\levelstartat3\levelspace0\levelindent0"
			+ @"{\leveltext\'02\'00.;}{\levelnumbers\'01;}\fi-360\li720\lin720\tx720}"
			+ @"{\listname ;}\listid1}}"
			+ @"{\*\listoverridetable{\listoverride\listid1\listoverridecount0\ls1}}"
			+ @"\pard\plain\ls1\ilvl0 first\par"
			+ @"\pard\plain\ls1\ilvl0 second\par}";

		[TestMethod]
		public void When_Standard_Rtf_ListTable_Is_Projected_And_Numbering_Is_Included()
		{
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, StandardListRtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("first\rsecond\r", text);
			var first = SUT.Document.GetRange(0, 5).ParagraphFormat;
			var second = SUT.Document.GetRange(6, 12).ParagraphFormat;
			Assert.AreEqual(MarkerType.Arabic, first.ListType);
			Assert.AreEqual(MarkerStyle.Period, first.ListStyle);
			Assert.AreEqual(MarkerAlignment.Left, first.ListAlignment);
			Assert.AreEqual(0, first.ListLevelIndex);
			Assert.AreEqual(3, first.ListStart);
			Assert.AreEqual(MarkerType.Arabic, second.ListType);

			SUT.Document.GetRange(0, 12).GetText(TextGetOptions.IncludeNumbering, out var numbered);
			Assert.AreEqual("3.\tfirst\r4.\tsecond", numbered);
		}

		[TestMethod]
		[DataRow(@"{\rtf1{\field{\*\fldinst HYPERLINK ""mailto:test@example.com""}{\fldrslt mail}}}", "mail", "\"mailto:test@example.com\"")]
		[DataRow(@"{\rtf1{\field{\*\fldinst HYPERLINK ""docs/page"" \l ""part-1""}{\fldrslt relative}}}", "relative", "\"docs/page\"")]
		[DataRow(@"{\rtf1{\field{\*\fldinst HYPERLINK \l ""anchor-only""}{\fldrslt anchor}}}", "anchor", "\"anchor-only\"")]
		public void When_Rtf_Hyperlink_Targets_Are_Preserved(string rtf, string resultText, string expectedLink)
		{
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);
			GetTextWithoutFinalEop(SUT.Document, out var text);
			var resultStart = text.LastIndexOf(resultText, StringComparison.Ordinal);

			Assert.IsTrue(resultStart >= 0);
			Assert.AreEqual(expectedLink, SUT.Document.GetRange(resultStart, resultStart + 1).Link);
		}
	}
}
