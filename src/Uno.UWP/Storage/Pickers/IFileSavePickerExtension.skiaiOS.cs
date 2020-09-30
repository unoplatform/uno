#if __SKIA__ || __IOS__
#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;

namespace Uno.Extensions.Storage.Pickers
{
	/// <summary>
	/// Provides an extension point to allow custom implementation of
	/// the FileSavePicker handling. The implementations must have
	/// a public constructor with a single object parameter.
	/// This parameter will contain the FileSavePicker instance that
	/// triggered the request.
	/// </summary>
	public interface IFileSavePickerExtension
	{
		Task<StorageFile?> PickSaveFileAsync(CancellationToken token);
	}
}
#endif
