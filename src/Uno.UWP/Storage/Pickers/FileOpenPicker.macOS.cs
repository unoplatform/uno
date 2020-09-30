#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private const int ModalResponseOk = 1;

		private Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			var files = PickFiles(false, token);
			return Task.FromResult<StorageFile?>(files.FirstOrDefault());
		}

		private Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token) =>
			Task.FromResult<IReadOnlyList<StorageFile>>(PickFiles(true, token));

		private FilePickerSelectedFilesArray PickFiles(bool pickMultiple, CancellationToken token)
		{
			var openPanel = new NSOpenPanel
			{
				CanChooseFiles = true,
				CanChooseDirectories = false,
				AllowsMultipleSelection = pickMultiple,
				AllowsOtherFileTypes = FileTypeFilter.Contains("*")
			};

			if (!openPanel.AllowsOtherFileTypes)
			{
				var fileTypes = GetFileTypes();
				openPanel.AllowedFileTypes = fileTypes;
			}

			if (!string.IsNullOrEmpty(CommitButtonText))
			{
				openPanel.Prompt = CommitButtonText;
			}

			var result = openPanel.RunModal();

			if (result == ModalResponseOk)
			{
				if (openPanel.Urls != null)
				{
					var files = openPanel.Urls
						.Where(url => url?.Path != null)
						.Select(url => StorageFile.GetFileFromPath(url.Path))
						.ToArray();
					return new FilePickerSelectedFilesArray(files);
				}
			}
			return FilePickerSelectedFilesArray.Empty;
		}

		private string[] GetFileTypes()
		{
			return FileTypeFilter.Except(new[] {"*"}).Select(ext => ext.TrimStart(new []{'.'})).ToArray();
		}
	}
}
