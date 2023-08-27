#if NET8_0_OR_GREATER
#nullable enable

using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using Microsoft.Win32;
using System;

namespace Uno.Extensions.Storage.Pickers;

partial class FolderPickerExtension
{
	public Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		var openFolderDialog = new OpenFolderDialog();

		var initialDirectory = GetPickerIdLocationPath() ??
			PickerHelpers.GetInitialDirectory(_picker.SuggestedStartLocation);

		if (initialDirectory is not null)
		{
			openFolderDialog.InitialDirectory = initialDirectory;
		}

		if (openFolderDialog.ShowDialog() == true &&
			!string.IsNullOrEmpty(openFolderDialog.FolderName))
		{
			return Task.FromResult<StorageFolder?>(new StorageFolder(openFolderDialog.FolderName));
		}

		return Task.FromResult<StorageFolder?>(null);
	}
}
#endif
