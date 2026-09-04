#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Extensibility;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		private IFolderPickerExtension? _folderPickerExtension;

		partial void InitializePlatform() => ApiExtensibility.CreateInstance(this, out _folderPickerExtension);

		private async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (_folderPickerExtension == null)
			{
				throw new NotSupportedException("FolderPicker is not supported on this target.");
			}

			_folderPickerExtension.Customize(this);
			return await _folderPickerExtension.PickSingleFolderAsync(token);
		}
	}
}
