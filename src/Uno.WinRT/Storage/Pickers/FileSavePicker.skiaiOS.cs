#if __IOS__ || __SKIA__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Extensibility;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		private IFileSavePickerExtension? _fileSavePickerExtension;

		partial void InitializePlatform() => ApiExtensibility.CreateInstance(this, out _fileSavePickerExtension);

		private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
		{
			if (_fileSavePickerExtension == null)
			{
				throw new NotSupportedException("FileSavePicker extension is not registered.");
			}

			_fileSavePickerExtension.Customize(this);
			return await _fileSavePickerExtension.PickSaveFileAsync(token);
		}
	}
}
#endif
