#if __SKIA__ || __IOS__
#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;

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

		// called just before Pick* methods to allow the customization of the native picker to match,
		// as much as possible, the user selected properties of the WinUI picker
		void Customize(FileSavePicker picker)
		{
		}
	}
}
#endif
