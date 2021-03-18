#if __SKIA__ || __IOS__
#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;

namespace Uno.Extensions.Storage.Pickers
{
	public interface IFileSavePickerExtension
    {
		Task<StorageFile?> PickSaveFileAsync(CancellationToken token);
	}
}
#endif
