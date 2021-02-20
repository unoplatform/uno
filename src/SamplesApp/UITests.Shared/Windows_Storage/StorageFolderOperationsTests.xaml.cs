using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_Storage
{
	[Sample("Windows.Storage", Name = "StorageFolder_Operations", ViewModelType = typeof(StorageFolderOperationsTestsViewModel))]
	public sealed partial class StorageFolderOperationsTests : Page
	{
		public StorageFolderOperationsTests()
		{
			StorageItemListTemplateSelector = new StorageItemListTemplateSelector(this);
			this.InitializeComponent();
			this.DataContextChanged += DataContextChangedHandler;
		}

		private void DataContextChangedHandler(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as StorageFolderOperationsTestsViewModel;
		}

		public StorageFolderOperationsTestsViewModel ViewModel { get; private set; }

		public StorageItemListTemplateSelector StorageItemListTemplateSelector { get; }
	}

	public class StorageFolderOperationsTestsViewModel : ViewModelBase
	{
		public StorageFolder _pickedFolder = null;
		private ObservableCollection<IStorageItem> _storageItemList;
		private string _folderNameInput;
		private string _errorMessage;
		private CreationCollisionOption _selectedCreationCollisionOption = CreationCollisionOption.FailIfExists;

		public StorageFolderOperationsTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public StorageFolder PickedFolder
		{
			get => _pickedFolder;
			set
			{
				Set(ref _pickedFolder, value);
				RaisePropertyChanged(nameof(IsFolderPicked));
			}
		}

		public bool IsFolderPicked => PickedFolder != null;

		public string FolderNameInput
		{
			get => _folderNameInput;
			set => Set(ref _folderNameInput, value);
		}

		public string ErrorMessage
		{
			get => _errorMessage;
			set => Set(ref _errorMessage, value);
		}

		public CreationCollisionOption[] CreationCollisionOptions { get; } =
			Enum.GetValues(typeof(CreationCollisionOption)).OfType<CreationCollisionOption>().ToArray();

		public CreationCollisionOption SelectedCreationCollisionOption
		{
			get => _selectedCreationCollisionOption;
			set => Set(ref _selectedCreationCollisionOption, value);
		}

		public ObservableCollection<IStorageItem> StorageItemList
		{
			get => _storageItemList;
			set => Set(ref _storageItemList, value);
		}

		public ICommand PickFolderCommand => GetOrCreateCommand(PickFolder);

		private async void PickFolder()
		{
			var picker = new FolderPicker()
			{
				SuggestedStartLocation = PickerLocationId.ComputerFolder,
				FileTypeFilter = { "*" }
			};
			PickedFolder = await picker.PickSingleFolderAsync();
		}

		public ICommand ListItemsCommand => GetOrCreateCommand(ListItems);

		private async void ListItems()
		{
			var items = await _pickedFolder.GetItemsAsync();
			StorageItemList = new ObservableCollection<IStorageItem>(items);
		}

		public ICommand ListFilesCommand => GetOrCreateCommand(ListFiles);

		private async void ListFiles()
		{
			var files = await _pickedFolder.GetFilesAsync();
			StorageItemList = new ObservableCollection<IStorageItem>(files);
		}

		public ICommand ListFoldersCommand => GetOrCreateCommand(ListFolders);

		private async void ListFolders()
		{
			var folders = await _pickedFolder.GetFoldersAsync();
			StorageItemList = new ObservableCollection<IStorageItem>(folders);
		}

		public ICommand GetBasicPropertiesCommand => GetOrCreateCommand(GetBasicProperties);

		private async void GetBasicProperties()
		{
			var basicProperties = await PickedFolder.GetBasicPropertiesAsync();
			var contentDialog = new ContentDialog()
			{
				Title = "Basic properties",
				Content = $"Size: {basicProperties.Size}, Date modified: {basicProperties.DateModified}"
			};
			contentDialog.PrimaryButtonText = "OK";
			await contentDialog.ShowAsync();
		}

		public ICommand GetFolderCommand => GetOrCreateCommand(GetFolder);

		private async void GetFolder()
		{
			ErrorMessage = "";
			try
			{
				PickedFolder = await PickedFolder.GetFolderAsync(FolderNameInput);
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot retrieve folder: {ex}";
			}
		}

		public ICommand CreateFolderCommand => GetOrCreateCommand(CreateFolder);

		private async void CreateFolder()
		{
			ErrorMessage = "";
			try
			{
				PickedFolder = await PickedFolder.CreateFolderAsync(FolderNameInput, SelectedCreationCollisionOption);
			}
			catch(Exception ex)
			{
				ErrorMessage = $"Cannot create folder: {ex}";
			}
		}
	}

	public class StorageItemListTemplateSelector : DataTemplateSelector
	{
		private readonly StorageFolderOperationsTests _page;

		public StorageItemListTemplateSelector(StorageFolderOperationsTests page)
		{
			_page = page;
		}

		protected override DataTemplate SelectTemplateCore(object item) =>
			item switch
			{
				IStorageFolder folder => _page.Resources["FolderItemTemplate"] as DataTemplate,
				IStorageFile file => _page.Resources["FileItemTemplate"] as DataTemplate,
				_ => null
			};


		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
	}
}
