using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

#if HAS_UNO
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage;

[TestClass]
public class Given_ApplicationData
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25560")]
	[DataRow(nameof(ApplicationData.LocalFolder))]
	[DataRow(nameof(ApplicationData.RoamingFolder))]
	[DataRow(nameof(ApplicationData.SharedLocalFolder))]
	[DataRow(nameof(ApplicationData.LocalCacheFolder))]
	[DataRow(nameof(ApplicationData.TemporaryFolder))]
	public async Task When_CreateFile_In_Folder(string folderName)
	{
		if (GetFolder(folderName) is not { } folder)
		{
			// SharedLocalFolder is null on Skia desktop, and on WinUI unless a group policy enables it.
			return;
		}

		Assert.IsTrue(Directory.Exists(folder.Path), $"{folderName} '{folder.Path}' does not exist.");

		var fileName = $"{Guid.NewGuid()}.txt";
		var file = await folder.CreateFileAsync(fileName);
		try
		{
			await FileIO.WriteTextAsync(file, fileName);

			var readBack = await folder.GetFileAsync(fileName);
			Assert.AreEqual(fileName, await FileIO.ReadTextAsync(readBack));
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25560")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(nameof(ApplicationData.LocalFolder))]
	[DataRow(nameof(ApplicationData.RoamingFolder))]
	[DataRow(nameof(ApplicationData.SharedLocalFolder))]
	public void When_Persisted_Folder_Is_Mounted(string folderName)
	{
#if HAS_UNO
		var path = GetFolder(folderName).Path;
		var mountType = WasmSemanticDomHelper.InvokeBrowserJs(
			$"typeof IDBFS === 'undefined' ? 'disabled' : (FS.lookupPath('{path}').node.mount.type === IDBFS ? 'IDBFS' : 'other')");

		// Without IDBFS nothing is persisted. With it, the persistence setup mounts the folder at startup,
		// after ApplicationData created it.
		if (mountType != "disabled")
		{
			Assert.AreEqual("IDBFS", mountType, $"{folderName} '{path}' is not mounted on IDBFS.");
		}
#endif
	}

	private static StorageFolder GetFolder(string folderName)
	{
		var data = ApplicationData.Current;
		return folderName switch
		{
			nameof(ApplicationData.LocalFolder) => data.LocalFolder,
			nameof(ApplicationData.RoamingFolder) => data.RoamingFolder,
			nameof(ApplicationData.SharedLocalFolder) => data.SharedLocalFolder,
			nameof(ApplicationData.LocalCacheFolder) => data.LocalCacheFolder,
			nameof(ApplicationData.TemporaryFolder) => data.TemporaryFolder,
			_ => throw new ArgumentOutOfRangeException(nameof(folderName)),
		};
	}
}
