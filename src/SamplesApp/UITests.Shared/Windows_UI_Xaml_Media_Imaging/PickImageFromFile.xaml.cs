using System;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml_Media_Imaging;

[Sample("Windows.UI.Xaml.Media", ViewModelType = typeof(PickImageFromFileViewModel), IsManualTest = true,
	Description = "Allows for selecting .jpg image from storage and displaying it. Not selecting an image should not cause an exception.")]
public sealed partial class PickImageFromFile : Page
{
	public PickImageFromFile()
	{
		this.InitializeComponent();
		this.DataContextChanged += PickImageFromFile_DataContextChanged;
	}

	private void PickImageFromFile_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as PickImageFromFileViewModel;
	}

	internal PickImageFromFileViewModel ViewModel { get; private set; }
}

internal class PickImageFromFileViewModel : ViewModelBase
{
	private string _errorMessage = string.Empty;
	private ImageSource _selectedItemSource;

	public string ErrorMessage
	{
		get => _errorMessage;
		set
		{
			_errorMessage = value;
			RaisePropertyChanged();
		}
	}

	public ImageSource SelectedItemSource
	{
		get => _selectedItemSource;
		set
		{
			_selectedItemSource = value;
			RaisePropertyChanged();
		}
	}

	public async void SelectImageButton()
	{
		try
		{
			var picker = new FileOpenPicker()
			{
				FileTypeFilter = { ".jpg" }
			};

			var fileName = $"{Guid.NewGuid()}.jpg";
			var file = await picker.PickSingleFileAsync();
			await file.CopyAsync(ApplicationData.Current.LocalFolder, fileName);
			var uri = new Uri($"ms-appdata:///Local/{fileName}");
			var bitmapImage = new BitmapImage(uri);
			SelectedItemSource = bitmapImage;
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Exception occurred: {ex}.";
		}
	}
}

