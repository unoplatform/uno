using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

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
			// Clear previous returned file name, if it exists, between iterations of this scenario
			OutputTextBlock.Text = "";

			var savePicker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
			// Dropdown of file types the user can save the file as
			savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
			// Default file name if the user does not type one in or select a file to replace
			savePicker.SuggestedFileName = "New Document";
			var file = await savePicker.PickSaveFileAsync();
			if (file != null)
			{
				// Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
				CachedFileManager.DeferUpdates(file);
				// write to file
				await FileIO.WriteTextAsync(file, "Example file contents.");
				// Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
				// Completing updates may require Windows to ask for user input.
				var status = await CachedFileManager.CompleteUpdatesAsync(file);
				OutputTextBlock.Text = status switch
				{
					FileUpdateStatus.Complete => "File " + file.Name + " was saved.",
					FileUpdateStatus.CompleteAndRenamed => "File " + file.Name + " was renamed and saved.",
					_ => "File " + file.Name + " couldn't be saved."
				};
			}
			else
			{
				OutputTextBlock.Text = "Operation cancelled.";
			}
		}
	}
}
