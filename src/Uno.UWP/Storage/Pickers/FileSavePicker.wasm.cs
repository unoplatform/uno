#nullable enable

using System;
using System.Collections.Generic;
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

using NativeMethods = __Windows.Storage.Pickers.FileSavePicker.NativeMethods;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		private static bool? _fileSystemAccessApiSupported;

		internal static bool IsNativePickerSupported()
		{
			if (_fileSystemAccessApiSupported is null)
			{
				_fileSystemAccessApiSupported = NativeMethods.IsNativeSupported();
			}

			return _fileSystemAccessApiSupported.Value;
		}

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

		private async Task<StorageFile?> NativePickerPickSaveFileAsync(CancellationToken token)
		{
			var fileTypeMapParameter = JsonHelper.Serialize(BuildFileTypesMap(), StorageSerializationContext.Default);
			var startIn = SuggestedStartLocation.ToStartInDirectory();

			var nativeStorageItemInfo = await NativeMethods.PickSaveFileAsync(true, fileTypeMapParameter, SuggestedFileName, SettingsIdentifier, startIn);
			if (nativeStorageItemInfo is null)
			{
				return null;
			}

			var info = JsonHelper.Deserialize<NativeStorageItemInfo>(nativeStorageItemInfo, StorageSerializationContext.Default);
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
					await temporaryFolder.MakePersistentAsync();
				}

				if (string.IsNullOrEmpty(SuggestedFileName))
				{
					SuggestedFileName = Guid.NewGuid().ToString();
				}

				var extension = FileTypeChoices.Count > 0 ? FileTypeChoices.First().Value[0] : "";

				// The mime type is chosen by the extension, and we cannot reliably send multiple mime type in the browser
				var fileName = SuggestedFileName + extension;
				SuggestedSaveFile = await temporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
				SuggestedSaveFile.ProviderOverride = StorageProviders.WasmDownloadPicker;
			}
			return SuggestedSaveFile;
		}
	}
}
