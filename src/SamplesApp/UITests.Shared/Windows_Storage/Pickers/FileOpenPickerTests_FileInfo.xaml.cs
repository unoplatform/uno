using System;
using System.Collections.ObjectModel;
using System.Linq;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using Microsoft.UI.Xaml;

namespace UITests.Shared.Windows_Storage.Pickers
{
	[Sample("Windows.Storage", IsManualTest = true,
		Description =
"""
- Not selecting a file should not cause an exception.
- Selecting a file should show information below the file picker buttons.
- It should be possible to pick multiple files, even if PicturesLibrary is selected and .jpg is used as file type.
- Important (iOS): iOS 17 changed the way the file picker works. When testing this sample make sure to test it on iOS 17 or higher and iOS 16 or lower.
"""
	)]
	public sealed partial class FileOpenPickerTests_FileInfo : Page
	{
		public FileOpenPickerTests_FileInfo()
		{
			this.InitializeComponent();
		}

		private async void PickVideosButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{

				var fileOpenPicker = new FileOpenPicker
				{
					ViewMode = PickerViewMode.Thumbnail,
					SuggestedStartLocation = PickerLocationId.VideosLibrary
				};

#if WINDOWS
                // For Uno.WinUI-based apps
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Instance.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);
#endif

				fileOpenPicker.FileTypeFilter.Add(".mp4");
				fileOpenPicker.FileTypeFilter.Add(".mov");
				fileOpenPicker.FileTypeFilter.Add(".wmv");
				fileOpenPicker.FileTypeFilter.Add(".webm");
				fileOpenPicker.FileTypeFilter.Add(".avi");
				fileOpenPicker.FileTypeFilter.Add(".flv");
				fileOpenPicker.FileTypeFilter.Add(".mkv");
				fileOpenPicker.FileTypeFilter.Add(".mts");

				var videos = await fileOpenPicker.PickMultipleFilesAsync();

				if (videos != null && videos.Count > 0)
				{
					var videoPaths = videos.Select(v => v.Path).ToList();
					var fileInfos = new FileInfo(videoPaths[0]);
					var fileLength = fileInfos.Length;
					var dialog = new ContentDialog()
					{
						XamlRoot = this.XamlRoot,
						Title = $"Successsfully picked {videos.Count} videos",
						Content = $"First Video Length: {fileLength} bytes",
						PrimaryButtonText = "OK"
					};
					await dialog.ShowAsync();
				}
				else
				{
					var dialog = new ContentDialog()
					{
						XamlRoot = this.XamlRoot,
						Title = "Failed to pick videos",
						Content = "You didn't pick any videos!",
						PrimaryButtonText = "OK"
					};
					await dialog.ShowAsync();
				}
			}
			catch (Exception ex)
			{
				var dialog = new ContentDialog()
				{
					XamlRoot = this.XamlRoot,
					Title = "Failed to pick videos",
					Content = ex.Message,
					PrimaryButtonText = "OK"
				};
				await dialog.ShowAsync();
			}
		}

		private async void PickPhotosButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{

				var fileOpenPicker = new FileOpenPicker
				{
					ViewMode = PickerViewMode.Thumbnail,
					SuggestedStartLocation = PickerLocationId.PicturesLibrary
				};

#if WINDOWS
                // For Uno.WinUI-based apps
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Instance.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);
#endif

				fileOpenPicker.FileTypeFilter.Add(".png");
				fileOpenPicker.FileTypeFilter.Add(".jpg");
				fileOpenPicker.FileTypeFilter.Add(".jpge");

				var photos = await fileOpenPicker.PickMultipleFilesAsync();

				if (photos != null && photos.Count > 0)
				{
					var photoPaths = photos.Select(v => v.Path).ToList();
					var fileInfos = new FileInfo(photoPaths[0]);
					var fileLength = fileInfos.Length;
					var dialog = new ContentDialog()
					{
						XamlRoot = this.XamlRoot,
						Title = $"Successsfully picked {photos.Count} Photos",
						Content = $"First Photo Length: {fileLength} bytes",
						PrimaryButtonText = "OK"
					};
					await dialog.ShowAsync();
				}
				else
				{
					var dialog = new ContentDialog()
					{
						XamlRoot = this.XamlRoot,
						Title = "Failed to pick photos",
						Content = "You didn't pick any photos!",
						PrimaryButtonText = "OK"
					};
					await dialog.ShowAsync();
				}
			}
			catch (Exception ex)
			{
				var dialog = new ContentDialog()
				{
					XamlRoot = this.XamlRoot,
					Title = "Failed to pick photos",
					Content = ex.Message,
					PrimaryButtonText = "OK"
				};
				await dialog.ShowAsync();
			}
		}
	}
}
