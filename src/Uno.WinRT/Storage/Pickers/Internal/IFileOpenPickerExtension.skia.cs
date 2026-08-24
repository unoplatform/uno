#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal interface IFileOpenPickerExtension
	{
		Task<StorageFile?> PickSingleFileAsync(CancellationToken token);

		Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token);

		// called just before Pick* methods to allow the customization of the native picker to match,
		// as much as possible, the user selected properties of the WinUI picker
		void Customize(FileOpenPicker picker)
		{
		}
	}
}
