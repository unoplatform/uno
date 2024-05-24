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
using Uno.UI.Dispatching;
using Windows.UI.Core;
using Uno.Extensions;
using AndroidUri = Android.Net.Uri;
using Android.Provider;
using System.IO;
using Java.Security;
using Android.Webkit;
using Android.Content.PM;
using Uno.Foundation.Logging;

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
		private Action<Intent>? _intentAction;

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

					return intent;
				}

				return new Intent(Intent.ActionOpenDocument);
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

			_intentAction?.Invoke(intent);

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
					if (item?.Uri is not { } uri || FileSystemUtils.EnsurePhysicalPath(uri) is not { } path)
					{
						continue;
					}

					var file = StorageFile.GetFileFromPath(path);
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

				return new FilePickerSelectedFilesArray([.. files]);
			}
			else if (resultIntent?.Data is { } data && FileSystemUtils.EnsurePhysicalPath(data) is { } path)
			{
				var file = StorageFile.GetFileFromPath(path);
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

			using MimeTypeMap? mimeTypeMap = MimeTypeMap.Singleton;
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
						.Select(MimeTypeService.GetFromExtension)
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

		internal void RegisterOnBeforeStartActivity(Action<Intent> intentAction)
			=> _intentAction = intentAction;

		static class FileSystemUtils
		{
			const string FolderHash = "2203693cc04e0be7f4f024d5f9499e13";

			const string StorageTypePrimary = "primary";
			const string StorageTypeRaw = "raw";
			const string StorageTypeImage = "image";
			const string StorageTypeVideo = "video";
			const string StorageTypeAudio = "audio";

			static readonly IReadOnlyCollection<string> _contentUriPrefixes =
			[
				"content://downloads/public_downloads",
				"content://downloads/my_downloads",
				"content://downloads/all_downloads",
			];

			const string UriSchemeFile = "file";
			const string UriSchemeContent = "content";

			const string UriAuthorityExternalStorage = "com.android.externalstorage.documents";
			const string UriAuthorityDownloads = "com.android.providers.downloads.documents";
			const string UriAuthorityMedia = "com.android.providers.media.documents";

			readonly static Type _this = typeof(FileSystemUtils);

			public static Java.IO.File GetTemporaryFile(Java.IO.File root, string fileName)
			{
				// create the directory for all files
				var rootDir = new Java.IO.File(root, FolderHash);
				rootDir.Mkdirs();
				rootDir.DeleteOnExit();

				// create a unique directory just in case there are multiple file with the same name
				var tmpDir = new Java.IO.File(rootDir, Guid.NewGuid().ToString("N"));
				tmpDir.Mkdirs();
				tmpDir.DeleteOnExit();

				// create the new temporary file
				var tmpFile = new Java.IO.File(tmpDir, fileName);
				tmpFile.DeleteOnExit();

				return tmpFile;
			}

			public static string? EnsurePhysicalPath(AndroidUri uri, bool requireExtendedAccess = true)
			{
				// if this is a file, use that
				if (uri.Scheme?.Equals(UriSchemeFile, StringComparison.OrdinalIgnoreCase) == true)
				{
					return uri.Path;
				}

				// try resolve using the content provider
				var absolute = ResolvePhysicalPath(uri, requireExtendedAccess);

				if (!string.IsNullOrWhiteSpace(absolute) && Path.IsPathRooted(absolute))
				{
					return absolute;
				}

				var cached = CacheContentFile(uri);

				if (!string.IsNullOrWhiteSpace(cached) && Path.IsPathRooted(cached))
				{
					return cached;
				}

				throw new FileNotFoundException($"Unable to resolve absolute path or retrieve contents of URI '{uri}'.");
			}

			static string? CacheContentFile(AndroidUri uri)
			{
				if (uri.Scheme?.Equals(UriSchemeContent, StringComparison.OrdinalIgnoreCase) == false)
				{
					return null;
				}

				if (_this.Log().IsEnabled(LogLevel.Debug))
				{
					_this.Log().Debug($"Copying content URI to local cache: '{uri}'");
				}

				// open the source stream
				using var srcStream = OpenContentStream(uri, out var extension);
				if (srcStream == null)
				{
					return null;
				}

				// resolve or generate a valid destination path
#pragma warning disable CS0618
				var filename = GetColumnValue(uri, MediaStore.Files.FileColumns.DisplayName) ?? Guid.NewGuid().ToString("N");
#pragma warning restore CS0618

				if (!Path.HasExtension(filename) && !string.IsNullOrEmpty(extension))
				{
					filename = Path.ChangeExtension(filename, extension);
				}

				// create a temporary file
				var hasPermission = IsDeclaredInManifest(global::Android.Manifest.Permission.WriteExternalStorage);
				var root = hasPermission
					? Application.Context.ExternalCacheDir
					: Application.Context.CacheDir;

				if (root is null)
				{
					return null;
				}

				var tmpFile = GetTemporaryFile(root, filename);

				using var dstStream = File.Create(tmpFile.CanonicalPath);
				srcStream.CopyTo(dstStream);

				return tmpFile.CanonicalPath;
			}

			static bool IsDeclaredInManifest(string permission)
			{
				var context = Application.Context;
				if (context.PackageName is not { } packageName)
				{
					return false;
				}
#pragma warning disable CS0618, CA1416, CA1422 // Deprecated in API 33: https://developer.android.com/reference/android/content/pm/PackageManager#getPackageInfo(java.lang.String,%20int)
				var packageInfo = context.PackageManager?.GetPackageInfo(packageName, PackageInfoFlags.Permissions);
#pragma warning restore CS0618, CA1416, CA1422
				var requestedPermissions = packageInfo?.RequestedPermissions;

				return requestedPermissions?.Any(r => r.Equals(permission, StringComparison.OrdinalIgnoreCase)) ?? false;
			}

			static Stream? OpenContentStream(AndroidUri uri, out string? extension)
			{
				var isVirtual = IsVirtualFile(uri);
				if (isVirtual)
				{
					if (_this.Log().IsEnabled(LogLevel.Debug))
					{
						_this.Log().Debug($"Content URI was virtual: '{uri}'");
					}

					return GetVirtualFileStream(uri, out extension);
				}

				extension = GetFileExtension(uri);

				return Application.Context.ContentResolver?.OpenInputStream(uri);
			}

			static bool IsVirtualFile(AndroidUri uri)
			{
				if (!DocumentsContract.IsDocumentUri(Application.Context, uri))
				{
					return false;
				}

				var value = GetColumnValue(uri, DocumentsContract.Document.ColumnFlags);

				if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var flagsInt))
				{
					var flags = (DocumentContractFlags)flagsInt;
#pragma warning disable CA1416 // Introduced in API 24: https://developer.android.com/reference/android/provider/DocumentsContract.Document#FLAG_VIRTUAL_DOCUMENT
					return flags.HasFlag(DocumentContractFlags.VirtualDocument);
#pragma warning restore CA1416
				}

				return false;
			}

			static string? GetFileExtension(AndroidUri uri)
			{
				var mimeType = Application.Context.ContentResolver?.GetType(uri);

				return mimeType != null
					? MimeTypeMap.Singleton?.GetExtensionFromMimeType(mimeType)
					: null;
			}

			static Stream? GetVirtualFileStream(AndroidUri uri, out string? extension)
			{
				extension = null;
				if (Application.Context.ContentResolver is not { } resolver)
				{
					return null;
				}

				var mimeTypes = resolver.GetStreamTypes(uri, "*/*");

				if (mimeTypes?.Length >= 1)
				{
					var mimeType = mimeTypes[0];

					var stream = resolver.OpenTypedAssetFileDescriptor(uri, mimeType, null)
						?.CreateInputStream();

					extension = MimeTypeMap.Singleton?.GetExtensionFromMimeType(mimeType);

					return stream;
				}

				return null;
			}

			static string? ResolvePhysicalPath(AndroidUri uri, bool requireExtendedAccess = true)
			{
				if (uri.Scheme?.Equals(UriSchemeFile, StringComparison.OrdinalIgnoreCase) == true)
				{
					// if it is a file, then return directly
					var resolved = uri.Path;
					if (File.Exists(resolved))
					{
						return resolved;
					}
				}
				else if (!requireExtendedAccess || !OperatingSystem.IsAndroidVersionAtLeast(29))
				{
					// if this is on an older OS version, or we just need it now
					if (OperatingSystem.IsAndroidVersionAtLeast(19) && DocumentsContract.IsDocumentUri(Application.Context, uri))
					{
						var resolved = ResolveDocumentPath(uri);
						if (File.Exists(resolved))
						{
							return resolved;
						}
					}
					else if (uri.Scheme?.Equals(UriSchemeContent, StringComparison.OrdinalIgnoreCase) == true)
					{
						var resolved = ResolveContentPath(uri);
						if (File.Exists(resolved))
						{
							return resolved;
						}
					}
				}

				return null;
			}

			static string? ResolveContentPath(AndroidUri uri)
			{
				if (_this.Log().IsEnabled(LogLevel.Debug))
				{
					_this.Log().Debug($"Trying to resolve content URI: '{uri}'");
				}

				if (GetDataFilePath(uri) is string filePath)
				{
					return filePath;
				}

				if (_this.Log().IsEnabled(LogLevel.Warning))
				{
					_this.Log().Warn($"Unable to resolve content URI: '{uri}'");
				}

				return null;
			}

			static string? GetDataFilePath(AndroidUri contentUri, string? selection = null, string[]? selectionArgs = null)
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
#pragma warning disable CA1416 // Validate platform compatibility
				const string column = MediaStore.Files.FileColumns.Data;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete

				// ask the content provider for the data column, which may contain the actual file path
				var path = GetColumnValue(contentUri, column, selection, selectionArgs);
				if (!string.IsNullOrEmpty(path) && Path.IsPathRooted(path))
				{
					return path;
				}

				return null;
			}

			static string? GetColumnValue(AndroidUri contentUri, string column, string? selection = null, string[]? selectionArgs = null)
			{
				try
				{
					var value = QueryContentResolverColumn(contentUri, column, selection, selectionArgs);
					if (!string.IsNullOrEmpty(value))
					{
						return value;
					}
				}
				catch { /* Ignore all exceptions and use null for the error indicator */ }

				return null;
			}

			static string? QueryContentResolverColumn(AndroidUri contentUri, string columnName, string? selection = null, string[]? selectionArgs = null)
			{
				string? text = null;
				var projection = new[] { columnName };

				using var cursor = Application.Context.ContentResolver?.Query(contentUri, projection, selection, selectionArgs, null);
				if (cursor?.MoveToFirst() == true)
				{
					var columnIndex = cursor.GetColumnIndex(columnName);
					if (columnIndex != -1)
					{
						text = cursor.GetString(columnIndex);
					}
				}

				return text;
			}

			static string? ResolveDocumentPath(AndroidUri uri)
			{
				if (_this.Log().IsEnabled(LogLevel.Debug))
				{
					_this.Log().Debug($"Trying to resolve content URI: '{uri}'");
				}

				var docId = DocumentsContract.GetDocumentId(uri);

				var docIdParts = docId?.Split(':');
				if (docIdParts is null || docIdParts.Length == 0)
				{
					return null;
				}

				if (uri.Authority?.Equals(UriAuthorityExternalStorage, StringComparison.OrdinalIgnoreCase) == true)
				{
					if (_this.Log().IsEnabled(LogLevel.Debug))
					{
						_this.Log().Debug($"Resolving external storage URI: '{uri}'");
					}

					if (docIdParts.Length == 2)
					{
						var storageType = docIdParts[0];
						var uriPath = docIdParts[1];

						// This is the internal "external" memory, NOT the SD Card
						if (storageType.Equals(StorageTypePrimary, StringComparison.OrdinalIgnoreCase))
						{
#pragma warning disable CS0618 // Type or member is obsolete
							if (global::Android.OS.Environment.ExternalStorageDirectory?.Path is not { } root)
#pragma warning restore CS0618 // Type or member is obsolete
							{
								return null;
							}

							return Path.Combine(root, uriPath);
						}
					}
				}
				else if (uri.Authority?.Equals(UriAuthorityDownloads, StringComparison.OrdinalIgnoreCase) == true)
				{
					if (_this.Log().IsEnabled(LogLevel.Debug))
					{
						_this.Log().Debug($"Resolving downloads URI: '{uri}'");
					}

					// NOTE: This only really applies to older Android vesions since the privacy changes
					if (docIdParts.Length == 2)
					{
						var storageType = docIdParts[0];
						var uriPath = docIdParts[1];

						if (storageType.Equals(StorageTypeRaw, StringComparison.OrdinalIgnoreCase))
						{
							return uriPath;
						}
					}

					// ID could be "###" or "msf:###"
					var fileId = docIdParts.Length == 2
						? docIdParts[1]
						: docIdParts[0];

					foreach (var prefix in _contentUriPrefixes)
					{
						var uriString = $"{prefix}/{fileId}";
						var contentUri = AndroidUri.Parse(uriString);

						if (contentUri is { } && GetDataFilePath(contentUri) is string filePath)
						{
							return filePath;
						}
					}
				}
				else if (uri.Authority?.Equals(UriAuthorityMedia, StringComparison.OrdinalIgnoreCase) == true)
				{
					if (_this.Log().IsEnabled(LogLevel.Debug))
					{
						_this.Log().Debug($"Resolving media URI: '{uri}'");
					}

					if (docIdParts.Length == 2)
					{
						var storageType = docIdParts[0];
						var uriPath = docIdParts[1];

						AndroidUri? contentUri = null;

						if (storageType.Equals(StorageTypeImage, StringComparison.OrdinalIgnoreCase))
						{
							contentUri = MediaStore.Images.Media.ExternalContentUri;
						}
						else if (storageType.Equals(StorageTypeVideo, StringComparison.OrdinalIgnoreCase))
						{
							contentUri = MediaStore.Video.Media.ExternalContentUri;
						}
						else if (storageType.Equals(StorageTypeAudio, StringComparison.OrdinalIgnoreCase))
						{
							contentUri = MediaStore.Audio.Media.ExternalContentUri;
						}

#pragma warning disable CS0618
						if (contentUri is { } && GetDataFilePath(contentUri, $"{MediaStore.MediaColumns.Id}=?", new[] { uriPath }) is string filePath)
						{
							return filePath;
						}
#pragma warning restore CS0618
					}
				}

				if (_this.Log().IsEnabled(LogLevel.Debug))
				{
					_this.Log().Debug($"Unable to resolve document URI: '{uri}'");
				}

				return null;
			}
		}
	}
}
