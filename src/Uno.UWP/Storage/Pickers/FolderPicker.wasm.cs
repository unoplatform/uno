#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation;
using System.Threading;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FolderPicker";

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			var returnValue = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickSingleFolderAsync()");

			if (returnValue is null)
			{
				// User did not select any folder.
				return null;
			}

			if (!Guid.TryParse(returnValue, out var guid))
			{
				throw new InvalidOperationException("GUID could not be parsed");
			}

			return StorageFolder.GetFolderFromNativePathAsync("", guid);
		}
	}
}
