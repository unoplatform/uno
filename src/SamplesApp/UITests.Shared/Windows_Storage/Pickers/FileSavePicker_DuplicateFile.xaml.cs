using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Shared.Windows_Storage.Pickers
{
	[Sample("Windows.Storage", ViewModelType = typeof(FileSavePicker_DuplicateFileViewModel), IgnoreInSnapshotTests = true, IsManualTest = true,
		Description =
			"Tap each of the buttons twice. For the first button, enter the file name in the dialog manually to be 'test.txt' or 'test'. " +
			"The second time the button is tapped, the dialog should either tell you the file with the same name exists and ask you if you want to overwrite, " +
			"or it should create a file with the name 'fileName (1).txt'")]
	public sealed partial class FileSavePicker_DuplicateFile : Page
	{
		public FileSavePicker_DuplicateFile()
		{
			InitializeComponent();
			this.DataContextChanged += FolderPickerTests_DataContextChanged;
		}

		private void FolderPickerTests_DataContextChanged(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as FileSavePicker_DuplicateFileViewModel;
		}

		internal FileSavePicker_DuplicateFileViewModel ViewModel { get; private set; }
	}

	internal class FileSavePicker_DuplicateFileViewModel : ViewModelBase
	{
		private string _errorMessage = string.Empty;
		private string _statusMessage = string.Empty;

		public FileSavePicker_DuplicateFileViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
#if __WASM__
			WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApi;
			Disposables.Add(Disposable.Create(() =>
			{
				WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApiWithFallback;
			}));
#endif
		}
		public string ErrorMessage
		{
			get => _errorMessage;
			set
			{
				_errorMessage = value;
				RaisePropertyChanged();
			}
		}

		public string StatusMessage
		{
			get => _statusMessage;
			set
			{
				_statusMessage = value;
				RaisePropertyChanged();
			}
		}

		public async void SaveFileNoName()
		{
			var picker = new FileSavePicker();
			picker.FileTypeChoices.Add(".txt", new[] { ".txt" });
			await PickAsync(picker);
		}

		public async void SaveFileNoExtension()
		{
			var picker = new FileSavePicker();
			picker.FileTypeChoices.Add(".txt", new[] { ".txt" });
			picker.SuggestedFileName = "fileNoExtension";
			await PickAsync(picker);
		}

		public async void SaveFileWithExtension()
		{
			var picker = new FileSavePicker();
			picker.FileTypeChoices.Add(".txt", new[] { ".txt" });
			picker.SuggestedFileName = "fileWithExtension.txt";
			await PickAsync(picker);
		}

		private async Task PickAsync(FileSavePicker picker)
		{
			ErrorMessage = string.Empty;
			StatusMessage = string.Empty;
			try
			{
				picker.SuggestedStartLocation = PickerLocationId.Downloads;
				await picker.PickSaveFileAsync();
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Exception occurred: {ex}.";
			}
		}
	}
}
