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

namespace UITests.Shared.Windows_Storage.Pickers;

[Sample("Windows.Storage",
		ViewModelType = typeof(FileOpenPickerTestsViewModel),
		IsManualTest = true,
		Description = "")]
public sealed partial class FileOpenPickerGalleryTests : Page
{
	public FileOpenPickerGalleryTests()
	{
		this.InitializeComponent();		
	}

	private async void PickSingleFileButton_Click(object sender, RoutedEventArgs e)
	{
		FileOpenPicker picker = new()
		{
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
		};
		
		picker.FileTypeFilter.Add(".jpg");
		picker.FileTypeFilter.Add(".jpeg");
		picker.FileTypeFilter.Add(".png");
		picker.FileTypeFilter.Add(".bmp");

		var storageFile = await picker.PickSingleFileAsync();

		if(storageFile is null)			
		{
			return;
		}

		using var bitmap = new BitmapImage();
		using var stream = await storageFile.OpenStreamForReadAsync()

		await bitmap.SetSourceAsync(stream);
		stack.Children.Add(new Image() { Width=200, Height=200, Source = bitmap });

		Status.Text = "Picked 1 image";

		// Display file information
		Status.Text += "\n\n Info: \n";
		Status.Text += $"Name: {storageFile.Name} \n";
		Status.Text += $"Path: {storageFile.Path} \n";			
	}

	private async void PickMultipleFilesButton_Click(object sender, RoutedEventArgs e)
	{
		FileOpenPicker picker = new()
		{
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
		};
		
		picker.FileTypeFilter.Add(".jpg");
		picker.FileTypeFilter.Add(".jpeg");
		picker.FileTypeFilter.Add(".png");
		picker.FileTypeFilter.Add(".bmp");

		var storageFile = await picker.PickMultipleFilesAsync();
	}		
}


public class FileOpenPickerTestsViewModel : ViewModelBase
{
	public ObservableCollection<CarouselItem> Items { get; } = new ObservableCollection<CarouselItem>();
}

public record CarouselItem(Image Image, string Details);
