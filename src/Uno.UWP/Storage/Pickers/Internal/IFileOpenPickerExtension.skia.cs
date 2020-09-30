#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.Extensions.Storage.Pickers
{
	internal interface IFileOpenPickerExtension
    {
		Task<StorageFile?> PickSingleFileAsync(CancellationToken token);

		Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token);
	}
}
