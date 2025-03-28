using System;
using System.Collections.ObjectModel;
using System.Linq;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_Storage.Pickers
{
	[Sample("Windows.Storage", ViewModelType = typeof(FolderPickerTestsViewModel), IsManualTest = true,
		Description = "Allows testing all features of FolderPicker. Currently not supported on Android, iOS, and macOS. Not selecting a folder should not cause an exception")]
	public sealed partial class FolderPickerTests : Page
	{
		public FolderPickerTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += FolderPickerTests_DataContextChanged;
		}

		private void FolderPickerTests_DataContextChanged(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as FolderPickerTestsViewModel;
		}

		internal FolderPickerTestsViewModel ViewModel { get; private set; }
	}

	internal class FolderPickerTestsViewModel : ViewModelBase
	{
		private string _fileType = string.Empty;
		private string _errorMessage = string.Empty;
		private string _statusMessage = string.Empty;

		private StorageFolder _pickedFolder = null;

		public FolderPickerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public PickerLocationId[] SuggestedStartLocations { get; } = Enum.GetValues<PickerLocationId>();

		public PickerLocationId SuggestedStartLocation { get; set; } = PickerLocationId.ComputerFolder;

		public string SettingsIdentifier { get; set; } = string.Empty;

		public string CommitButtonText { get; set; } = string.Empty;

		public PickerViewMode[] ViewModes { get; } = Enum.GetValues<PickerViewMode>();

		public PickerViewMode ViewMode { get; set; } = PickerViewMode.List;

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

		public StorageFolder PickedFolder
		{
			get => _pickedFolder;
			set
			{
				_pickedFolder = value;
				RaisePropertyChanged();
			}
		}

		public async void PickFolder()
		{
			ErrorMessage = string.Empty;
			StatusMessage = string.Empty;
			try
			{
				var folderPicker = new FolderPicker
				{
					SuggestedStartLocation = SuggestedStartLocation,
					ViewMode = ViewMode,
				};
				if (!string.IsNullOrEmpty(SettingsIdentifier))
				{
					folderPicker.SettingsIdentifier = SettingsIdentifier;
				}
				if (!string.IsNullOrEmpty(CommitButtonText))
				{
					folderPicker.CommitButtonText = CommitButtonText;
				}
				folderPicker.FileTypeFilter.AddRange(FileTypeFilter);
				var pickedFolder = await folderPicker.PickSingleFolderAsync();
				if (pickedFolder != null)
				{
					StatusMessage = "Folder picked successfully.";
					PickedFolder = pickedFolder;
				}
				else
				{
					StatusMessage = "No folder picked.";
					PickedFolder = null;
				}
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Exception occurred: {ex}.";
			}
		}
	}
}
