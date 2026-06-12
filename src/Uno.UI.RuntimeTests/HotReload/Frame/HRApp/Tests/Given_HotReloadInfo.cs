using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tmds.DBus.Protocol;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_HotReloadInfo : BaseTestClass
{
	[TestMethod]
	public async Task When_HotReload_Then_HotReloadInfoUpdated()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		var client = RemoteControlClient
			.Instance
			?.Processors
			.OfType<ClientHotReloadProcessor>()
			.FirstOrDefault()
			?? throw new InvalidOperationException("Hot reload not initialized properly.");

		var file = new HR_HRInfo_Case1().GetDebugParseContext().FileName;
		var request = new UpdateRequest(file, "<!--place_holder-->", "<!--place_holder--><!--bla-->");

		var originalAppVersion = __Uno.HotReload.HotReloadInfo.VersionId;
		var originalUpdateFileRequestId = __Uno.HotReload.HotReloadInfo.LastUpdateFileRequestId;

		Console.WriteLine($"Original App Version: {originalAppVersion}");
		Console.WriteLine($"Original update file: {originalUpdateFileRequestId}");

		try
		{
			var result = await client.TryUpdateFileAsync(request, ct);
			Assert.IsTrue(result.FileUpdated);
			Assert.IsNull(result.Error);

			var currentAppVersion = __Uno.HotReload.HotReloadInfo.VersionId;
			var currentUpdateFileRequestId = __Uno.HotReload.HotReloadInfo.LastUpdateFileRequestId;

			Console.WriteLine($"Current App Version: {currentAppVersion}");
			Console.WriteLine($"Current update file: {currentUpdateFileRequestId}");

			Assert.IsGreaterThan(originalAppVersion, currentAppVersion, "Application should have been updated (even if update didn't produce any effective code change).");
			Assert.IsNotNull(currentUpdateFileRequestId, "We sent an UpdateFile request, we should get back its ID.");
			Assert.AreNotEqual(currentUpdateFileRequestId, originalUpdateFileRequestId, "We sent an UpdateFile request, we should get back its ID.");
		}
		finally
		{
			await client.TryUpdateFileAsync(request.Undo(), CancellationToken.None);
		}
	}

	[TestMethod]
	public async Task When_PerformFailingCase()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		var sut = new HR_HRInfo_Case1();

		UnitTestsUIContentHelper.Content = sut;

		await UnitTestsUIContentHelper.WaitForLoaded(sut);
		await UnitTestsUIContentHelper.WaitForIdle();

		var client = RemoteControlClient
			.Instance
			?.Processors
			.OfType<ClientHotReloadProcessor>()
			.FirstOrDefault()
			?? throw new InvalidOperationException("Hot reload not initialized properly.");

		var file = sut.GetDebugParseContext().FileName;

		try
		{
			// Perform first replace
			await client.TryUpdateFileAsync(new (file, "<!--place_holder-->", "<ToggleButton /><!--place_holder-->"), ct);

			// Invoke HR that will **NOT** change anything
			await client.TryUpdateFileAsync(new (file, "<!--place_holder-->", "<!--place_holder_no_changes-->"), ct);

			// This should cause https://github.com/dotnet/roslyn/issues/79898
			// But as we produced changes in previous HR update (HRInfo), it should be fine.
			await client.TryUpdateFileAsync(new(file, "<!--place_holder_no_changes-->", "<ToggleButton />"), ct);
		}
		finally
		{
			await client.TryUpdateFileAsync(new(file, "<ToggleButton /><ToggleButton />", "<!--place_holder-->"), CancellationToken.None);
		}
	}
}
