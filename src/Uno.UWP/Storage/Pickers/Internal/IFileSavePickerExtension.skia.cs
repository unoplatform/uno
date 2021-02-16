#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;

namespace Uno.Extensions.Storage.Pickers
{
	internal interface IFileSavePickerExtension
    {
		Task<StorageFile?> PickSaveFileAsync(CancellationToken token);
	}
}
