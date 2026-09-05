#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp;
using Uno;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace UITests.Shared.Windows_Storage.Pickers;

[Sample(
	"Windows.Storage",
		IsManualTest = true,
		Description =
"""
1. Select a disposable .txt file with FileOpenPicker.
2. The sample replaces its contents with a known value using FileIO.WriteTextAsync.
3. Open the original file in the source location with File Explorer or the platform equivalent and verify the new contents.
4. On WebAssembly, use a browser that supports the File System Access API; the upload fallback cannot update the original source file.
"""
)]
public sealed partial class FileOpenPicker_FileIOWrite : Page
{
	private const string UpdatedContents = "Uno Platform FileOpenPicker manual test.";

	public FileOpenPicker_FileIOWrite()
	{
		this.InitializeComponent();
		this.Loaded += FileOpenPicker_FileIOWrite_Loaded;
		this.Unloaded += FileOpenPicker_FileIOWrite_Unloaded;
	}

	private void FileOpenPicker_FileIOWrite_Loaded(object sender, RoutedEventArgs e)
	{
#if __WASM__
		WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApi;
#endif
	}

	private void FileOpenPicker_FileIOWrite_Unloaded(object sender, RoutedEventArgs e)
	{
#if __WASM__
		WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApiWithFallback;
#endif
	}

	private async void PickAndModifyButton_Click(object sender, RoutedEventArgs e)
	{
		PickAndModifyButton.IsEnabled = false;
		StatusTextBlock.Text = "Select a disposable .txt file...";

		try
		{
			var picker = new FileOpenPicker
			{
				SuggestedStartLocation = PickerLocationId.ComputerFolder
			};
			picker.FileTypeFilter.Add(".txt");

			var handle = WindowNative.GetWindowHandle(App.MainWindow);
			InitializeWithWindow.Initialize(picker, handle);

			var file = await picker.PickSingleFileAsync();
			if (file is null)
			{
				StatusTextBlock.Text = "No file selected.";
				return;
			}

			await FileIO.WriteTextAsync(file, UpdatedContents, UnicodeEncoding.Utf8);
			StatusTextBlock.Text = $"Updated '{file.Name}'. Verify the contents in the original source location.";
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = $"The file could not be updated: {ex.Message}";
		}
		finally
		{
			PickAndModifyButton.IsEnabled = true;
		}
	}
}
