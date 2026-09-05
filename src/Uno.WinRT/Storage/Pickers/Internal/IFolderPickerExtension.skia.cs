#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal interface IFolderPickerExtension
	{
		Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token);

		// called just before Pick* methods to allow the customization of the native picker to match,
		// as much as possible, the user selected properties of the WinUI picker
		void Customize(FolderPicker picker)
		{
		}
	}
}
