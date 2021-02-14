using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FolderPicker";

		public FolderPicker()
		{
		}

		public IAsyncOperation<StorageFolder> PickSingleFolderAsync() =>
			PickSingleFolderImplAsync().AsAsyncOperation();

		private async Task<StorageFolder> PickSingleFolderImplAsync()
		{
			var returnValue = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickSingleFolderAsync()");

			if (returnValue is null)
			{
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
