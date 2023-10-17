#nullable enable

using Gtk;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.Gtk;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal class FileSavePickerExtension : IFileSavePickerExtension
	{
		private readonly FileSavePicker _picker;

		public FileSavePickerExtension(FileSavePicker owner)
		{
			_picker = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
		{
			string commitText = "Save File";
			if (!string.IsNullOrWhiteSpace(_picker.CommitButtonText))
			{
				commitText = _picker.CommitButtonText;
			}

			using FileChooserDialog dialog = new FileChooserDialog(
			"Save File",
			GtkHost.Current!.InitialWindow,
			FileChooserAction.Save,
			"Cancel", ResponseType.Cancel,
			commitText, ResponseType.Accept);

			dialog.SelectMultiple = false;
			dialog.SetFilename(_picker.SuggestedFileName);
			dialog.SetCurrentFolder(PickerHelpers.GetInitialDirectory(_picker.SuggestedStartLocation));

			StorageFile? file = null;
			if (dialog.Run() == (int)ResponseType.Accept)
			{
				if (!File.Exists(dialog.Filename))
				{
					File.Create(dialog.Filename).Dispose();
				}
				file = await StorageFile.GetFileFromPathAsync(dialog.Filename);
			}


			return file;
		}
	}
}
