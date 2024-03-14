#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Extensibility;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private IFileOpenPickerExtension? _fileOpenPickerExtension;

		partial void InitializePlatform() => ApiExtensibility.CreateInstance(this, out _fileOpenPickerExtension);

		private async Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			if (_fileOpenPickerExtension == null)
			{
				throw new NotSupportedException("FileOpenPicker is not supported on this target.");
			}

			_fileOpenPickerExtension.Customize(this);
			return await _fileOpenPickerExtension.PickSingleFileAsync(token);
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			if (_fileOpenPickerExtension == null)
			{
				throw new NotSupportedException("FileOpenPicker is not supported on this target.");
			}

			_fileOpenPickerExtension.Customize(this);
			return await _fileOpenPickerExtension.PickMultipleFilesAsync(token);
		}
	}
}
