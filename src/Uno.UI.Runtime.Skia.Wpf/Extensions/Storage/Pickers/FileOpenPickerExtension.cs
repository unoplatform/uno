#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

			var filterBuilder = new StringBuilder();
			var all = false;
			foreach (var fileType in _picker.FileTypeFilter)
			{
				if (fileType == "*")
				{
					all = true;
				}
				else
				{
					filterBuilder.Append($"|{fileType}|*{fileType}");
				}
			}

			if (all)
			{
				filterBuilder.Append("|All|*");
			}
			else
			{
				var fullFilter = string.Join(";", _picker.FileTypeFilter.Select(fileType => $"*{fileType}"));
				filterBuilder.Append($"|All|{fullFilter}");
			}


			openFileDialog.Filter = filterBuilder.ToString(1, filterBuilder.Length - 1);
			openFileDialog.FilterIndex = _picker.FileTypeFilter.Count > 1 ? _picker.FileTypeFilter.Count : 0;

			openFileDialog.InitialDirectory = PickerHelpers.GetInitialDirectory(_picker.SuggestedStartLocation);

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
