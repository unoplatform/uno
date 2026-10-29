using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

/// <summary>
/// #25527, end to end with the real XAML generator: a XAML file the generator cannot parse must fail the
/// pass instead of applying the page truncated at the error.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_XamlGeneratorError : BaseTestClass
{
	private const string ValidSecond = "<TextBlock Text=\"Second\"";
	private const string BrokenSecond = "<TextBlockText=\"Second\"";

	[TestMethod]
	public async Task When_XamlBroken_Then_PassFails_AndPageKept_UntilFixed()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
		var ct = cts.Token;
		var hr = RemoteControlClient.Instance?.Processors.OfType<ClientHotReloadProcessor>().SingleOrDefault()
			?? throw new InvalidOperationException("Hot-reload not initialized properly");

		var xaml = FrameworkElementExtensions.GetDebugParseContext(new HR_XamlGeneratorError_Page()).FileName;
		var codeBehind = xaml + ".cs";
		var originalXaml = File.ReadAllText(xaml);
		var originalCodeBehind = File.ReadAllText(codeBehind);

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;
		frame.Navigate(typeof(HR_XamlGeneratorError_Page));
		await AssertTexts(frame, "First", "Second", "Third");

		try
		{
			// The edit from the issue: the generator reports UXAML0001 and generates the page up to the error only.
			AssertFailed(await Update(hr, new FileEdit(xaml, ValidSecond, BrokenSecond), ct));
			await AssertTexts(frame, "First", "Second", "Third");

			// The truncated page must not ship with the next edit of the same project either.
			AssertFailed(await Update(hr, new FileEdit(codeBehind, "\"Original marker\"", "\"Edited marker\""), ct));
			await AssertTexts(frame, "First", "Second", "Third");

			// Fixing the XAML applies both pending edits.
			var fixedResult = await Update(hr, new FileEdit(xaml, BrokenSecond, "<TextBlock Text=\"Second (fixed)\""), ct);
			Assert.IsNull(fixedResult.Error, $"The fixed XAML should apply: {fixedResult.Error}");
			await AssertTexts(frame, "First", "Second (fixed)", "Third");

			var page = frame.Content!;
			Assert.AreEqual("Edited marker", page.GetType().GetProperty(nameof(HR_XamlGeneratorError_Page.Marker))?.GetValue(page));
		}
		finally
		{
			await Restore(hr, xaml, originalXaml, codeBehind, originalCodeBehind);
		}
	}

	private static async Task<UpdateResult> Update(ClientHotReloadProcessor hr, FileEdit edit, CancellationToken ct)
		=> await hr.TryUpdateFilesAsync(new UpdateRequest([edit]).WithExtendedTimeouts(), ct);

	private static void AssertFailed(UpdateResult result)
	{
		Assert.IsNotNull(result.Error, "Hot reload should have failed instead of applying the truncated page");
		StringAssert.Contains(result.Error.Message, "hot-reload failed on server: Failed");
	}

	private static async Task AssertTexts(Microsoft.UI.Xaml.Controls.Frame frame, params string[] expected)
	{
		await UnitTestsUIContentHelper.WaitForIdle();

		var texts = frame.EnumerateDescendants().OfType<TextBlock>().Select(textBlock => textBlock.Text).ToArray();
		CollectionAssert.AreEqual(expected, texts, $"Page shows [{string.Join(", ", texts)}]");
	}

	private static async Task Restore(ClientHotReloadProcessor hr, string xaml, string originalXaml, string codeBehind, string originalCodeBehind)
	{
		// Leaving the XAML broken would fail every later hot reload test of this app.
		var edits = new[] { (Path: xaml, Original: originalXaml), (Path: codeBehind, Original: originalCodeBehind) }
			.Select(file => new FileEdit(file.Path, File.ReadAllText(file.Path), file.Original))
			.Where(edit => edit.OldText != edit.NewText)
			.ToImmutableArray();

		if (!edits.IsEmpty)
		{
			await hr.TryUpdateFilesAsync(new UpdateRequest(edits).WithExtendedTimeouts(), CancellationToken.None);
		}
	}
}
