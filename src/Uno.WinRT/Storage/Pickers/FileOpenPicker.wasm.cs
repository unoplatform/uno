#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
using Uno.Storage.Pickers;
using Uno.Storage.Pickers.Internal;

using NativeMethods = __Windows.Storage.Pickers.FileOpenPicker.NativeMethods;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private static bool? _fileSystemAccessApiSupported;
		private static readonly string[] _asteriskArray = new[] { "*" };

		internal static bool IsNativePickerSupported()
		{
			if (_fileSystemAccessApiSupported is null)
			{
				_fileSystemAccessApiSupported = NativeMethods.IsNativeSupported();
			}

			return _fileSystemAccessApiSupported.Value;
		}

		private async Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			var files = await PickFilesAsync(false, token);
			return files.Count > 0 ? files[0] : null;
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			return await PickFilesAsync(true, token);
		}

		private async Task<FilePickerSelectedFilesArray> PickFilesAsync(bool multiple, CancellationToken token)
		{
			var fileSystemAccessApiEnabled = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration
				.HasFlag(WasmPickerConfiguration.FileSystemAccessApi);
			if (fileSystemAccessApiEnabled && IsNativePickerSupported())
			{
				return await NativePickerPickFilesAsync(multiple, token);
			}

			var downloadUploadEnabled = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration
				.HasFlag(WasmPickerConfiguration.DownloadUpload);
			if (downloadUploadEnabled)
			{
				// Fallback to download-based picker.
				return await UploadPickerPickFilesAsync(multiple, token);
			}

			throw new NotSupportedException("Could not handle the request using any picker implementation.");
		}

		private async Task<FilePickerSelectedFilesArray> NativePickerPickFilesAsync(bool multiple, CancellationToken token)
		{
			var fileTypeAcceptTypes = BuildFileTypesMap();
			var fileTypeAcceptTypesJson = JsonHelper.Serialize(fileTypeAcceptTypes, StorageSerializationContext.Default);
			var startIn = SuggestedStartLocation.ToStartInDirectory();

			var nativeStorageItemInfosJson = await NativeMethods.PickFilesAsync(multiple, FileTypeFilter.Contains("*"), fileTypeAcceptTypesJson, SettingsIdentifier, startIn);

			var infos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(nativeStorageItemInfosJson, StorageSerializationContext.Default);

			var results = new List<StorageFile>();
			foreach (var info in infos)
			{
				var storageFile = StorageFile.GetFromNativeInfo(info);
				results.Add(storageFile);
			}

			return new FilePickerSelectedFilesArray(results.ToArray());
		}

		private NativeFilePickerAcceptType[] BuildFileTypesMap()
		{
			var allExtensions = FileTypeFilter.Except(_asteriskArray);

			var acceptTypes = allExtensions
				.Select(fileType => BuildNativeFilePickerAcceptType(fileType))
				.ToList();

			if (!FileTypeFilter.Contains("*"))
			{
				var fullAcceptItem = new NativeFilePickerAcceptTypeItem
				{
					MimeType = "*/*",
					Extensions = allExtensions.ToArray()
				};

				var fullAcceptType = new NativeFilePickerAcceptType()
				{
					Description = "All files",
					Accept = new[] { fullAcceptItem }
				};

				acceptTypes.Insert(0, fullAcceptType);
			}

			return acceptTypes.ToArray();
		}

		private NativeFilePickerAcceptType BuildNativeFilePickerAcceptType(string fileType)
		{
			var acceptItem = new NativeFilePickerAcceptTypeItem
			{
				// This generic MIME type prevents unrelated
				// extensions from showing up in some browsers.
				MimeType = "*/*",
				Extensions = new[] { fileType }
			};
			return new NativeFilePickerAcceptType
			{
				// An empty string is consistent with UWP implementation.
				// However, some browsers show a generic description when 
				// this string is empty.
				Description = string.Empty,
				Accept = new[] { acceptItem }
			};
		}

		private async Task<FilePickerSelectedFilesArray> UploadPickerPickFilesAsync(bool multiple, CancellationToken token)
		{
			var temporaryFolder = ApplicationData.Current.LocalCacheFolder;
			if (!Directory.Exists(temporaryFolder.Path))
			{
				await temporaryFolder.MakePersistentAsync();
			}
			var targetFolder = Directory.CreateDirectory(Path.Combine(temporaryFolder.Path, Guid.NewGuid().ToString()));

			var fileCountString = await NativeMethods.UploadPickFilesAsync(multiple, targetFolder.FullName, BuildAcceptString());

			if (int.TryParse(fileCountString, CultureInfo.InvariantCulture, out var fileCount))
			{
				var files = targetFolder
					.GetFiles()
					.Select(f => StorageFile.GetFileFromPath(f.FullName))
					.ToArray();

				return new FilePickerSelectedFilesArray(files);
			}
			return new FilePickerSelectedFilesArray(Array.Empty<StorageFile>());
		}

		private string BuildAcceptString()
		{
			var mimeTypes = new HashSet<string>();
			foreach (var fileExtension in FileTypeFilter)
			{
				if (fileExtension == "*")
				{
					continue;
				}

				var mimeType = MimeTypeService.GetFromExtension(fileExtension);
				mimeTypes.Add(mimeType);
			}

			if (mimeTypes.Count == 0)
			{
				// No restriction
				return string.Empty;
			}

			return string.Join(", ", mimeTypes);
		}
	}
}
