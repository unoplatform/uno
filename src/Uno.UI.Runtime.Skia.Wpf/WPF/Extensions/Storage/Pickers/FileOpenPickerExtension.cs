#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal class FileOpenPickerExtension : IFileOpenPickerExtension
	{
		private readonly FileOpenPicker _picker;

		public FileOpenPickerExtension(object owner)
		{
			if (!(owner is FileOpenPicker picker))
			{
				throw new InvalidOperationException("Owner of FileOpenPickerExtension must be a FileOpenPicker.");
			}
			_picker = picker;
		}

		public async Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
		{
			var files = await OpenPickerAsync(false);
			return files.FirstOrDefault();
		}

		public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
		{
			var files = await OpenPickerAsync(true);
			return new FilePickerSelectedFilesArray(files);
		}

		private async Task<StorageFile[]> OpenPickerAsync(bool multiple)
		{
			var openFileDialog = new OpenFileDialog
			{				
				Multiselect = multiple
			};

			//openFileDialog.Filter = string.Join("|", _picker.FileTypeFilter);

			var files = new List<StorageFile>();
			if (openFileDialog.ShowDialog() == true)
			{
				foreach (var fileName in openFileDialog.FileNames)
				{
					files.Add(await StorageFile.GetFileFromPathAsync(fileName));
				}
			}
			return files.ToArray();
		}
	}
}
