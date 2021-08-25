#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Uno.UI;

namespace Windows.Storage.Pickers
{
    public partial class FileOpenPicker
    {
		internal const int RequestCode = 6002;
		private static TaskCompletionSource<Intent?>? _currentFileOpenPickerRequest;

		private const string StorageIdentifierFormatString = "Uno.FileOpenPicker.{0}";

		internal static bool TryHandleIntent(Intent intent, Result resultCode)
		{
			if (_currentFileOpenPickerRequest == null)
			{
				return false;
			}
			if (resultCode == Result.Canceled)
			{
				_currentFileOpenPickerRequest.SetResult(null);
			}
			else
			{
				_currentFileOpenPickerRequest.SetResult(intent);
			}
			return true;
		}

		private async Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			var files = await PickFilesAsync(false, token);
			return files.FirstOrDefault();
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			return await PickFilesAsync(true, token);
		}

		private async Task<FilePickerSelectedFilesArray> PickFilesAsync(bool multiple, CancellationToken token)
		{
			if (!(ContextHelper.Current is Activity appActivity))
			{
				throw new InvalidOperationException("Application activity is not yet set, API called too early.");
			}

			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Kitkat)
			{
				throw new NotSupportedException("FileOpenPicker requires Android KitKat (API level 19) or newer");
			}

			var action = Intent.ActionOpenDocument;

			var intent = new Intent(action);
			intent.PutExtra(Intent.ExtraAllowMultiple, multiple);

			var settingName = string.Format(StorageIdentifierFormatString, SettingsIdentifier);
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(settingName))
			{
				var uri = ApplicationData.Current.LocalSettings.Values[settingName].ToString();
				intent.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri, uri);
			}

			intent.SetType("*/*");

			var mimeTypes = GetMimeTypes();
			intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);

			_currentFileOpenPickerRequest = new TaskCompletionSource<Intent?>();

			appActivity.StartActivityForResult(intent, RequestCode);

			var resultIntent = await _currentFileOpenPickerRequest.Task;
			_currentFileOpenPickerRequest = null;

			if (resultIntent?.ClipData != null)
			{
				List<StorageFile> files = new List<StorageFile>();
				bool wasPath = false;

				for (var i = 0; i < resultIntent.ClipData.ItemCount; i++)
				{
					var item = resultIntent.ClipData.GetItemAt(i);
					if (item?.Uri == null)
					{
						continue;
					}
					var file = StorageFile.GetFromSafUri(item.Uri);
					files.Add(file);

					// for PickMultipleFilesAsync(), we preserve (existing) path of last selected file
					if (!string.IsNullOrEmpty(file.Path))
					{
						ApplicationData.Current.LocalSettings.Values[settingName] = file.Path;
						wasPath = true;
					}
				}

				if(!wasPath)
				{   // if we have no path in any of files, remove setting - next call to Picker will not have InitialDir
					ApplicationData.Current.LocalSettings.Values.Remove(settingName);
				}
				return new FilePickerSelectedFilesArray(files.ToArray());
			}
			else if (resultIntent?.Data != null)
			{
				var file = StorageFile.GetFromSafUri(resultIntent.Data);
				return new FilePickerSelectedFilesArray(new[] { file });
			}

			return FilePickerSelectedFilesArray.Empty;
		}

		private string[] GetMimeTypes()
		{
			if (FileTypeFilter.Contains("*"))
			{
				return new[] { "*/*" };
			}

			return FileTypeFilter
				.Select(extension => MimeTypeService.GetFromExtension(extension))
				.Distinct()
				.ToArray();
		}
	}
}
