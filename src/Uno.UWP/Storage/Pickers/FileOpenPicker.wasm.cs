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

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.Storage.Pickers.FileOpenPicker.NativeMethods;
#endif

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Storage.Pickers.FileOpenPicker";
#endif

		private static bool? _fileSystemAccessApiSupported;

		internal static bool IsNativePickerSupported()
		{
			if (_fileSystemAccessApiSupported is null)
			{
#if NET7_0_OR_GREATER
				_fileSystemAccessApiSupported = NativeMethods.IsNativeSupported();
#else
				var isSupportedString = WebAssemblyRuntime.InvokeJS($"{JsType}.isNativeSupported()");
				_fileSystemAccessApiSupported = bool.TryParse(isSupportedString, out var isSupported) && isSupported;
#endif
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
			var fileTypeAcceptTypesJson = JsonHelper.Serialize(fileTypeAcceptTypes);
			var startIn = SuggestedStartLocation.ToStartInDirectory();

#if NET7_0_OR_GREATER
			var nativeStorageItemInfosJson = await NativeMethods.PickFilesAsync(multiple, FileTypeFilter.Contains("*"), fileTypeAcceptTypesJson, SettingsIdentifier, startIn);
#else
			var multipleParameter = multiple ? "true" : "false";
			var showAllEntryParameter = FileTypeFilter.Contains("*") ? "true" : "false";
			var fileTypeMapParameter = WebAssemblyRuntime.EscapeJs(fileTypeAcceptTypesJson);
			var id = WebAssemblyRuntime.EscapeJs(SettingsIdentifier);
			var nativeStorageItemInfosJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.nativePickFilesAsync({multipleParameter},{showAllEntryParameter},'{fileTypeMapParameter}','{id}','{startIn}')");
#endif

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
			var temporaryFolder = ApplicationData.Current.LocalCacheFolder;
			if (!Directory.Exists(temporaryFolder.Path))
			{
				temporaryFolder.MakePersistent();
			}
			var targetFolder = Directory.CreateDirectory(Path.Combine(temporaryFolder.Path, Guid.NewGuid().ToString()));

#if NET7_0_OR_GREATER
			var fileCountString = await NativeMethods.UploadPickFilesAsync(multiple, targetFolder.FullName, BuildAcceptString());
#else
			var multipleParameter = multiple ? "true" : "false";
			var targetFolderParameter = WebAssemblyRuntime.EscapeJs(targetFolder.FullName);
			var acceptParameter = WebAssemblyRuntime.EscapeJs(BuildAcceptString());
			var jsUploadQuery = $"{JsType}.uploadPickFilesAsync({multipleParameter},'{targetFolderParameter}','{acceptParameter}')";
			var fileCountString = await WebAssemblyRuntime.InvokeAsync(jsUploadQuery);
#endif

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
