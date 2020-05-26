#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Provider;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		private string _suggestedStartLocationUri = "";
		private PickerLocationId _suggestedStartLocation;
		public PickerLocationId SuggestedStartLocation
		{
			get => _suggestedStartLocation;
			set
			{
				_suggestedStartLocation = value;
				SetSuggestedStartLocationUri(_suggestedStartLocation);
			}
		} 

		private void SetSuggestedStartLocationUri(PickerLocationId locationId) => _suggestedStartLocationUri = locationId switch
		{
			PickerLocationId.DocumentsLibrary => "file://" + Android.OS.Environment.DirectoryDocuments,
			PickerLocationId.MusicLibrary => "file://" + Android.OS.Environment.DirectoryMusic,
			PickerLocationId.PicturesLibrary => "file://" + Android.OS.Environment.DirectoryPictures,
			PickerLocationId.Downloads => "file://" + Android.OS.Environment.DirectoryDownloads,
			PickerLocationId.VideosLibrary => "file://" + Android.OS.Environment.DirectoryMovies,
			PickerLocationId.Unspecified => "file://" + "",
			_ => throw new NotImplementedException("FileOpenPicker unimplemented type of initial dir"),
		};

		//private readonly IDictionary<string, IList<string>> _fileTypeChoice = new Dictionary<string, IList<string> >();

		//public StorageFile SuggestedSaveFile
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member StorageFile FileSavePicker.SuggestedSaveFile is not implemented in Uno.");
		//	}
		//	set
		//	{
		//		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "StorageFile FileSavePicker.SuggestedSaveFile");
		//	}
		//}

		//public  string SuggestedFileName
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member string FileSavePicker.SuggestedFileName is not implemented in Uno.");
		//	}
		//	set
		//	{
		//		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "string FileSavePicker.SuggestedFileName");
		//	}
		//}

		//public  string SettingsIdentifier
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member string FileSavePicker.SettingsIdentifier is not implemented in Uno.");
		//	}
		//	set
		//	{
		//		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "string FileSavePicker.SettingsIdentifier");
		//	}
		//}

		//public  string DefaultFileExtension
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member string FileSavePicker.DefaultFileExtension is not implemented in Uno.");
		//	}
		//	set
		//	{
		//		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "string FileSavePicker.DefaultFileExtension");
		//	}
		//}

		//public  string CommitButtonText
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member string FileSavePicker.CommitButtonText is not implemented in Uno.");
		//	}
		//	set
		//	{
		//		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "string FileSavePicker.CommitButtonText");
		//	}
		//}

		//public IDictionary<string, IList<string>> FileTypeChoices => _fileTypeChoice;

		//public  global::Windows.Foundation.Collections.ValueSet ContinuationData
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member ValueSet FileSavePicker.ContinuationData is not implemented in Uno.");
		//	}
		//}

		//public  string EnterpriseId
		//{
		//	get
		//	{
		//		throw new global::System.NotImplementedException("The member string FileSavePicker.EnterpriseId is not implemented in Uno.");
		//	}
		//	set
		//	{
		//		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "string FileSavePicker.EnterpriseId");
		//	}
		//}

		public FileSavePicker()
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
		}

		public IAsyncOperation<StorageFile> PickSaveFileAsync() => PickFilesTask().AsAsyncOperation();


		private async Task<StorageFile> PickFilesTask()
		{
			var fileSavePickerActivity = new FileSavePickerActivity(_suggestedStartLocationUri);
			return await fileSavePickerActivity.Run();

		}


		//public void PickSaveFileAndContinue()
		//{
		//	global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FileSavePicker", "void FileSavePicker.PickSaveFileAndContinue()");
		//}

		[Activity(ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
		internal class FileSavePickerActivity : Activity
		{
			private readonly Intent _intent;
			private readonly TaskCompletionSource<bool> _completed;
			private Android.Net.Uri _fileSaveUri;

			public FileSavePickerActivity(string initialDir)
			{
				_intent = new Intent(Intent.ActionCreateDocument);
				_completed = new TaskCompletionSource<bool>();

				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O && !string.IsNullOrEmpty(initialDir))
				{
					_intent.PutExtra(DocumentsContract.ExtraInitialUri, initialDir);
				}
			}
			protected override void OnCreate(Android.OS.Bundle savedInstanceState)
			{
				base.OnCreate(savedInstanceState);

			}

			public async Task<StorageFile> Run()
			{
				StartActivityForResult(_intent, 1);
				await _completed.Task;
				return await StorageFile.GetFileFromPathAsync(_fileSaveUri.EncodedPath);
			}

			protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
			{
				base.OnActivityResult(requestCode, resultCode, data);
				var pickedFiles = new List<Android.Net.Uri>();
				if (resultCode != Result.Canceled)
				{
					if (data?.Data != null)
					{
						_fileSaveUri = data.Data;
					}
				}
				Finish();
			}

			protected override void OnDestroy()
			{
				_completed?.TrySetResult(true);
			}
		}
	}
}

#endif
