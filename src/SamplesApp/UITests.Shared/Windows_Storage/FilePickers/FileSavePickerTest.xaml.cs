using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_Storage.FilePickers
{
	[SampleControlInfo("FilePickers", "FileSavePicker")]
	public sealed partial class FileSavePickerTest : Page
	{
		public FileSavePickerTest()
		{
			InitializeComponent();
		}

		private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
		{

			var actionsText = new StringBuilder();
			var file = await GetFile();
			actionsText.AppendLine("Got file.");

			if (file != null)
			{
				UpdateFileStatus(file);

				await WriteToFile(file);
				actionsText.AppendLine("Written to file.");

				await ReadFile(file);
				actionsText.AppendLine("File read.");
			}
			else
			{
				actionsText.AppendLine("Operation cancelled.");
			}
			OutputTextBlock.Text = actionsText.ToString();
			actionsText.Clear();
		}

		private static async Task<StorageFile> GetFile()
		{
			var savePicker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
			savePicker.FileTypeChoices.Add("plain/text", new List<string>() { ".txt" });
			savePicker.SuggestedFileName = "New Documents";
			var file = await savePicker.PickSaveFileAsync();
			return file;
		}

		private void UpdateFileStatus(StorageFile file)
		{
			FileName.Text = file.Name;
			FilePath.Text = file.Path;
		}

		private async Task WriteToFile(StorageFile file)
		{
			FileUpdateStatus.Text = "Deferring";
			CachedFileManager.DeferUpdates(file);

			var stream = await file.OpenStreamForWriteAsync();
			using (var writer = new StreamWriter(stream))
			{
				writer.Write("Hi, this is the content of the file.");
			}
			var status = await CachedFileManager.CompleteUpdatesAsync(file);
			FileUpdateStatus.Text = status.ToString();
		}

		private async Task ReadFile(StorageFile file)
		{
			var stream = await file.OpenStreamForReadAsync();
			using (var reader = new StreamReader(stream))
			{
				var fileContent = new StringBuilder();
				fileContent.AppendLine(reader.ReadLine());
				FileContent.Text = fileContent.ToString();
				fileContent.Clear();
			}
		}
	}
}
