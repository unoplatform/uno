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
/// #26364: while the app's only .resw cannot be parsed, other edits keep applying, and the pages they
/// regenerate keep their x:Uid lookups.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ReswError : BaseTestClass
{
	private const string ValidSecond = "<TextBlock Text=\"Second\"";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26364")]
	public async Task When_ReswBroken_Then_XamlEditApplies_AndLocalizedTextKept()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
		var ct = cts.Token;
		var hr = RemoteControlClient.Instance?.Processors.OfType<ClientHotReloadProcessor>().SingleOrDefault()
			?? throw new InvalidOperationException("Hot-reload not initialized properly");

		var xaml = FrameworkElementExtensions.GetDebugParseContext(new HR_ReswError_Page()).FileName;
		var resw = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(xaml)!, "..", "..", "Strings", "en", "Resources.resw"));
		var originalXaml = File.ReadAllText(xaml);
		var originalResw = File.ReadAllText(resw);

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;
		frame.Navigate(typeof(HR_ReswError_Page));
		await AssertTexts(frame, "Localized", "Second");

		try
		{
			// The generator reports UXAML0003 for the file, but must neither change the UnoHasLocalizationResources
			// assembly attribute (a rude edit, ENC0003) nor drop the x:Uid lookup of the page.
			var reswResult = await Update(hr, new FileEdit(resw, "</root>", "</roo>"), ct);
			Assert.IsNull(reswResult.Error, $"Breaking the .resw should not fail hot reload: {reswResult.Error}");
			await AssertTexts(frame, "Localized", "Second");

			var xamlResult = await Update(hr, new FileEdit(xaml, ValidSecond, "<TextBlock Text=\"Second (edited)\""), ct);
			Assert.IsNull(xamlResult.Error, $"The XAML edit should apply while the .resw is broken: {xamlResult.Error}");
			await AssertTexts(frame, "Localized", "Second (edited)");

			var restoreError = await Restore(hr, (xaml, originalXaml), (resw, originalResw));
			Assert.IsNull(restoreError, $"Restoring the files should apply through hot reload: {restoreError}");
			await AssertTexts(frame, "Localized", "Second");
		}
		finally
		{
			await Restore(hr, (xaml, originalXaml), (resw, originalResw));
		}
	}

	private static async Task<UpdateResult> Update(ClientHotReloadProcessor hr, FileEdit edit, CancellationToken ct)
		=> await hr.TryUpdateFilesAsync(new UpdateRequest([edit]).WithExtendedTimeouts(), ct);

	private static async Task AssertTexts(Microsoft.UI.Xaml.Controls.Frame frame, params string[] expected)
	{
		await UnitTestsUIContentHelper.WaitForIdle();

		var texts = frame.EnumerateDescendants().OfType<TextBlock>().Select(textBlock => textBlock.Text).ToArray();
		CollectionAssert.AreEqual(expected, texts, $"Page shows [{string.Join(", ", texts)}]");
	}

	/// <returns>The error of the hot reload that restored the files, if it failed.</returns>
	private static async Task<Exception?> Restore(ClientHotReloadProcessor hr, params (string Path, string Original)[] files)
	{
		var edits = files
			.Select(file => new FileEdit(file.Path, File.ReadAllText(file.Path), file.Original))
			.Where(edit => edit.OldText != edit.NewText)
			.ToImmutableArray();

		if (edits.IsEmpty)
		{
			return null;
		}

		Exception? error;
		try
		{
			error = (await hr.TryUpdateFilesAsync(new UpdateRequest(edits).WithExtendedTimeouts(), CancellationToken.None)).Error;
		}
		catch (Exception e)
		{
			error = e;
		}

		if (error is not null)
		{
			// Every later hot reload test of this app would otherwise run against the broken .resw.
			foreach (var (path, original) in files)
			{
				File.WriteAllText(path, original);
			}
		}

		return error;
	}
}
