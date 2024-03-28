using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.RuntimeTests.Tests.Windows_Storage;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Tests;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_Storage
{
	[Sample("Windows.Storage", Name = "Native_Storage_RuntimeTests", ViewModelType = typeof(NativeStorageRuntimeTestsViewModel), IsManualTest = true,
		Description = "This test page allows verifying the runtime tests for native storage operations for a given picked folder.")]
	public sealed partial class NativeStorageRuntimeTests : Page
	{
		public NativeStorageRuntimeTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += DataContextChangedHandler;
		}

		private void DataContextChangedHandler(Windows.UI.Xaml.DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			var newViewModel = args.NewValue as NativeStorageRuntimeTestsViewModel;
			ViewModel = newViewModel;
			if (newViewModel != null)
			{
				ViewModel.UnitTestsControl = UnitTestsControl;
			}
		}

		internal NativeStorageRuntimeTestsViewModel ViewModel { get; private set; }
	}

	internal class NativeStorageRuntimeTestsViewModel : ViewModelBase
	{
		public UnitTestsControl UnitTestsControl { get; set; }

		public StorageFolder PickedFolder { get; set; }

		public string ErrorMessage { get; set; }

		public NativeStorageRuntimeTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
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

		public ICommand RunStorageFolderTestsCommand => GetOrCreateCommand(RunStorageFolderTests);

		private async void RunStorageFolderTests()
		{
			var testClass = new Pickable_StorageFolder_Tests(PickedFolder);
			await UnitTestsControl.RunTestsForInstance(testClass);
		}

		public ICommand RunStorageFileTestsCommand => GetOrCreateCommand(RunStorageFileTests);

		private async void RunStorageFileTests()
		{
			var testClass = new Pickable_StorageFile_Tests(PickedFolder);
			await UnitTestsControl.RunTestsForInstance(testClass);
		}

		public ICommand RunFileIOTestsCommand => GetOrCreateCommand(RunFileIOTests);

		private async void RunFileIOTests()
		{
			var testClass = new Pickable_FileIO_Tests(PickedFolder);
			await UnitTestsControl.RunTestsForInstance(testClass);
		}
	}

	public class Pickable_StorageFolder_Tests : Given_StorageFolder_Native_Base
	{
		private readonly StorageFolder _rootFolder;

		public Pickable_StorageFolder_Tests(StorageFolder rootFolder)
		{
			_rootFolder = rootFolder;
		}

		protected override Task<StorageFolder> GetRootFolderAsync() => Task.FromResult(_rootFolder);
	}

	public class Pickable_StorageFile_Tests : Given_StorageFile_Native_Base
	{
		private readonly StorageFolder _rootFolder;

		public Pickable_StorageFile_Tests(StorageFolder rootFolder)
		{
			_rootFolder = rootFolder;
		}

		protected override Task<StorageFolder> GetRootFolderAsync() => Task.FromResult(_rootFolder);
	}

	public class Pickable_FileIO_Tests : Given_FileIO_Native_Base
	{
		private readonly StorageFolder _rootFolder;

		public Pickable_FileIO_Tests(StorageFolder rootFolder)
		{
			_rootFolder = rootFolder;
		}

		protected override Task<StorageFolder> GetRootFolderAsync() => Task.FromResult(_rootFolder);
	}
}
