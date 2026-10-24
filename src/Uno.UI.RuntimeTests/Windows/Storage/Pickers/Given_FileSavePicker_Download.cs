#if __UNO__
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Pickers;

// The download picker, which WebAssembly falls back to without the File System Access API, returns its file without showing anything.
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_FileSavePicker_Download
{
	[TestMethod]
	[DataRow("Report.pdf")]
	[DataRow("Report.PDF")]
	public async Task When_Name_Ends_With_The_Choice_Extension_Then_It_Is_Not_Added_Again(string suggestedFileName)
	{
		var picker = new FileSavePicker { SuggestedFileName = suggestedFileName };
		picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });

		Assert.AreEqual(suggestedFileName, await PickNameAsync(picker));
	}

	[TestMethod]
	[DataRow("Report.txt")]
	[DataRow("Report.text")]
	public async Task When_Name_Ends_With_Another_Choice_Extension_Then_It_Is_Kept(string suggestedFileName)
	{
		var picker = new FileSavePicker { SuggestedFileName = suggestedFileName };
		picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });
		picker.FileTypeChoices.Add("Text", new List<string> { ".txt", ".text" });

		Assert.AreEqual(suggestedFileName, await PickNameAsync(picker));
	}

	private static async Task<string> PickNameAsync(FileSavePicker picker)
	{
		var configuration = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration;
		WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.DownloadUpload;
		StorageFile? file = null;

		try
		{
			file = await picker.PickSaveFileAsync();

			Assert.IsNotNull(file);
			Assert.AreEqual("wasmdownloadpicker", file.Provider.Id);

			return file.Name;
		}
		finally
		{
			WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = configuration;

			if (file is not null)
			{
				await file.DeleteAsync();
			}
		}
	}
}
#endif
