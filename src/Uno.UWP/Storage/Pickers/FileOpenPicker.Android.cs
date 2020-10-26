#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Android.Webkit;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Storage.Pickers.Internal;
using Uno.UI;
using AndroidUri = Android.Net.Uri;

// small, innocent change to force build restart.

#pragma warning disable CS0618
namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private const string StorageIdentifierFormatString = "Uno.FileOpenPicker.{0}";

		private TaskCompletionSource<IReadOnlyList<StorageFile>> _pickCompletionSource;
		private string _docUri = "";

		partial void Init()
		{
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Kitkat)
			{
				throw new NotSupportedException("FileOpenPicker requires Android KitKat (API level 19) or newer");
			}
			GetInitialUri();

			if(!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadExternalStorage))
			{
				throw new NotSupportedException("FileOpenPicker requires ReadExternalStorage permission defined in Android Manifest");
			}
		}

		private async Task<StorageFile> PickSingleFileAsyncTask()
		{
			var pickedFiles = await PickFilesAsync(false);
			if (pickedFiles.Count == 0)
			{
				return null;
			}

			return pickedFiles[0];
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsyncTask() => await PickFilesAsync(true);

		private string GetInitialUri()
		{
			var settingName = string.Format(StorageIdentifierFormatString, SettingsIdentifier);

			if (ApplicationData.Current.RoamingSettings.Values.ContainsKey(settingName))
			{
				var uri = ApplicationData.Current.LocalSettings.Values[settingName].ToString();
				return uri;
			}

			switch (SuggestedStartLocation)
			{
				// see also Windows.Storage.KnownFolders
				case PickerLocationId.DocumentsLibrary:
					return "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).CanonicalPath;
				case PickerLocationId.MusicLibrary:
					return "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).CanonicalPath;
				case PickerLocationId.PicturesLibrary:
					return "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).CanonicalPath;
				case PickerLocationId.Downloads:
					return "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).CanonicalPath;
				case PickerLocationId.VideosLibrary:
					return "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).CanonicalPath;
				case PickerLocationId.Unspecified:
					return string.Empty;
				default:
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"{SuggestedStartLocation} is not supported for Android FileOpenPicker");
					}
					return string.Empty;
			}
		}

		private string PathFromUri(AndroidUri fileUri)
		{
			// basic file
			if (fileUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
			{
				return fileUri.Path;
			}

			// more complicated Uris ("content://")
			if (!DocumentsContract.IsDocumentUri(Android.App.Application.Context, fileUri))
			{
				throw new NotSupportedException("FileOpenPicker - unsupported document type");
			}

			if (fileUri.Authority.Equals("com.microsoft.skydrive.content.StorageAccessProvider"))
			{
				throw new NotSupportedException("FileOpenPicker - OneDrive documents are not implemented yet");
			}

			string dataColumnName = MediaStore.Files.FileColumns.Data;
			string[] projection = { dataColumnName };

			using (var cursor = Android.App.Application.Context.ContentResolver.Query(fileUri, projection, null, null, null))
			{
				if (cursor is null)
				{
					throw new InvalidOperationException("FileOpenPicker - cannot get cursor");
				}

				if (!cursor.MoveToFirst())
				{
					throw new InvalidOperationException("FileOpenPicker - cannot MoveToFirst");
				}

				int columnNo = cursor.GetColumnIndex(dataColumnName);
				if (columnNo < 0)
				{
					throw new InvalidOperationException("FileOpenPicker - column with data doesn't exist?");
				}

				string filePath = cursor.GetString(columnNo);
				cursor.Close();

				if (!string.IsNullOrEmpty(filePath))
				{
					return filePath;
				}
			}
			return null;
		}

		private Task<IReadOnlyList<StorageFile>> PickFilesAsync(bool allowMultiple)
		{
			var intent = new Intent(ContextHelper.Current, typeof(FileOpenPickerActivity));

			intent.PutExtra(Intent.ExtraAllowMultiple, allowMultiple);
			intent.PutExtra(DocumentsContract.ExtraInitialUri, _docUri);
			intent.PutExtra(Intent.ExtraMimeTypes, GetMimeTypesFilter());

			// wrap it in Task
			_pickCompletionSource = new TaskCompletionSource<IReadOnlyList<StorageFile>>();
			FileOpenPickerActivity.FilePicked += FilePickerHandler;

			ContextHelper.Current.StartActivity(intent);

			async void FilePickerHandler(object sender, List<Android.Net.Uri> list)
			{
				FileOpenPickerActivity.FilePicked -= FilePickerHandler;

				// convert list of Uris tolist of StorageFiles
				var storageFiles = new List<StorageFile>();

				if (list.Count > 0)
				{
					var settingName = string.Format(StorageIdentifierFormatString, SettingsIdentifier);
					ApplicationData.Current.LocalSettings.Values[settingName] = list.ElementAt(0).ToString();
				}

				foreach (var fileUri in list)
				{
					Android.App.Application.Context.ContentResolver.TakePersistableUriPermission(
						fileUri, ActivityFlags.GrantReadUriPermission);

					string filePath = PathFromUri(fileUri);
					if (!string.IsNullOrEmpty(filePath))
					{
						var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
						storageFiles.Add(storageFile);
					}

				}
				_pickCompletionSource.SetResult(new FilePickerSelectedFilesArray(storageFiles.ToArray()));
			}

			return _pickCompletionSource.Task;
		}

		private string[] GetMimeTypesFilter()
		{
			var mimeTypes = new HashSet<string>();
			foreach (var fileType in FileTypeFilter)
			{
				if (fileType == "*")
				{
					// Special handling for wildcard
					mimeTypes.Clear();
					mimeTypes.Add("*/*");
					return mimeTypes.ToArray();
				}

				string mimeType = fileType;

				if (fileType.StartsWith("."))
				{
					// Supported format from UWP, e.g. ".jpg"
					mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(fileType.Substring(1));
				}

				if (mimeType != null && !mimeTypes.Contains(mimeType))
				{
					mimeTypes.Add(mimeType);
				}
			}

			return mimeTypes.ToArray();
		}
	}
}
#pragma warning restore CS0618
#endif
