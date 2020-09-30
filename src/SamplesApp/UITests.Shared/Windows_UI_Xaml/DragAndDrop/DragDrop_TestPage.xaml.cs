using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Common;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample]
	public sealed partial class DragDrop_TestPage : UserControl
	{
		private int messageCounter;

		public DragDrop_TestPage()
		{
			this.InitializeComponent();

			this.messageCounter = 0;
		}

		/// <summary>
		/// Displays the contents of the given <see cref="DragEventArgs"/> for any drag/drop event.
		/// </summary>
		/// <param name="activeEvent">The name of the event to display.</param>
		/// <param name="args">The event arguments to display.</param>
		private void ShowDragDropDetails(string activeEvent, DragEventArgs args)
		{
			// Determine the standard formats (Uri is obsolete)
			var standardDataFormats = string.Empty;
			var sep = Environment.NewLine;
			if (args.DataView.Contains(StandardDataFormats.ApplicationLink)) { standardDataFormats += nameof(StandardDataFormats.ApplicationLink) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Bitmap)) { standardDataFormats += nameof(StandardDataFormats.Bitmap) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Html)) { standardDataFormats += nameof(StandardDataFormats.Html) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Rtf)) { standardDataFormats += nameof(StandardDataFormats.Rtf) + sep; }
			if (args.DataView.Contains(StandardDataFormats.StorageItems)) { standardDataFormats += nameof(StandardDataFormats.StorageItems) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Text)) { standardDataFormats += nameof(StandardDataFormats.Text) + sep; }
			if (args.DataView.Contains(StandardDataFormats.UserActivityJsonArray)) { standardDataFormats += nameof(StandardDataFormats.UserActivityJsonArray) + sep; }
			if (args.DataView.Contains(StandardDataFormats.WebLink)) { standardDataFormats += nameof(StandardDataFormats.WebLink) + sep; }

			if (standardDataFormats.EndsWith(sep))
			{
				standardDataFormats = standardDataFormats.Substring(0, standardDataFormats.Length - sep.Length);
			}

			// Show the details
			this.ActiveEventTextBlock.Text = activeEvent ?? string.Empty;
			this.AvailableFormatsTextBlock.Text = string.Join(Environment.NewLine, args.DataView.AvailableFormats) ?? string.Empty;
			this.AvailableStdFormatsTextBlock.Text = standardDataFormats;
			this.RequestedOperationTextBlock.Text = args.DataView.RequestedOperation.ToString();

			// Automatically hide after a certain duration
			var dispatcherTimer = new DispatcherTimer();
			dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5000);
			dispatcherTimer.Tick += (sender, e) =>
				{
					this.messageCounter--;
					dispatcherTimer.Stop();

					// Multiple dispatch timers can be running simultaneously.
					// Therefore, only clear the info after the last has elapsed.
					if (this.messageCounter <= 0)
					{
						this.ActiveEventTextBlock.Text = "<None>";
						this.AvailableFormatsTextBlock.Text = "<None>";
						this.AvailableStdFormatsTextBlock.Text = "<None>";
						this.RequestedOperationTextBlock.Text = "<None>";
					}
				};
			dispatcherTimer.Start();

			return;
		}

		/// <summary>
		/// Displays the contents of the given <see cref="DragEventArgs"/> after a drop occurs.
		/// </summary>
		/// <param name="args">The drop event arguments to display.</param>
		private async void ShowDropDetails(DragEventArgs args)
		{
			string title = "Last Drop Details";
			string details = string.Empty;

			this.DropDetailsTextBlock.Visibility = Visibility.Visible;
			this.DropDetailsImage.Visibility = Visibility.Collapsed;

			if (args.DataView.Contains(StandardDataFormats.ApplicationLink))
			{
				title += " (ApplicationLink)";

				// TODO
			}
			else if (args.DataView.Contains(StandardDataFormats.Bitmap))
			{
				title += " (Bitmap)";

				var stream = await args.DataView.GetBitmapAsync();

				var bitmapImage = new BitmapImage();
				bitmapImage.SetSource(await stream.OpenReadAsync());

				this.DropDetailsImage.Source = bitmapImage;
				this.DropDetailsImage.Visibility = Visibility.Visible;

				this.DropDetailsTextBlock.Visibility = Visibility.Collapsed;
			}
			else if (args.DataView.Contains(StandardDataFormats.Html))
			{
				title += " (Html)";

				var html = await args.DataView.GetHtmlFormatAsync();
				details = html;
			}
			else if (args.DataView.Contains(StandardDataFormats.Rtf))
			{
				title += " (Rtf)";

				var rtf = await args.DataView.GetRtfAsync();
				details = rtf;
			}
			else if (args.DataView.Contains(StandardDataFormats.StorageItems))
			{
				title += " (StorageItems)";

				var items = await args.DataView.GetStorageItemsAsync();
				if (items.Count > 0)
				{
					// Only use the first item
					foreach (IStorageItem item in items)
					{
						var file = item as StorageFile;

						if (file != null)
						{
							details += "Name: " + file.Name + Environment.NewLine;
							details += "DisplayName: " + file.DisplayName + Environment.NewLine;
							details += "DisplayType: " + file.DisplayType + Environment.NewLine;
							details += "ContentType: " + file.ContentType + Environment.NewLine;
							details += "Path: " + file.Path;
						}
					}
				}
			}
			else if (args.DataView.Contains(StandardDataFormats.Text))
			{
				title += " (Text)";

				var text = await args.DataView.GetTextAsync();
				details = text;
			}
			else if (args.DataView.Contains(StandardDataFormats.UserActivityJsonArray))
			{
				title += " (UserActivityJsonArray)";

				// TODO
			}
			else if (args.DataView.Contains(StandardDataFormats.WebLink))
			{
				title += " (WebLink)";

				var webLink = await args.DataView.GetWebLinkAsync();
				details = webLink.ToString();
			}

			this.DropTitleTextBlock.Text = title ?? string.Empty;
			this.DropDetailsTextBlock.Text = details ?? string.Empty;

			return;
		}

		/// <summary>
		/// Event handler for when a drag operation leaves the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_DragEnter(object sender, DragEventArgs e)
		{
			// Accept everything possible
			if (e.DataView.RequestedOperation != DataPackageOperation.None)
			{
				e.AcceptedOperation = e.DataView.RequestedOperation;
			}
			else
			{
				e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Link | DataPackageOperation.Move;
			}

			this.ShowDragDropDetails(nameof(UIElement.DragEnter), e);
			return;
		}

		/// <summary>
		/// Event handler for when a drag operation leaves the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_DragLeave(object sender, DragEventArgs e)
		{
			this.ShowDragDropDetails(nameof(UIElement.DragLeave), e);
			return;
		}

		/// <summary>
		/// Event handler for when a drag operation is over the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_DragOver(object sender, DragEventArgs e)
		{
			this.ShowDragDropDetails(nameof(UIElement.DragOver), e);
			return;
		}

		/// <summary>
		/// Event handler for when an internal drag starts from a known source.
		/// </summary>
		private void DragSource_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			args.AllowedOperations = DataPackageOperation.Copy | DataPackageOperation.Link | DataPackageOperation.Move;

			if (object.ReferenceEquals(sender, this.DragTextSourceTextBlock))
			{
				var sourceText = ((TextBlock)sender).Text;

				args.Data.RequestedOperation = DataPackageOperation.Copy;
				args.Data.SetText(sourceText);
			}
			else if (object.ReferenceEquals(sender, this.DragImageSourceImage))
			{
				var imageUri = RandomAccessStreamReference.CreateFromUri(new Uri(@"https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Rain_over_Beinn_Eich%2C_Luss_Hills%2C_Scotland.jpg/2560px-Rain_over_Beinn_Eich%2C_Luss_Hills%2C_Scotland.jpg"));

				args.Data.RequestedOperation = DataPackageOperation.Copy;
				args.Data.SetBitmap(imageUri);
			}
			else if (object.ReferenceEquals(sender, this.DragWebLinkSourceTextBlock))
			{
				var link = ((TextBlock)sender).Text;

				args.Data.RequestedOperation = DataPackageOperation.Copy;
				args.Data.SetWebLink(new Uri(link));
			}

			return;
		}

		/// <summary>
		/// Event handler for when an item is dropped on the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_Drop(object sender, DragEventArgs e)
		{
			this.ShowDragDropDetails(nameof(UIElement.Drop), e);
			this.ShowDropDetails(e);
			return;
		}

		private void DropBorder_DropCompleted(UIElement sender, DropCompletedEventArgs args)
		{
			// Currently not used
			return;
		}
	}
}
