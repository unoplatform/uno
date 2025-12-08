using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample(
		Description = "Test page for external drag and drop support. Drop files, images, text, or HTML content from external applications.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_ExternalSource : UserControl
	{
		public DragDrop_ExternalSource()
		{
			this.InitializeComponent();

			DropTarget.DragEnter += DropTarget_DragEnter;
			DropTarget.DragOver += DropTarget_DragOver;
			DropTarget.DragLeave += DropTarget_DragLeave;
			DropTarget.Drop += DropTarget_Drop;
		}

		private void DropTarget_DragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
		{
			// Visual feedback when drag enters
			DropTarget.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
				Windows.UI.Color.FromArgb(255, 200, 230, 255));
			DropTarget.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
				Windows.UI.Color.FromArgb(255, 0, 120, 215));

			// Accept all operations
			e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Link;
		}

		private void DropTarget_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Link;
		}

		private void DropTarget_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
		{
			// Restore original appearance
			DropTarget.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
				Windows.UI.Color.FromArgb(255, 232, 244, 248));
			DropTarget.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
				Windows.UI.Color.FromArgb(255, 30, 144, 255));
		}

		private async void DropTarget_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
		{
			// Restore original appearance
			DropTarget_DragLeave(sender, e);

			// Hide drop indicator and show clear button
			DropIndicator.Visibility = Visibility.Collapsed;
			ClearButton.Visibility = Visibility.Visible;

			var dataView = e.DataView;

			// Display available formats
			var formats = dataView.AvailableFormats;
			FormatsText.Text = string.Join(", ", formats);
			InfoPanel.Visibility = Visibility.Visible;

			// Display requested operations
			OperationsText.Text = e.DataView.RequestedOperation.ToString();

			// Try to handle storage items (files)
			if (dataView.Contains(StandardDataFormats.StorageItems))
			{
				try
				{
					var items = await dataView.GetStorageItemsAsync();
					if (items.Any())
					{
						var fileNames = new List<string>();
						foreach (var item in items)
						{
							if (item is IStorageFile file)
							{
								fileNames.Add(file.Path);

								// Check if it's an image file
								var extension = Path.GetExtension(file.Name).ToLowerInvariant();
								if (IsImageFile(extension))
								{
									await DisplayImageFromFile(file);
								}
								// Check if it's a text or HTML file
								else if (extension == ".txt" || extension == ".html" || extension == ".htm")
								{
									await DisplayTextFromFile(file, extension == ".txt");
								}
							}
						}

						if (fileNames.Any())
						{
							FilesList.ItemsSource = fileNames;
							FilePanel.Visibility = Visibility.Visible;
						}
					}
				}
				catch (Exception ex)
				{
					ShowError($"Error reading files: {ex.Message}");
				}
			}

			// Try to handle bitmap (image data)
			if (dataView.Contains(StandardDataFormats.Bitmap))
			{
				try
				{
					var bitmapRef = await dataView.GetBitmapAsync();
					if (bitmapRef != null)
					{
						using var stream = await bitmapRef.OpenReadAsync();
						var bitmap = new BitmapImage();
						await bitmap.SetSourceAsync(stream);
						ImagePreview.Source = bitmap;
						ImagePanel.Visibility = Visibility.Visible;
					}
				}
				catch (Exception ex)
				{
					ShowError($"Error displaying image: {ex.Message}");
				}
			}

			// Try to handle text
			if (dataView.Contains(StandardDataFormats.Text))
			{
				try
				{
					var text = await dataView.GetTextAsync();
					if (!string.IsNullOrEmpty(text))
					{
						TextContent.Text = text;
						TextPanel.Visibility = Visibility.Visible;
					}
				}
				catch (Exception ex)
				{
					ShowError($"Error reading text: {ex.Message}");
				}
			}

			// Try to handle HTML
			if (dataView.Contains(StandardDataFormats.Html))
			{
				try
				{
					var html = await dataView.GetHtmlFormatAsync();
					if (!string.IsNullOrEmpty(html))
					{
						HtmlContent.Text = html;
						HtmlPanel.Visibility = Visibility.Visible;
					}
				}
				catch (Exception ex)
				{
					ShowError($"Error reading HTML: {ex.Message}");
				}
			}

			e.Handled = true;
		}

		private bool IsImageFile(string extension)
		{
			return extension == ".png" || extension == ".jpg" || extension == ".jpeg" ||
				   extension == ".gif" || extension == ".bmp" || extension == ".tiff" ||
				   extension == ".ico" || extension == ".webp";
		}

		private async Task DisplayImageFromFile(IStorageFile file)
		{
			try
			{
				using var stream = await file.OpenReadAsync();
				var bitmap = new BitmapImage();
				await bitmap.SetSourceAsync(stream);
				ImagePreview.Source = bitmap;
				ImagePanel.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				ShowError($"Error loading image from file: {ex.Message}");
			}
		}

		private async Task DisplayTextFromFile(IStorageFile file, bool isPlainText)
		{
			try
			{
				var content = await FileIO.ReadTextAsync(file);

				if (isPlainText)
				{
					TextContent.Text = content;
					TextPanel.Visibility = Visibility.Visible;
				}
				else
				{
					HtmlContent.Text = content;
					HtmlPanel.Visibility = Visibility.Visible;
				}
			}
			catch (Exception ex)
			{
				ShowError($"Error reading file content: {ex.Message}");
			}
		}

		private void ShowError(string message)
		{
			TextContent.Text = message;
			TextContent.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
				Windows.UI.Color.FromArgb(255, 255, 0, 0));
			TextPanel.Visibility = Visibility.Visible;
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			// Clear all content
			DropIndicator.Visibility = Visibility.Visible;
			ClearButton.Visibility = Visibility.Collapsed;
			InfoPanel.Visibility = Visibility.Collapsed;
			ImagePanel.Visibility = Visibility.Collapsed;
			TextPanel.Visibility = Visibility.Collapsed;
			HtmlPanel.Visibility = Visibility.Collapsed;
			FilePanel.Visibility = Visibility.Collapsed;

			FormatsText.Text = string.Empty;
			OperationsText.Text = string.Empty;
			ImagePreview.Source = null;
			TextContent.Text = string.Empty;
			TextContent.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
				Windows.UI.Color.FromArgb(255, 0, 0, 0));
			HtmlContent.Text = string.Empty;
			FilesList.ItemsSource = null;
		}
	}
}
