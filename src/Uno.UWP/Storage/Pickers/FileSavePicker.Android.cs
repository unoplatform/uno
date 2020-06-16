#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Uno;
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

		public IAsyncOperation<StorageFile> PickSaveFileAsync() => PickFilesTask().AsAsyncOperation();


		private async Task<StorageFile> PickFilesTask()
		{
			var intent = AsyncFileSaveActivity.CreateIntent(FileTypeChoices, SuggestedStartLocation.ToString(), SuggestedFileName);
			var fileSavePickerActivity = await AsyncActivity.InitialiseActivity<AsyncFileSaveActivity>(intent, 10);
			var fileUri = await fileSavePickerActivity.GetFileUri();
			return await StorageFile.GetFileFromUriAsync(fileUri);

		}
	}
}

#endif
