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
	public partial class FileSavePicker
	{
		private const string JsType = "Windows.Storage.Pickers.FileSavePicker";

		private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
		{
			var fileSystemAccessApiEnabled = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration
				.HasFlag(WasmPickerConfiguration.FileSystemAccessApi);
			if (fileSystemAccessApiEnabled && IsNativePickerSupported())
			{
				return await NativePickerPickSaveFileAsync(token);
			}

			var downloadUploadEnabled = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration
				.HasFlag(WasmPickerConfiguration.DownloadUpload);
			if (downloadUploadEnabled)
			{
				// Fallback to download-based picker.
				return await DownloadPickerPickSaveFileAsync(token);
			}

			throw new NotSupportedException("Could not handle the request using any picker implementation.");
		}

		private bool IsNativePickerSupported()
		{
			var isSupportedString = WebAssemblyRuntime.InvokeJS($"{JsType}.isNativeSupported()");
			return bool.TryParse(isSupportedString, out var isSupported) && isSupported;
		}

		private async Task<StorageFile?> NativePickerPickSaveFileAsync(CancellationToken token)
		{
			var showAllEntryParameter = "true";
			var fileTypeMapParameter = JsonHelper.Serialize(BuildFileTypesMap());

			var promise = $"{JsType}.nativePickSaveFileAsync({showAllEntryParameter},'{WebAssemblyRuntime.EscapeJs(fileTypeMapParameter)}')";
			var nativeStorageItemInfo = await WebAssemblyRuntime.InvokeAsync(promise);
			if (nativeStorageItemInfo is null)
			{
				return null;
			}

			var info = JsonHelper.Deserialize<NativeStorageItemInfo>(nativeStorageItemInfo);
			return StorageFile.GetFromNativeInfo(info);
		}

		private NativeFilePickerAcceptType[] BuildFileTypesMap()
		{
			var acceptTypes = new List<NativeFilePickerAcceptType>();

			foreach (var choice in FileTypeChoices)
			{
				var acceptType = new NativeFilePickerAcceptType();
				acceptType.Description = choice.Key;

				var acceptItem = new NativeFilePickerAcceptTypeItem() { MimeType = "*/*", Extensions = choice.Value.ToArray() };

				acceptType.Accept = new NativeFilePickerAcceptTypeItem[] { acceptItem };
				acceptTypes.Add(acceptType);
			}

			return acceptTypes.ToArray();
		}

		private async Task<StorageFile?> DownloadPickerPickSaveFileAsync(CancellationToken token)
		{
			if (SuggestedSaveFile == null)
			{
				var temporaryFolder = ApplicationData.Current.LocalCacheFolder;
				if (!Directory.Exists(temporaryFolder.Path))
				{
					temporaryFolder.MakePersistent();
				}

				if (string.IsNullOrEmpty(SuggestedFileName))
				{
					SuggestedFileName = Guid.NewGuid().ToString();
				}

				var extension = FileTypeChoices.Count > 0 ? FileTypeChoices.First().Value[0] : "";

				// The mime type is chosen by the extension, and we cannot reliably send multiple mime type in the browser
				var fileName = SuggestedFileName + extension;
				SuggestedSaveFile = await temporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
			}
			return SuggestedSaveFile;
		}
	}
}
