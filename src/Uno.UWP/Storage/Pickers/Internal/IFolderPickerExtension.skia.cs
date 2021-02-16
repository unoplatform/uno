#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;

namespace Uno.Extensions.Storage.Pickers
{
	internal interface IFolderPickerExtension
	{
		Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token);
	}
}
