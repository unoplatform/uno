#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
using Uno.Storage.Pickers;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.Storage.Pickers.FolderPicker.NativeMethods;
#endif

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Storage.Pickers.FolderPicker";
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

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (!IsNativePickerSupported())
			{
				throw new NotSupportedException("Could not handle the request using any picker implementation.");
			}

			var startIn = SuggestedStartLocation.ToStartInDirectory();

#if NET7_0_OR_GREATER
			var pickedFolderJson = await NativeMethods.PickSingleFolderAsync(SettingsIdentifier, startIn);
#else
			var id = WebAssemblyRuntime.EscapeJs(SettingsIdentifier);
			var pickedFolderJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickSingleFolderAsync('{id}','{startIn}')");
#endif

			if (pickedFolderJson is null)
			{
				// User did not select any folder.
				return null;
			}

			var info = JsonHelper.Deserialize<NativeStorageItemInfo>(pickedFolderJson);

			return StorageFolder.GetFromNativeInfo(info, null);
		}
	}
}
