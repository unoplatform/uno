using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_Storage
{
	[Sample("Windows.Storage", Name = "StorageFolder_Operations", ViewModelType = typeof(StorageFolderOperationsTestsViewModel), IsManualTest = true,
		Description = "This test page verifies some basic StorageFolder operations on a folder picked by FolderPicker.")]
	public sealed partial class StorageFolderOperationsTests : Page
	{
		public StorageFolderOperationsTests()
		{
			StorageItemListTemplateSelector = new StorageItemListTemplateSelector(this);
			this.InitializeComponent();
			this.DataContextChanged += DataContextChangedHandler;
		}

		private void DataContextChangedHandler(Microsoft.UI.Xaml.DependencyObject sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as StorageFolderOperationsTestsViewModel;
		}

		internal StorageFolderOperationsTestsViewModel ViewModel { get; private set; }

		public StorageItemListTemplateSelector StorageItemListTemplateSelector { get; }
	}

	internal class StorageFolderOperationsTestsViewModel : ViewModelBase
	{
		public StorageFolder _pickedFolder = null;
		private ObservableCollection<IStorageItem> _storageItemList;
		private string _itemNameInput;
		private string _errorMessage;
		private CreationCollisionOption _selectedCreationCollisionOption = CreationCollisionOption.FailIfExists;

		public StorageFolderOperationsTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
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

		public string ItemNameInput
		{
			get => _itemNameInput;
			set => Set(ref _itemNameInput, value);
		}

		public string ErrorMessage
		{
			get => _errorMessage;
			set => Set(ref _errorMessage, value);
		}

		public CreationCollisionOption[] CreationCollisionOptions { get; } =
			Enum.GetValues<CreationCollisionOption>();

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
			ErrorMessage = string.Empty;
			try
			{
				var picker = new FolderPicker()
				{
					SuggestedStartLocation = PickerLocationId.ComputerFolder,
					FileTypeFilter = { "*" }
				};
				PickedFolder = await picker.PickSingleFolderAsync();
			}
			catch (Exception ex)
			{
				ErrorMessage = "Can't pick folder: " + ex;
			}
		}

		public ICommand ListItemsCommand => GetOrCreateCommand(ListItems);

		private async void ListItems()
		{
			ErrorMessage = string.Empty;
			try
			{
				var items = await _pickedFolder.GetItemsAsync();
				StorageItemList = new ObservableCollection<IStorageItem>(items);
			}
			catch (Exception ex)
			{
				ErrorMessage = "Can't list items: " + ex;
			}
		}

		public ICommand ListFilesCommand => GetOrCreateCommand(ListFiles);

		private async void ListFiles()
		{
			ErrorMessage = string.Empty;
			try
			{
				var files = await _pickedFolder.GetFilesAsync();
				StorageItemList = new ObservableCollection<IStorageItem>(files);
			}
			catch (Exception ex)
			{
				ErrorMessage = "Can't list files: " + ex;
			}
		}

		public ICommand ListFoldersCommand => GetOrCreateCommand(ListFolders);

		private async void ListFolders()
		{
			ErrorMessage = string.Empty;
			try
			{
				var folders = await _pickedFolder.GetFoldersAsync();
				StorageItemList = new ObservableCollection<IStorageItem>(folders);
			}
			catch (Exception ex)
			{
				ErrorMessage = "Can't list folders: " + ex;
			}
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
				var pickedFolder = await PickedFolder.GetFolderAsync(ItemNameInput);
				await ShowDialogAsync("Folder retrieved", $"Successfully retrieved folder '{pickedFolder.Name}'.");
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot retrieve folder: {ex}";
			}
		}

		public ICommand GetFileCommand => GetOrCreateCommand(GetFile);

		private async void GetFile()
		{
			ErrorMessage = "";
			try
			{
				var pickedFile = await PickedFolder.GetFileAsync(ItemNameInput);
				await ShowDialogAsync("File retrieved", $"Successfully retrieved file '{pickedFile.Name}' with extension '{pickedFile.FileType}'");
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot retrieve file: {ex}";
			}
		}

		public ICommand CreateFolderCommand => GetOrCreateCommand(CreateFolder);

		private async void CreateFolder()
		{
			ErrorMessage = "";
			try
			{
				var createdFolder = await PickedFolder.CreateFolderAsync(ItemNameInput, SelectedCreationCollisionOption);
				await ShowDialogAsync("Folder created", $"Successfully created folder '{createdFolder.Name}'.");
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot create folder: {ex}";
			}
		}

		public ICommand CreateFileCommand => GetOrCreateCommand(CreateFile);

		private async void CreateFile()
		{
			ErrorMessage = "";
			try
			{
				var createdFile = await PickedFolder.CreateFileAsync(ItemNameInput, SelectedCreationCollisionOption);
				await ShowDialogAsync("File created", $"Successfully created file '{createdFile.Name}' with extension '{createdFile.FileType}'.");
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot create folder: {ex}";
			}
		}

		public ICommand TryGetItemCommand => GetOrCreateCommand(TryGetItem);

		private async void TryGetItem()
		{
			ErrorMessage = "";
			try
			{
				var retrievedItem = await PickedFolder.TryGetItemAsync(ItemNameInput);
				if (retrievedItem != null)
				{
					var itemType = retrievedItem.IsOfType(StorageItemTypes.File) ? "file" : "folder";
					await ShowDialogAsync("Item retrieved", $"Successfully retrieved item '{retrievedItem.Name}' which is a {itemType}.");
				}
				else
				{
					await ShowDialogAsync("Item not found", $"There is no item with such name.");
				}
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot get item: {ex}";
			}
		}

		public ICommand DeleteCommand => GetOrCreateCommand(Delete);

		private async void Delete()
		{
			ErrorMessage = "";
			try
			{
				var retrievedItem = await PickedFolder.TryGetItemAsync(ItemNameInput);
				if (retrievedItem != null)
				{
					await retrievedItem.DeleteAsync();
					await ShowDialogAsync("Item deleted", $"Item {ItemNameInput} was successfully deleted.");
				}
				else
				{
					await ShowDialogAsync("Item not found", $"There is no item with such name.");
				}
			}
			catch (Exception ex)
			{
				ErrorMessage = $"Cannot create folder: {ex}";
			}
		}

		private async Task ShowDialogAsync(string title, string text)
		{
			var dialog = new ContentDialog
			{
				Title = title,
				Content = text,
				PrimaryButtonText = "Ok"
			};
			await dialog.ShowAsync();
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
