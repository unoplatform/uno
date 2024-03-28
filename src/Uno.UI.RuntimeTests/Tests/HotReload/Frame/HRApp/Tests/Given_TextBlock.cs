#nullable disable

using System;
using System.Formats.Asn1;
using System.Threading;
using System.Threading.Tasks;
using Microsoft/* UWP don't rename */.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.RemoteControl;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using Uno.UI.RuntimeTests.Tests.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_TextBlock : BaseTestClass
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
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			() => UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText),
			ct);
	}
}
