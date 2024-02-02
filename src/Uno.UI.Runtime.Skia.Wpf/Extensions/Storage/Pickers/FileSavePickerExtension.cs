#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Uno.Extensions.Storage.Pickers
{
	internal class FileSavePickerExtension : IFileSavePickerExtension
	{
		private readonly FileSavePicker _picker;

		public FileSavePickerExtension(object owner)
		{
			if (!(owner is FileSavePicker picker))
			{
				throw new InvalidOperationException("Owner of FileSavePickerExtension must be a FileSavePicker.");
			}
			_picker = picker;
		}

		public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
		{
			var saveFileDialog = new SaveFileDialog
			{
				CheckPathExists = true,
				OverwritePrompt = true,
			};

			var filterBuilder = new StringBuilder();
			foreach (var choice in _picker.FileTypeChoices)
			{
				if (filterBuilder.Length > 0)
				{
					filterBuilder.Append('|');
				}

				filterBuilder.Append(choice.Key);
				filterBuilder.Append('|');
				filterBuilder.Append(string.Join(';', choice.Value.Select(item => $"*{item}")));
			}

			saveFileDialog.Filter = filterBuilder.ToString();

			saveFileDialog.FileName = _picker.SuggestedFileName;
			saveFileDialog.InitialDirectory = PickerHelpers.GetInitialDirectory(_picker.SuggestedStartLocation);

			if (saveFileDialog.ShowDialog() == true)
			{
				if (!File.Exists(saveFileDialog.FileName))
				{
					File.Create(saveFileDialog.FileName).Dispose();
				}
				return await StorageFile.GetFileFromPathAsync(saveFileDialog.FileName);
			}
			return null;
		}
	}
}
