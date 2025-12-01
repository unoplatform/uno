#nullable disable

using System;
using System.Formats.Asn1;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Disposables;
using Uno.UI.Helpers;
using Uno.UI.HotReload;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

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

#if HAS_UNO_WINUI
	/// <summary>
	/// Checks that a simple change to a XAML element (change Text on TextBlock) will be applied to
	/// the currently visible page:
	/// Open Page1
	/// Change Page1
	/// </summary>
	[TestMethod]
	public async Task When_Changing_TextBlock_UsingHRClient()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

		var content = new HR_Frame_Pages_Page2();

		UnitTestsUIContentHelper.Content = new ContentControl
		{
			Content = content
		};

		var hr = Uno.UI.RemoteControl.RemoteControlClient.Instance?.Processors.OfType<Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor>().Single();
		var ctx = Uno.UI.RuntimeTests.Tests.HotReload.FrameworkElementExtensions.GetDebugParseContext(content);
		var req = new Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.UpdateRequest(
			ctx.FileName,
			SecondPageTextBlockOriginalText,
			SecondPageTextBlockChangedText,
			true)
			.WithExtendedTimeouts(); // Required for CI
		try
		{
			await hr.UpdateFileAsync(req, ct);

			await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(SecondPageTextBlockChangedText);
		}
		finally
		{
			await hr.UpdateFileAsync(req.Undo(waitForHotReload: true), CancellationToken.None);
		}
	}

	/// <summary>
	/// Ensure that UpdateFileAsync() completes when no changes are made to the file.
	/// </summary>
	[TestMethod]
	public async Task When_Changing_TextBlock_UsingHRClient_NoChanges()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

		var content = new HR_Frame_Pages_Page2();

		UnitTestsUIContentHelper.Content = new ContentControl
		{
			Content = content
		};

		var hr = Uno.UI.RemoteControl.RemoteControlClient.Instance?.Processors.OfType<Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor>().Single();
		var ctx = Uno.UI.RuntimeTests.Tests.HotReload.FrameworkElementExtensions.GetDebugParseContext(content);
		var req = new Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.UpdateRequest(
			ctx.FileName,
			SecondPageTextBlockOriginalText,
			SecondPageTextBlockOriginalText + Environment.NewLine,
			true)
			.WithExtendedTimeouts(); // Required for CI
		
		try
		{
			await hr.UpdateFileAsync(req, ct);
		}
		finally
		{
			// Make sure to undo to not impact other tests!
			await hr.UpdateFileAsync(req.Undo(waitForHotReload: true), CancellationToken.None);
		}
	}

	// Another version of the test above, but pausing the TypeMapping before calling the file update
	[TestMethod]
	public async Task When_Changing_TextBlock_UsingHRClient_PausingTypeMapping()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(25)).Token;

		var content = new HR_Frame_Pages_Page1();

		UnitTestsUIContentHelper.Content = new ContentControl
		{
			Content = content
		};

		var hr = Uno.UI.RemoteControl.RemoteControlClient.Instance?.Processors.OfType<Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor>().Single();
		var ctx = Uno.UI.RuntimeTests.Tests.HotReload.FrameworkElementExtensions.GetDebugParseContext(content);
		var req = new Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.UpdateRequest(
			ctx.FileName,
			FirstPageTextBlockOriginalText,
			FirstPageTextBlockChangedText,
			true)
			.WithExtendedTimeouts(); // Required for CI
		await using (var pause = HotReloadService.Instance?.PauseUIUpdates())
		{
			await hr.UpdateFileAsync(req, ct);

			await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(FirstPageTextBlockOriginalText); // should NOT be changed
		}

		await hr.UpdateFileAsync(req.Undo(waitForHotReload: true), CancellationToken.None);
	}
#endif
}
