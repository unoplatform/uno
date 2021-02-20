#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation;
using System.Threading;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FolderPicker";

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			var pickedFolderJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickSingleFolderAsync()");

			if (pickedFolderJson is null)
			{
				// User did not select any folder.
				return null;
			}

			var info = JsonHelper.Deserialize<NativeStorageItemInfo>(pickedFolderJson);

			return StorageFolder.GetFromNativeInfo(info);
		}
	}
}
