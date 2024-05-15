using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

internal class SvgImageSourceHelpers
{
	internal static async Task CopySourcesToAppDataAsync()
	{
		var appDataSvgs = new string[] { "chef.svg", "bookstack.svg" };
		var localFolder = ApplicationData.Current.LocalFolder;
		var folder = await localFolder.CreateFolderAsync("svg", CreationCollisionOption.OpenIfExists);

		foreach (var appDataSvg in appDataSvgs)
		{
			var item = await folder.TryGetItemAsync(appDataSvg);
			if (item is null)
			{
				var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Formats/{appDataSvg}"));
				await file.CopyAsync(folder);
			}
		}
	}
}
