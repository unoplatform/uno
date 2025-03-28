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
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_Storage.Pickers
{
	[Sample("Windows.Storage", ViewModelType = typeof(FileOpenPickerTestsViewModel), IsManualTest = true,
		Description =
"""
- Not selecting a file should not cause an exception.
- Selecting a file should show information below the file picker buttons.
- It should be possible to pick multiple files, even if PicturesLibrary is selected and .jpg is used as file type.
- Important (iOS): iOS 17 changed the way the file picker works. When testing this sample make sure to test it on iOS 17 or higher and iOS 16 or lower.
"""
	)]
	public sealed partial class FileOpenPickerTests : Page
	{
		public FileOpenPickerTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += FolderPickerTests_DataContextChanged;
		}

		private void FolderPickerTests_DataContextChanged(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			// When the picker is showing, the Page is resetting the DataContext to null.
			if (args.NewValue is FileOpenPickerTestsViewModel viewModel)
			{
				ViewModel = viewModel;
			}

		}

		internal FileOpenPickerTestsViewModel ViewModel { get; private set; }
	}

	internal class FileOpenPickerTestsViewModel : ViewModelBase
	{
		private string _fileType = string.Empty;
		private string _errorMessage = string.Empty;
		private string _statusMessage = string.Empty;

		private StorageFile[] _pickedFiles = null;

		public FileOpenPickerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
#if __WASM__
			WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApi;
			Disposables.Add(Disposable.Create(() =>
			{
				WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApiWithFallback;
			}));
#endif
		}

		public PickerLocationId[] SuggestedStartLocations { get; } = Enum.GetValues<PickerLocationId>();

		public PickerLocationId SuggestedStartLocation { get; set; } = PickerLocationId.ComputerFolder;

		public string SettingsIdentifier { get; set; } = string.Empty;

		public string CommitButtonText { get; set; } = string.Empty;

		public PickerViewMode[] ViewModes { get; } = Enum.GetValues<PickerViewMode>();

		public PickerViewMode ViewMode { get; set; } = PickerViewMode.List;

		public int MaxFiles { get; set; } = 2;

		public string FileType
		{
			get => _fileType;
			set
			{
				_fileType = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<string> FileTypeFilter { get; } = new ObservableCollection<string>() { "*" };

		public string ErrorMessage
		{
			get => _errorMessage;
			set
			{
				_errorMessage = value;
				RaisePropertyChanged();
			}
		}

#if __WASM__
		public bool UseNativePicker
		{
			get => WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration == WasmPickerConfiguration.FileSystemAccessApi;
			set
			{
				var usesNativePicker = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration == WasmPickerConfiguration.FileSystemAccessApi;
				if (usesNativePicker != value)
				{
					WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = value ?
						WasmPickerConfiguration.FileSystemAccessApi :
						WasmPickerConfiguration.DownloadUpload;

					RaisePropertyChanged();
				}
			}
		}
#endif

		public string StatusMessage
		{
			get => _statusMessage;
			set
			{
				_statusMessage = value;
				RaisePropertyChanged();
			}
		}

		public void AddFileType()
		{
			if (!string.IsNullOrEmpty(FileType))
			{
				FileTypeFilter.Add(FileType);
				FileType = string.Empty;
			}
		}

		public void ClearFileTypes() => FileTypeFilter.Clear();

		public StorageFile[] PickedFiles
		{
			get => _pickedFiles;
			set
			{
				_pickedFiles = value;
				RaisePropertyChanged();
			}
		}

		public async void PickSingleFile()
		{
			ErrorMessage = string.Empty;
			StatusMessage = string.Empty;
			try
			{
				var filePicker = new FileOpenPicker
				{
					SuggestedStartLocation = SuggestedStartLocation,
					ViewMode = ViewMode,
				};
				if (!string.IsNullOrEmpty(SettingsIdentifier))
				{
					filePicker.SettingsIdentifier = SettingsIdentifier;
				}
				if (!string.IsNullOrEmpty(CommitButtonText))
				{
					filePicker.CommitButtonText = CommitButtonText;
				}
				filePicker.FileTypeFilter.AddRange(FileTypeFilter);

				StatusMessage = "Picking single file....";

				var pickedFile = await filePicker.PickSingleFileAsync();
				if (pickedFile != null)
				{
					StatusMessage = "File picked successfully.";
					PickedFiles = new[] { pickedFile };
				}
				else
				{
					StatusMessage = "No file picked";
					PickedFiles = Array.Empty<StorageFile>();
				}
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Exception occurred: {ex}.";
			}
		}

		public async void PickMultipleFiles()
		{
			ErrorMessage = string.Empty;
			StatusMessage = string.Empty;
			try
			{
				var filePicker = new FileOpenPicker
				{
					SuggestedStartLocation = SuggestedStartLocation,
					ViewMode = ViewMode,
				};
				if (!string.IsNullOrEmpty(SettingsIdentifier))
				{
					filePicker.SettingsIdentifier = SettingsIdentifier;
				}
				if (!string.IsNullOrEmpty(CommitButtonText))
				{
					filePicker.CommitButtonText = CommitButtonText;
				}
				filePicker.FileTypeFilter.AddRange(FileTypeFilter);

				StatusMessage = "Picking multiple files...";
#if __IOS__
				filePicker.SetMultipleFilesLimit(MaxFiles);
#endif
				var pickedFiles = await filePicker.PickMultipleFilesAsync();
				if (pickedFiles.Any())
				{
					StatusMessage = $"{pickedFiles.Count} files picked successfully.";
					PickedFiles = pickedFiles.ToArray();
				}
				else
				{
					StatusMessage = "No files picked.";
					PickedFiles = Array.Empty<StorageFile>();
				}
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Exception occurred: {ex}.";
			}
		}
	}
}
