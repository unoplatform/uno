#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;

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
			var returnValue = await WebAssemblyRuntime.InvokeAsync(FormattableString.Invariant($"{JsType}.pickFilesAsync({multiple})"));
			return new FilePickerSelectedFilesArray(Array.Empty<StorageFile>());
			//if (returnValue is null)
			//{
				
			//}

			//if (!Guid.TryParse(returnValue, out var guid))
			//{
			//	throw new InvalidOperationException("GUID could not be parsed");
			//}

			//return StorageFolder.GetFolderFromNativePathAsync("", guid);
		}
	}
}
