using System;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.Storage.Pickers;
using System.Text;
using System.Collections.Generic;

namespace UITests.Shared.Windows_Storage
{
	[SampleControlInfo("Windows.Storage")]
	public sealed partial class FilePickerTests : Page
	{
		public FilePickerTests()
		{
			this.InitializeComponent();
		}

		private async void PickOpenSingle_Click(object sender, RoutedEventArgs args)
		{
			var picker = new FileOpenPicker();
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
			picker.FileTypeFilter.Add(".jpg");
			var result = await picker.PickSingleFileAsync();
			if (result == null) {
				PickOpenResult.Text = "No file was picked";
			}
			else
			{
				PickOpenResult.Text = result.Path;
			}
		}

		private async void PickSaveSingle_Click(object sender, RoutedEventArgs args)
		{
			var picker = new FileSavePicker();
			picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
			picker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
			picker.SuggestedFileName = "New Document";
			var result = await picker.PickSaveFileAsync();
			if (result == null)
			{
				PickOpenResult.Text = "No file was picked";
			}
			else
			{
				PickOpenResult.Text = result.Path;
			}
		}

		private async void PickOpenMultiple_Click(object sender, RoutedEventArgs args)
		{
			var picker = new FileOpenPicker();
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
			picker.FileTypeFilter.Add(".jpg");
			var result = await picker.PickMultipleFilesAsync();
			if (result.Count == 0)
			{
				PickOpenResult.Text = "No file was picked";
			}
			else
			{
				var builder = new StringBuilder();
				foreach (var file in result)
				{
					builder.AppendLine(file.Path);
				}
				PickOpenResult.Text = builder.ToString();
			}
		}
	}
}
