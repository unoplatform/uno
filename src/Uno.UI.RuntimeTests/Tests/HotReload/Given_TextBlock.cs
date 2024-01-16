#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
[RunsInSecondaryApp]
public class Given_TextBlock : BaseHotReloadTestClass
{
	public const string SimpleTextChange = " (changed)";

	public const string FirstPageTextBlockOriginalText = "First page";
	public const string FirstPageTextBlockChangedText = FirstPageTextBlockOriginalText + SimpleTextChange;

	public const string SecondPageTextBlockOriginalText = "Second page";
	public const string SecondPageTextBlockChangedText = SecondPageTextBlockOriginalText + SimpleTextChange;


	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will be applied to
	/// the currently visible page:
	/// Open Page1
	/// Change Page1
	/// </summary>
	[TestMethod]
	public async Task When_Changing_TextBlock()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

		UnitTestsUIContentHelper.Content = new ContentControl
		{
			Content = new HR_Frame_Pages_Page1()
		};

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Check the updated text of the TextBlock
		await using (await HotReloadHelper.UpdateSourceFile<HR_Frame_Pages_Page1>(FirstPageTextBlockOriginalText, FirstPageTextBlockChangedText, ct))
		{
			await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText);
		}
	}
}
