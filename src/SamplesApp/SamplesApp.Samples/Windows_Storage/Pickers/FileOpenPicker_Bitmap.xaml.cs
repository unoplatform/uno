using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.UI.Core;
using WinRT.Interop;
using SamplesApp;

namespace UITests.Windows_Storage.Pickers;

[Sample("Windows.Storage", IsManualTest = true,
	Description =
"""
 Select an image file. After confirming the dialog, it should be displayed in a dialog.
"""
)]
public sealed partial class FileOpenPicker_Bitmap : Page
{
	public FileOpenPicker_Bitmap()
	{
		this.InitializeComponent();
	}

	private async void OpenPickerButton_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FileOpenPicker();
		picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
		picker.FileTypeFilter.Add(".jpg");
		picker.FileTypeFilter.Add(".jpeg");
		picker.FileTypeFilter.Add(".png");
		picker.FileTypeFilter.Add(".bmp");
		InitForWin(picker);

		var storageFiles = await picker.PickMultipleFilesAsync();

		if (storageFiles?.Any() ?? false)
		{
			var stack = new StackPanel() { Spacing = 10 };
			foreach (var file in storageFiles)
			{
				var bitmap = new BitmapImage();
				var memoryStream = new MemoryStream();
				using (var stream = await file.OpenReadAsync())
				{
					await stream.AsStreamForRead().CopyToAsync(memoryStream);
					memoryStream.Position = 0;
					await bitmap.SetSourceAsync(memoryStream.AsRandomAccessStream());
				}
				stack.Children.Add(new Image() { Source = bitmap });
			}
			var dialog = new ContentDialog()
			{
				Title = "Successfully picked images",
				Content = new ScrollViewer() { Content = stack },
				PrimaryButtonText = "OK",
				XamlRoot = XamlRoot
			};
			await dialog.ShowAsync();
		}
		else
		{
			await new ContentDialog()
			{
				Content = "You didn't pick any image!",
				Title = "Failed to pick images",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
	}

	private void InitForWin(object instance) // `object` here can be replaced by whatever type of 1st param of InitializeWithWindow.Initialize
	{
		var handle = WindowNative.GetWindowHandle(App.MainWindow);
		InitializeWithWindow.Initialize(instance, handle);
	}
}
