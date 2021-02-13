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
using SpecialFolder = System.Environment.SpecialFolder;

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

			if (_picker.FileTypeFilter.Count > 1)
			{
				// Add default entry for all item types at once
				var fullFilter = string.Join(";", _picker.FileTypeFilter.Select(fileType => $"*{fileType}"));
				filterBuilder.Append($"All|{fullFilter}");
			}

			foreach (var fileType in _picker.FileTypeFilter)
			{
				if (filterBuilder.Length != 0)
				{
					filterBuilder.Append('|');
				}
				filterBuilder.Append($"{fileType}|*{fileType}");
			}

			openFileDialog.Filter = filterBuilder.ToString();

			openFileDialog.InitialDirectory = GetInitialDirectory();

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

		private string GetInitialDirectory()
		{
			switch (_picker.SuggestedStartLocation)
			{
				case PickerLocationId.DocumentsLibrary:
					return Environment.GetFolderPath(SpecialFolder.MyDocuments);
				case PickerLocationId.ComputerFolder:
					// Special CLSID for the "virtual" Computer folder (https://www.autohotkey.com/docs/misc/CLSID-List.htm)
					return "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
				case PickerLocationId.Desktop:
					return Environment.GetFolderPath(SpecialFolder.Desktop);
				case PickerLocationId.MusicLibrary:
					return Environment.GetFolderPath(SpecialFolder.MyMusic);
				case PickerLocationId.PicturesLibrary:
					return Environment.GetFolderPath(SpecialFolder.MyPictures);
				case PickerLocationId.VideosLibrary:
					return Environment.GetFolderPath(SpecialFolder.MyVideos);
				default:
					return string.Empty;
			}
		}
	}
}
