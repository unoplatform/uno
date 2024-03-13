#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
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
		private const string AnyWildcard = "*/*";
		private const string ImageWildcard = "image/*";
		private const string VideoWildcard = "video/*";
		private ActivityFlags _activityFlags;

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
			return files.Count == 0 ? null : files[0];
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			return await PickFilesAsync(true, token);
		}

		private async Task<FilePickerSelectedFilesArray> PickFilesAsync(bool multiple, CancellationToken token)
		{
			if (ContextHelper.Current is not Activity appActivity)
			{
				throw new InvalidOperationException("Application activity is not yet set, API called too early.");
			}

			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Kitkat)
			{
				throw new NotSupportedException("FileOpenPicker requires Android KitKat (API level 19) or newer");
			}

			Intent GetIntent()
			{
				if (SuggestedStartLocation == PickerLocationId.VideosLibrary
					|| SuggestedStartLocation == PickerLocationId.PicturesLibrary)
				{
					// For images and videos we want to use the ACTION_GET_CONTENT since this allows
					// apps related to Photos and Videos to be suggested on the picker.
					var intent = new Intent(Intent.ActionGetContent);
					intent.AddCategory(Intent.CategoryOpenable);
					// additional flags are added
					if (_activityFlags != 0)
					{
						intent.AddFlags(_activityFlags);
					}

					return intent;
				}
				var openDocumentIntent = new Intent(Intent.ActionOpenDocument);
				// additional flags are added
				if (_activityFlags != 0)
				{
					openDocumentIntent.AddFlags(_activityFlags);
				}

				return openDocumentIntent;
			}

			var intent = GetIntent();

			intent.PutExtra(Intent.ExtraAllowMultiple, multiple);

			var settingName = string.Format(CultureInfo.InvariantCulture, StorageIdentifierFormatString, SettingsIdentifier);
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(settingName))
			{
				var uri = ApplicationData.Current.LocalSettings.Values[settingName].ToString();
				intent.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri, uri);
			}

			intent.SetType(GetMimeType());
			// We have already set the intent type based on the SuggestedStartLocation from above,
			// which constraints to the broad category of any-file, any-image or any-video.
			// To preserve the picture or video ones, we must not include any extra mime type,
			// that is less restrictive than what is suggested by SuggestedStartLocation.
			if (GetExtraMimeTypes() is { } extraMimeTypes)
			{
				intent.PutExtra(Intent.ExtraMimeTypes, extraMimeTypes);
			}

			_currentFileOpenPickerRequest = new TaskCompletionSource<Intent?>();

			var pickerIntent = Intent.CreateChooser(intent, "");

			appActivity.StartActivityForResult(pickerIntent, RequestCode);

			var resultIntent = await _currentFileOpenPickerRequest.Task;
			_currentFileOpenPickerRequest = null;

			if (resultIntent?.ClipData is { } clipData)
			{
				var files = new List<StorageFile>();
				var wasPath = false;

				for (var i = 0; i < clipData.ItemCount; i++)
				{
					var item = clipData.GetItemAt(i);
					if (item?.Uri is null)
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

				if (!wasPath)
				{   // if we have no path in any of files, remove setting - next call to Picker will not have InitialDir
					ApplicationData.Current.LocalSettings.Values.Remove(settingName);
				}

				return new FilePickerSelectedFilesArray(files.ToArray());
			}
			else if (resultIntent?.Data is { } data)
			{
				var file = StorageFile.GetFromSafUri(data);
				return new FilePickerSelectedFilesArray([file]);
			}

			return FilePickerSelectedFilesArray.Empty;
		}

		private string GetMimeType()
		{
			return SuggestedStartLocation switch
			{
				PickerLocationId.PicturesLibrary => ImageWildcard,
				PickerLocationId.VideosLibrary => VideoWildcard,
				_ => AnyWildcard,
			};
		}

		private string[]? GetExtraMimeTypes()
		{
			if (FileTypeFilter.Contains("*"))
			{
				return null;
			}

			List<string> mimeTypes = new List<string>();

			using Android.Webkit.MimeTypeMap? mimeTypeMap = Android.Webkit.MimeTypeMap.Singleton;
			if (mimeTypeMap is null)
			{
				// when map is unavailable (probably never happens, but Singleton returns nullable)
				return null;
			}

			foreach (string oneExtensionForLoop in FileTypeFilter)
			{
				bool unknownExtensionPresent = false;

				string oneExtension = oneExtensionForLoop;
				if (oneExtension.StartsWith('.'))
				{
					// Supported format from UWP, e.g. ".jpg"
					oneExtension = oneExtension.Substring(1);
				}

				if (!mimeTypeMap.HasExtension(oneExtension))
				{
					// when there is unknown extension, we should show all files
					unknownExtensionPresent = true;
				}

				string? mimeType = mimeTypeMap.GetMimeTypeFromExtension(oneExtension);
				if (string.IsNullOrEmpty(mimeType))
				{
					// second check for unknown extension...
					unknownExtensionPresent = true;
				}
				else
				{
#pragma warning disable CS8604 // Possible null reference argument.
					// it cannot be null, as this is within "if", but still compiler complains about possible null reference
					if (!mimeTypes.Contains(mimeType))
					{
						mimeTypes.Add(mimeType);
					}
#pragma warning restore CS8604 // Possible null reference argument.
				}

				if (unknownExtensionPresent)
				{
					// it is some unknown extension
					var mimeTypesFromUno = FileTypeFilter
						.Select(extension => MimeTypeService.GetFromExtension(extension))
						.Distinct();

					if (!mimeTypesFromUno.Any())
					{
						return null;
					}

					foreach (var oneUnoMimeType in mimeTypesFromUno)
					{
						if (!mimeTypes.Contains(oneUnoMimeType))
						{
							mimeTypes.Add(oneUnoMimeType);
						}
					}
				}
			}

			return mimeTypes.ToArray();
		}

		internal void RegisterOnBeforeStartActivity(Intent intent)
			=> _activityFlags = intent.Flags;
	}
}
