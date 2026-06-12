#nullable disable

using System;
using System.Formats.Asn1;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.RemoteControl;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using Uno.UI.RuntimeTests.Tests.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Popup : BaseTestClass
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
	public async Task When_Changing_TextBlock_Within_Popup()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

		var page = new HR_Frame_Pages_Page1();
		var popup = new Popup { Child = page };
		UnitTestsUIContentHelper.Content = new ContentControl { Content = popup };

		await UnitTestsUIContentHelper.WaitForIdle();
		popup.IsOpen = true;

		await UnitTestsUIContentHelper.WaitForLoaded(page);
		await UnitTestsUIContentHelper.WaitForIdle();

		// Check the initial text of the TextBlock
		await page.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			() => page.ValidateTextOnChildTextBlock(FirstPageTextBlockChangedText),
			ct);
	}
}
