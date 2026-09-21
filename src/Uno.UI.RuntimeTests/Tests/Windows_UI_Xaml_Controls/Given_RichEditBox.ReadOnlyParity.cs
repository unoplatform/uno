#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_ReadOnly_Document_SetText_Matches_Native_Protection()
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "fixed");
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			editor.IsReadOnly = true;

			Assert.ThrowsExactly<UnauthorizedAccessException>(
				() => editor.Document.SetText(TextSetOptions.None, "programmatic"));
			GetTextWithoutFinalEop(editor.Document, out var unchanged);
			Assert.AreEqual("fixed", unchanged);

			editor.IsReadOnly = false;
			editor.Document.SetText(TextSetOptions.None, "programmatic");
			GetTextWithoutFinalEop(editor.Document, out var updated);
			Assert.AreEqual("programmatic", updated);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
