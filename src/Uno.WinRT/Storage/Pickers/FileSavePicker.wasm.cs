#nullable enable

using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Gets whether <see cref="PickSaveFileAsync"/> has an implementation it can use under the current
		/// <see cref="WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration"/>.
		/// </summary>
		internal static bool IsSupported() => GetImplementation() is not null;

		/// <summary>
		/// Gets the implementation <see cref="PickSaveFileAsync"/> uses under the current
		/// <see cref="WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration"/>, or <see langword="null"/> if there is none.
		/// </summary>
		private static WasmPickerConfiguration? GetImplementation()
		{
			var configuration = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration;

			if (configuration.HasFlag(WasmPickerConfiguration.FileSystemAccessApi) && IsNativePickerSupported())
			{
				return WasmPickerConfiguration.FileSystemAccessApi;
			}

			// Fallback to download-based picker.
			return configuration.HasFlag(WasmPickerConfiguration.DownloadUpload) ? WasmPickerConfiguration.DownloadUpload : null;
		}

		private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
		{
			switch (GetImplementation())
			{
				case WasmPickerConfiguration.FileSystemAccessApi:
					return await NativePickerPickSaveFileAsync(token);

				case WasmPickerConfiguration.DownloadUpload:
					return await DownloadPickerPickSaveFileAsync(token);

				default:
					throw new NotSupportedException("Could not handle the request using any picker implementation.");
			}
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

		private Task<StorageFile?> DownloadPickerPickSaveFileAsync(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();

			if (SuggestedSaveFile == null)
			{
				if (string.IsNullOrEmpty(SuggestedFileName))
				{
					SuggestedFileName = Guid.NewGuid().ToString();
				}

				var extension = FileTypeChoices.Count > 0 ? FileTypeChoices.First().Value[0] : "";

				// The mime type is chosen by the extension, and we cannot reliably send multiple mime type in the browser
				var fileName = SuggestedFileName + extension;

				// The content is staged in a chunked JS-side buffer rather than a file in the
				// in-memory filesystem, to keep large payloads off the tab's memory ceilings.
				SuggestedSaveFile = StorageFile.GetForDownloadPicker(fileName);
			}
			return Task.FromResult<StorageFile?>(SuggestedSaveFile);
		}
	}
}
