using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
			actionsText.AppendLine("Button Clicked.");
			var savePicker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
			savePicker.FileTypeChoices.Add("text/plain", new List<string>() { ".txt" });
			savePicker.SuggestedFileName = "New Documents";
			var file = await savePicker.PickSaveFileAsync();

			if (file != null)
			{
				UpdateFileStatus(actionsText, file);
				WriteToFile(actionsText, file);
				ReadFile(actionsText, file);
				//{
				//	FileUpdateStatus.Complete => "File " + file.Name + " was saved.",
				//	FileUpdateStatus.CompleteAndRenamed => "File " + file.Name + " was renamed and saved.",
				//	_ => "File " + file.Name + " couldn't be saved."
				//};
			}
			else
			{
				actionsText.AppendLine("Operation cancelled.");
			}
			OutputTextBlock.Text = actionsText.ToString();
			actionsText.Clear();
		}

		private void UpdateFileStatus(StringBuilder actionsText, StorageFile file)
		{
			FileName.Text = file.Name;
			FilePath.Text = file.Path;
			actionsText.AppendLine("File created.");
		}

		private static void WriteToFile(StringBuilder actionsText, Windows.Storage.StorageFile file)
		{
			// Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
			CachedFileManager.DeferUpdates(file);

			// write to file
			using (var writer = new BinaryWriter(File.Create(file.Path)))
			{
				for (var i = 0; i < 11; i++)
				{
					writer.Write(i);
				}
			}

			//// Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
			//// Completing updates may require Windows to ask for user input.
			//var status = await CachedFileManager.CompleteUpdatesAsync(file);
			actionsText.AppendLine("Written to file.");
		}

		private void ReadFile(StringBuilder actionsText, Windows.Storage.StorageFile file)
		{
			using (var reader = new BinaryReader(File.OpenRead(file.Path)))
			{
				var fileContent = new StringBuilder();
				for (var i = 0; i < 11; i++)
				{
					fileContent.AppendLine(reader.ReadInt32().ToString());
					FileContent.Text = fileContent.ToString();
				}
				fileContent.Clear();
			}
			actionsText.AppendLine("File read.");
		}
	}
}
