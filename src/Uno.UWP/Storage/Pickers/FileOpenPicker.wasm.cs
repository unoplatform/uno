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
using Uno.Storage.Pickers.Internal;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FileOpenPicker";

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

		private bool IsNativePickerSupported()
		{
			var isSupportedString = WebAssemblyRuntime.InvokeJS($"{JsType}.isNativeSupported()");
			return bool.TryParse(isSupportedString, out var isSupported) && isSupported;
		}

		private async Task<FilePickerSelectedFilesArray> NativePickerPickFilesAsync(bool multiple, CancellationToken token)
		{
			var showAllEntryParameter = FileTypeFilter.Contains("*") ? "true" : "false";
			var multipleParameter = multiple ? "true" : "false";
			var fileTypeAcceptTypes = BuildFileTypesMap();
			var fileTypeAcceptTypesJson = JsonHelper.Serialize(fileTypeAcceptTypes);
			var fileTypeMapParameter = WebAssemblyRuntime.EscapeJs(fileTypeAcceptTypesJson);

			var nativeStorageItemInfosJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.nativePickFilesAsync({multipleParameter},{showAllEntryParameter},'{fileTypeMapParameter}')");
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
			var acceptTypes = new List<NativeFilePickerAcceptType>();

			var mimeTypeGroups = FileTypeFilter
				.Except(new[] { "*" })
				.GroupBy(f => MimeTypeService.GetFromExtension(f))
				.ToArray();

			var allAccepts = new List<NativeFilePickerAcceptTypeItem>();

			foreach (var mimeTypeGroup in mimeTypeGroups)
			{
				var extensions = mimeTypeGroup.ToArray();

				var acceptType = new NativeFilePickerAcceptType();
				acceptType.Description = extensions.Length > 1 ? string.Empty : extensions.First();

				var acceptItem = new NativeFilePickerAcceptTypeItem()
				{
					MimeType = mimeTypeGroup.Key,
					Extensions = extensions
				};
				allAccepts.Add(acceptItem);

				acceptType.Accept = new[] { acceptItem };
				acceptTypes.Add(acceptType);
			}

			if (allAccepts.Count > 1)
			{
				var fullAcceptType = new NativeFilePickerAcceptType()
				{
					Description = "All",
					Accept = allAccepts.ToArray()
				};

				acceptTypes.Insert(0, fullAcceptType);
			}

			return acceptTypes.ToArray();
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
