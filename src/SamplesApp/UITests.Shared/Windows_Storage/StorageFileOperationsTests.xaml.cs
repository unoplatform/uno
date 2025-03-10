using System;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_Storage
{
	[Sample("Windows.Storage", Name = "StorageFile_Operations", ViewModelType = typeof(StorageFileOperationsTestsViewModel), IsManualTest = true,
		Description = "This test page verifies some basic StorageFile operations on a file picked by FileOpenPicker.")]
	public sealed partial class StorageFileOperationsTests : Page
	{
		public StorageFileOperationsTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += StorageFileOperationsTests_DataContextChanged;
		}

		private void StorageFileOperationsTests_DataContextChanged(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as StorageFileOperationsTestsViewModel;
		}

		internal StorageFileOperationsTestsViewModel ViewModel { get; private set; }
	}

	internal class StorageFileOperationsTestsViewModel : ViewModelBase
	{
		public StorageFile _pickedFile = null;
		private string _errorMessage;

		public StorageFileOperationsTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public StorageFile PickedFile
		{
			get => _pickedFile;
			set
			{
				if (_pickedFile != value)
				{
					_pickedFile = value;
					RaisePropertyChanged();
					RaisePropertyChanged(nameof(IsFilePicked));
				}
			}
		}

		public string ErrorMessage
		{
			get => _errorMessage;
			set => Set(ref _errorMessage, value);
		}

		public bool IsFilePicked => PickedFile != null;

		public ICommand PickFileCommand => GetOrCreateCommand(PickFile);

		private async void PickFile()
		{
			var picker = new FileOpenPicker()
			{
				SuggestedStartLocation = PickerLocationId.ComputerFolder,
				FileTypeFilter = { "*" }
			};
			PickedFile = await picker.PickSingleFileAsync();
		}

		public ICommand GetBasicPropertiesCommand => GetOrCreateCommand(GetBasicProperties);

		private async void GetBasicProperties()
		{
			var basicProperties = await PickedFile.GetBasicPropertiesAsync();
			var contentDialog = new ContentDialog()
			{
				Title = "Basic properties",
				Content = $"Size: {basicProperties.Size}, Date modified: {basicProperties.DateModified}"
			};
			contentDialog.PrimaryButtonText = "OK";
			await contentDialog.ShowAsync();
		}

		public ICommand WriteHelloWorldCommand => GetOrCreateCommand(WriteHelloWorld);

		private async void WriteHelloWorld() => await FileIO.WriteTextAsync(PickedFile, "Hello, world!", UnicodeEncoding.Utf8);

		public ICommand AppendHelloWorldCommand => GetOrCreateCommand(AppendHelloWorld);

		private async void AppendHelloWorld() => await FileIO.AppendTextAsync(PickedFile, "Hello, world!", UnicodeEncoding.Utf8);

		public ICommand ReadTextCommand => GetOrCreateCommand(ReadText);

		private async void ReadText()
		{
			ErrorMessage = string.Empty;
			try
			{
				var text = await FileIO.ReadTextAsync(PickedFile);
				var contentDialog = new ContentDialog
				{
					Title = "File text content",
					Content = text
				};
				contentDialog.PrimaryButtonText = "OK";
				await contentDialog.ShowAsync();
			}
			catch (Exception ex)
			{
				ErrorMessage = "Error reading text: " + ex;
			}
		}
	}
}
