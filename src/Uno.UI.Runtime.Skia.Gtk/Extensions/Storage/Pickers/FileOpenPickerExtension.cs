#nullable enable

using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.Gtk;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal class FileOpenPickerExtension : IFileOpenPickerExtension
	{
		private readonly FileOpenPicker _picker;

		public FileOpenPickerExtension(FileOpenPicker owner)
		{
			_picker = owner ?? throw new ArgumentNullException(nameof(owner));
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
			string commitText = "Open";
			if (!string.IsNullOrWhiteSpace(_picker.CommitButtonText))
			{
				commitText = _picker.CommitButtonText;
			}

			using FileChooserDialog dialog = new FileChooserDialog(
				"Open",
				GtkHost.Current!.InitialWindow,
				FileChooserAction.Open,
				"Cancel", ResponseType.Cancel,
				commitText, ResponseType.Accept);

			dialog.SelectMultiple = multiple;

			if (!_picker.FileTypeFilter.Contains("*"))
			{
				FileFilter filter = new FileFilter();

				foreach (string pattern in _picker.FileTypeFilter)
				{
					// Pattern is already validated to start with a period, so prepend star
					filter.AddPattern("*" + pattern);
				}

				if (_picker.FileTypeFilter.Count > 0)
				{
					dialog.AddFilter(filter);
				}
			}

			dialog.SetCurrentFolder(PickerHelpers.GetInitialDirectory(_picker.SuggestedStartLocation));

			var files = new List<StorageFile>();
			if (dialog.Run() == (int)ResponseType.Accept)
			{
				foreach (var fileName in dialog.Filenames)
				{
					files.Add(await StorageFile.GetFileFromPathAsync(fileName));
				}
			}

			return files.ToArray();
		}
	}
}
