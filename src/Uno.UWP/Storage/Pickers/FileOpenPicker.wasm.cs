#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
using Uno.Storage.Pickers;
using Uno.Storage.Pickers.Internal;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FileOpenPicker";

		private static bool? _fileSystemAccessApiSupported;

		internal static bool IsNativePickerSupported()
		{
			if (_fileSystemAccessApiSupported is null)
			{
				var isSupportedString = WebAssemblyRuntime.InvokeJS($"{JsType}.isNativeSupported()");
				_fileSystemAccessApiSupported = bool.TryParse(isSupportedString, out var isSupported) && isSupported;
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
			var showAllEntryParameter = FileTypeFilter.Contains("*") ? "true" : "false";
			var multipleParameter = multiple ? "true" : "false";
			var fileTypeAcceptTypes = BuildFileTypesMap();
			var fileTypeAcceptTypesJson = JsonHelper.Serialize(fileTypeAcceptTypes);
			var fileTypeMapParameter = WebAssemblyRuntime.EscapeJs(fileTypeAcceptTypesJson);
			var id = WebAssemblyRuntime.EscapeJs(SettingsIdentifier);
			var startIn = SuggestedStartLocation.ToStartInDirectory();
			var nativeStorageItemInfosJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.nativePickFilesAsync({multipleParameter},{showAllEntryParameter},'{fileTypeMapParameter}','{id}','{startIn}')");
			var infos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(nativeStorageItemInfosJson);

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
			var allExtensions = FileTypeFilter.Except(new[] { "*" });

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
			var multipleParameter = multiple ? "true" : "false";
			var acceptParameter = WebAssemblyRuntime.EscapeJs(BuildAcceptString());
			var temporaryFolder = ApplicationData.Current.LocalCacheFolder;
			if (!Directory.Exists(temporaryFolder.Path))
			{
				temporaryFolder.MakePersistent();
			}
			var targetFolder = Directory.CreateDirectory(Path.Combine(temporaryFolder.Path, Guid.NewGuid().ToString()));
			var targetFolderParameter = WebAssemblyRuntime.EscapeJs(targetFolder.FullName);
			var jsUploadQuery = $"{JsType}.uploadPickFilesAsync({multipleParameter},'{targetFolderParameter}','{acceptParameter}')";
			var fileCountString = await WebAssemblyRuntime.InvokeAsync(jsUploadQuery);
			if (int.TryParse(fileCountString, out var fileCount))
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
				if (!mimeTypes.Contains(mimeType))
				{
					mimeTypes.Add(mimeType);
				}
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
