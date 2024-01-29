using System;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Media_Imaging;

[Sample("Microsoft.UI.Xaml.Media", IsManualTest = true)]
public sealed partial class PickImageFromFile : Page
{
	public PickImageFromFile()
	{
		this.InitializeComponent();
	}

	private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add(".jpg");

		var fileName = $"{Guid.NewGuid()}.jpg";
		StorageFile file = await picker.PickSingleFileAsync();
		await file.CopyAsync(ApplicationData.Current.LocalFolder, fileName);
		var uri = new Uri($"ms-appdata:///Local/{fileName}");
		var bitmapImage = new BitmapImage(uri);
		SelectedImage.Source = bitmapImage;
	}
}
