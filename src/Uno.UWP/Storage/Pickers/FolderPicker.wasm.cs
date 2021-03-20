#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FolderPicker";

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (!IsNativePickerSupported())
			{
				throw new NotSupportedException("Could not handle the request using any picker implementation.");
			}

			var pickedFolderJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickSingleFolderAsync()");

			if (pickedFolderJson is null)
			{
				// User did not select any folder.
				return null;
			}

			var info = JsonHelper.Deserialize<NativeStorageItemInfo>(pickedFolderJson);

			return StorageFolder.GetFromNativeInfo(info, null);
		}

		private bool IsNativePickerSupported()
		{
			var isSupportedString = WebAssemblyRuntime.InvokeJS($"{JsType}.isNativeSupported()");
			return bool.TryParse(isSupportedString, out var isSupported) && isSupported;
		}
	}
}
