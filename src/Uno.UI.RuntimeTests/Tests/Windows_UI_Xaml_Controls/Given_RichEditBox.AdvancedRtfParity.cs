#nullable enable

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_Rtf_Table_Cell_Text_Is_Edited()
	{
		const string rtf = @"{\rtf1\trowd\cellx1000\intbl AB\cell\row tail}";
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.FormatRtf, rtf);

		document.GetRange(1, 1).SetText(TextSetOptions.None, "x");
		document.GetText(TextGetOptions.FormatRtf, out var exported);

		StringAssert.Contains(exported, @"\trowd");
		StringAssert.Contains(exported, @"\cellx1000");
		StringAssert.Contains(exported, "AxB");
		StringAssert.Contains(exported, @"\row");
	}

	[TestMethod]
	public void When_Rtf_Nested_Table_Outer_Cell_Text_Is_Edited()
	{
		const string rtf = @"{\rtf1\trowd\cellx3000\intbl outer "
			+ @"{\trowd\itap2\cellx1000 nested\cell\nestrow}"
			+ @" after\cell\row}";
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.FormatRtf, rtf);

		document.GetRange(1, 1).SetText(TextSetOptions.None, "x");
		document.GetText(TextGetOptions.FormatRtf, out var exported);

		StringAssert.Contains(exported, @"\itap2");
		StringAssert.Contains(exported, @"\nestrow");
	}
}
