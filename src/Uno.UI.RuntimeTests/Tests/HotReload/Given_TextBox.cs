#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
[RunsInSecondaryApp]
#if !DEBUG
[Ignore("HotReload tests can be run only in DEBUG")]
#endif
public class Given_TextBox : BaseHotReloadTestClass
{
	public const string SimpleTextChange = " (changed)";

	public const string FirstPageTextBlockOriginalText = "First page";
	public const string FirstPageTextBlockChangedText = FirstPageTextBlockOriginalText + SimpleTextChange;

	public const string SecondPageTextBlockOriginalText = "Second page";
	public const string SecondPageTextBlockChangedText = SecondPageTextBlockOriginalText + SimpleTextChange;


	/// <summary>
	/// Checks that when simple change to a XAML element (change Text on TextBlock) is applied to
	/// the currently visible page, the UI update process will capture and restore property values
	/// of specific controls (in this case the Text property of a TextBox
	/// </summary>
	[TestMethod]
	[Ignore("This doesn't work on the CI pipeline")]
	public async Task When_Changing_TextBox()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

		var page = new HR_Frame_Pages_Page1();
		UnitTestsUIContentHelper.Content = new ContentControl
		{
			Content = page
		};

		var text = "Hello World!";
		page.SetTextBoxText(text);

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		await UnitTestsUIContentHelper.Content.ValidateChildElement<TextBox>(tb => Assert.AreEqual(tb.Text, text));

		// Check the updated text of the TextBlock
		await using (await HotReloadHelper.UpdateSourceFile<HR_Frame_Pages_Page1>(FirstPageTextBlockOriginalText, FirstPageTextBlockChangedText, ct))
		{
			await UnitTestsUIContentHelper.Content.ValidateChildElement<TextBox>(tb => Assert.AreEqual(text, tb.Text));
		}
	}
}
