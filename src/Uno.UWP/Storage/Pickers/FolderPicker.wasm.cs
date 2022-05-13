#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
using Uno.Storage.Pickers;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FolderPicker";

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

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (!IsNativePickerSupported())
			{
				throw new NotSupportedException("Could not handle the request using any picker implementation.");
			}

			var id = WebAssemblyRuntime.EscapeJs(SettingsIdentifier);
			var startIn = SuggestedStartLocation.ToStartInDirectory();

			var pickedFolderJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickSingleFolderAsync('{id}','{startIn}')");

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
