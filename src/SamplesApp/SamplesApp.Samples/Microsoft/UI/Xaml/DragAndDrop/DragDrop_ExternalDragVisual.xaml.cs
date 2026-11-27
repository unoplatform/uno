using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample(
		Description =
		"""
		Drag an image, a file or text from another app over each area, then drop it.
		- Default: the source's own drag image follows the pointer, with the "Copy" glyph and caption.
		- Custom caption: the source's image, with the caption "Custom caption" and no glyph.
		- Custom content: this app's image replaces the source's.
		- Content hidden: no image, only the glyph and caption.
		Leaving the window removes the glyph and caption, and the source's image follows the pointer over other windows.
		A drop lists the formats and the data.
		""",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_ExternalDragVisual : UserControl
	{
		private readonly BitmapImage _customContent = new(new Uri("ms-appx:///Assets/test_image_100_100.png"));

		public DragDrop_ExternalDragVisual()
		{
			this.InitializeComponent();

			Subscribe(DefaultTarget, _ => { });
			Subscribe(CaptionTarget, e =>
			{
				e.DragUIOverride.Caption = "Custom caption";
				e.DragUIOverride.IsGlyphVisible = false;
			});
			Subscribe(ContentTarget, e => e.DragUIOverride.SetContentFromBitmapImage(_customContent));
			Subscribe(HiddenContentTarget, e => e.DragUIOverride.IsContentVisible = false);
		}

		private void Subscribe(UIElement target, Action<DragEventArgs> overrideDragUI)
		{
			target.DragOver += (_, e) =>
			{
				e.AcceptedOperation = DataPackageOperation.Copy;
				overrideDragUI(e);
			};
			target.Drop += OnDrop;
		}

		private async void OnDrop(object sender, DragEventArgs e)
		{
			var deferral = e.GetDeferral();
			try
			{
				var view = e.DataView;
				var details = $"Dropped on {((FrameworkElement)sender).Name}. Formats: {string.Join(", ", view.AvailableFormats)}.";
				if (view.Contains(StandardDataFormats.StorageItems))
				{
					var items = await view.GetStorageItemsAsync();
					details += $" Files: {string.Join(", ", items.Select(item => item.Name))}.";
				}
				if (view.Contains(StandardDataFormats.Bitmap))
				{
					using var stream = await (await view.GetBitmapAsync()).OpenReadAsync();
					details += $" Bitmap: {stream.Size} bytes.";
				}
				if (view.Contains(StandardDataFormats.Text))
				{
					var text = await view.GetTextAsync();
					details += $" Text: {Truncate(text, 100)}";
				}
				Output.Text = details;
			}
			catch (Exception error)
			{
				Output.Text = $"Reading the drop failed: {error.Message}";
			}
			finally
			{
				deferral.Complete();
			}
		}

		private static string Truncate(string text, int length)
		{
			if (text.Length <= length)
			{
				return text;
			}

			// Don't split a surrogate pair.
			return text[..(char.IsHighSurrogate(text[length - 1]) ? length - 1 : length)] + "...";
		}
	}
}
