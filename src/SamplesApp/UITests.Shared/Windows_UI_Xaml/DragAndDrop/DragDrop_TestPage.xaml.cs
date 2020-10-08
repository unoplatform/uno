using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	public class EventDetails
	{
		public string AvailableFormats { get; set; }
		public string AvailableStdFormats { get; set; }
		public Brush EventBackground { get; set; }
		public string EventName { get; set; }
		public string RequestedOperation { get; set; }
		public string Timestamp { get; set; }

		public bool Equals(EventDetails eventDetails)
		{
			// Timestamp is ignored on purpose
			if (this.AvailableFormats != eventDetails.AvailableFormats ||
				this.AvailableStdFormats != eventDetails.AvailableStdFormats ||
				this.EventBackground.Equals(eventDetails.EventBackground) == false ||
				this.EventName != eventDetails.EventName ||
				this.RequestedOperation != eventDetails.RequestedOperation)
			{
				return false;
			}

			return true;
		}
	}

	[Sample]
	public sealed partial class DragDrop_TestPage : UserControl
	{
		/// <summary>
		/// Maximum number of drag and drop events to show in the list.
		/// </summary>
		private const int MaxDragDropEventsListCount = 100;

		// Define brushes to quickly recognized drag events in the events list
		private Brush DragStartingBrush = new SolidColorBrush(Colors.LimeGreen);
		private Brush DragEnterBrush = new SolidColorBrush(Colors.LightGreen);
		private Brush DragLeaveBrush = new SolidColorBrush(Colors.LightSalmon);
		private Brush DragOverBrush = new SolidColorBrush(Colors.LightBlue);
		private Brush DropBrush = new SolidColorBrush(Colors.LightPink);
		private Brush DropCompletedBrush = new SolidColorBrush(Colors.Tomato);

		public DragDrop_TestPage()
		{
			this.InitializeComponent();
			this.DataContext = this;
		}

		/// <summary>
		/// Gets or sets a value indicating whether duplicate drag drop events will be shown right after one another.
		/// </summary>
		public bool ShowDuplicateEvents { get; set; } = false;

		/// <summary>
		/// Gets the list of past drag and drop events.
		/// </summary>
		public ObservableCollection<EventDetails> DragDropEvents { get; } = new ObservableCollection<EventDetails>();

		private void AddToDragDropEvents(EventDetails eventDetails)
		{
			if (this.ShowDuplicateEvents ||
				this.DragDropEvents.Count < 1 ||
				eventDetails.Equals(this.DragDropEvents[0]) == false)
			{
				this.DragDropEvents.Insert(0, eventDetails);
				while (this.DragDropEvents.Count > MaxDragDropEventsListCount)
				{
					this.DragDropEvents.RemoveAt(this.DragDropEvents.Count - 1);
				}
			}

			return;
		}

		/// <summary>
		/// Displays the contents of the given <see cref="global::Windows.UI.Xaml.DragEventArgs"/> for any drag/drop event.
		/// </summary>
		/// <param name="eventName">The name of the event to display.</param>
		/// <param name="args">The event arguments to display.</param>
		private void ShowDragDropDetails(string eventName, global::Windows.UI.Xaml.DragEventArgs args)
		{
			// Determine the standard formats
			var standardDataFormats = string.Empty;
			var sep = Environment.NewLine;
			if (args.DataView.Contains(StandardDataFormats.ApplicationLink)) { standardDataFormats += nameof(StandardDataFormats.ApplicationLink) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Bitmap)) { standardDataFormats += nameof(StandardDataFormats.Bitmap) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Html)) { standardDataFormats += nameof(StandardDataFormats.Html) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Rtf)) { standardDataFormats += nameof(StandardDataFormats.Rtf) + sep; }
			if (args.DataView.Contains(StandardDataFormats.StorageItems)) { standardDataFormats += nameof(StandardDataFormats.StorageItems) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Text)) { standardDataFormats += nameof(StandardDataFormats.Text) + sep; }
			if (args.DataView.Contains(StandardDataFormats.UserActivityJsonArray)) { standardDataFormats += nameof(StandardDataFormats.UserActivityJsonArray) + sep; }
			if (args.DataView.Contains(StandardDataFormats.Uri)) { standardDataFormats += nameof(StandardDataFormats.Uri) + sep; }
			if (args.DataView.Contains(StandardDataFormats.WebLink)) { standardDataFormats += nameof(StandardDataFormats.WebLink) + sep; }

			if (standardDataFormats.EndsWith(sep))
			{
				standardDataFormats = standardDataFormats.Substring(0, standardDataFormats.Length - sep.Length);
			}

			var eventDetails = new EventDetails()
			{
				EventName = eventName ?? string.Empty,
				AvailableFormats = string.Join(Environment.NewLine, args.DataView.AvailableFormats) ?? string.Empty,
				AvailableStdFormats = standardDataFormats,
				RequestedOperation = args.DataView.RequestedOperation.ToString(),
				Timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff")
			};

			switch (eventName)
			{
				case nameof(UIElement.DragStarting):
					{
						eventDetails.EventBackground = this.DragStartingBrush;
						break;
					}
				case nameof(UIElement.DragEnter):
					{
						eventDetails.EventBackground = this.DragEnterBrush;
						break;
					}
				case nameof(UIElement.DragLeave):
					{
						eventDetails.EventBackground = this.DragLeaveBrush;
						break;
					}
				case nameof(UIElement.DragOver):
					{
						eventDetails.EventBackground = this.DragOverBrush;
						break;
					}
				case nameof(UIElement.Drop):
					{
						eventDetails.EventBackground = this.DropBrush;
						break;
					}
				case nameof(UIElement.DropCompleted):
					{
						eventDetails.EventBackground = this.DropCompletedBrush;
						break;
					}
			}

			this.AddToDragDropEvents(eventDetails);

			return;
		}

		/// <summary>
		/// Displays the contents of the given <see cref="global::Windows.UI.Xaml.DragEventArgs"/> after a drop occurs.
		/// </summary>
		/// <param name="args">The drop event arguments to display.</param>
		private async void ShowDropDetails(global::Windows.UI.Xaml.DragEventArgs args)
		{
			string title = "Last Drop Details";
			string position = string.Empty;
			string details = string.Empty;

			this.DropDetailsTextBlock.Visibility = Visibility.Visible;
			this.DropDetailsImage.Visibility = Visibility.Collapsed;

			// Only one data format can be displayed at once.
			// Therefore, the order/priority here is used to determine which should be displayed.
			// It should be ordered most specific to least generally speaking.
			if (args.DataView.Contains(StandardDataFormats.ApplicationLink))
			{
				title += " (ApplicationLink)";

				var appLink = await args.DataView.GetApplicationLinkAsync();
				details = appLink.ToString();
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
							//details += "DisplayType: " + file.DisplayType + Environment.NewLine;
							details += "ContentType: " + file.ContentType + Environment.NewLine;
							details += "Path: " + file.Path;
						}
					}
				}
			}
			else if (args.DataView.Contains(StandardDataFormats.UserActivityJsonArray))
			{
				title += " (UserActivityJsonArray)";

				// TODO
			}
			else if (args.DataView.Contains(StandardDataFormats.Uri))
			{
				title += " (Uri)";

				var uri = await args.DataView.GetUriAsync();
				details = uri.ToString();
			}
			else if (args.DataView.Contains(StandardDataFormats.WebLink))
			{
				title += " (WebLink)";

				var webLink = await args.DataView.GetWebLinkAsync();
				details = webLink.ToString();
			}
			else if (args.DataView.Contains(StandardDataFormats.Text))
			{
				title += " (Text)";

				var text = await args.DataView.GetTextAsync();
				details = text;
			}

			// Determine the drop position
			var pos = args.GetPosition(this.DropBorder);
			position = "(" + pos.X.ToString("0.00") + ", " + pos.Y.ToString("0.00") + ")";

			this.DropTitleTextBlock.Text = title ?? string.Empty;
			this.DropPositionTextBlock.Text = position ?? string.Empty;
			this.DropDetailsTextBlock.Text = details ?? string.Empty;

			return;
		}

		/// <summary>
		/// Event handler for when a drag operation enters the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_DragEnter(object sender, global::Windows.UI.Xaml.DragEventArgs e)
		{
			// We accept only one operation
			e.AcceptedOperation = DataPackageOperation.Copy;

			this.ShowDragDropDetails(nameof(global::Windows.UI.Xaml.UIElement.DragEnter), e);
			return;
		}

		/// <summary>
		/// Event handler for when a drag operation leaves the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_DragLeave(object sender, global::Windows.UI.Xaml.DragEventArgs e)
		{
			this.ShowDragDropDetails(nameof(global::Windows.UI.Xaml.UIElement.DragLeave), e);
			return;
		}

		/// <summary>
		/// Event handler for when a drag operation is over the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_DragOver(object sender, global::Windows.UI.Xaml.DragEventArgs e)
		{
			this.ShowDragDropDetails(nameof(global::Windows.UI.Xaml.UIElement.DragOver), e);
			return;
		}

		/// <summary>
		/// Event handler for when an item is dropped on the <see cref="DropBorder"/>.
		/// </summary>
		private void DropBorder_Drop(object sender, global::Windows.UI.Xaml.DragEventArgs e)
		{
			this.ShowDragDropDetails(nameof(global::Windows.UI.Xaml.UIElement.Drop), e);
			this.ShowDropDetails(e);

			return;
		}

		/// <summary>
		/// Event handler for when an internal drag starts from a known source.
		/// </summary>
		private void DragSource_DragStarting(global::Windows.UI.Xaml.UIElement sender, DragStartingEventArgs args)
		{
			string availableStdFormats = string.Empty;
			string requestedOperation = DataPackageOperation.Copy.ToString();

			args.Data.RequestedOperation = DataPackageOperation.Copy;
			args.AllowedOperations = DataPackageOperation.Copy | DataPackageOperation.Link | DataPackageOperation.Move;

			if (object.ReferenceEquals(sender, this.DragTextSourceTextBlock))
			{
				var sourceText = ((TextBlock)sender).Text;
				args.Data.SetText(sourceText);

				availableStdFormats = nameof(StandardDataFormats.Text);
			}
			else if (object.ReferenceEquals(sender, this.DragImageSourceImage))
			{
				var imageUri = RandomAccessStreamReference.CreateFromUri(new Uri(@"https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Rain_over_Beinn_Eich%2C_Luss_Hills%2C_Scotland.jpg/2560px-Rain_over_Beinn_Eich%2C_Luss_Hills%2C_Scotland.jpg"));
				args.Data.SetBitmap(imageUri);

				availableStdFormats = nameof(StandardDataFormats.Bitmap);
			}
			else if (object.ReferenceEquals(sender, this.DragWebLinkSourceTextBlock))
			{
				var link = ((TextBlock)sender).Text;
				args.Data.SetWebLink(new Uri(link));

				availableStdFormats = nameof(StandardDataFormats.WebLink);
			}

			var eventDetails = new EventDetails()
			{
				EventBackground = this.DragStartingBrush,
				EventName = nameof(global::Windows.UI.Xaml.UIElement.DragStarting),
				AvailableFormats = string.Empty,
				AvailableStdFormats = availableStdFormats,
				RequestedOperation = requestedOperation,
				Timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff")
			};

			this.AddToDragDropEvents(eventDetails);

			return;
		}

		/// <summary>
		/// Event handler for when a drop operation is completed.
		/// </summary>
		private void DragSource_DropCompleted(global::Windows.UI.Xaml.UIElement sender, DropCompletedEventArgs args)
		{
			var eventDetails = new EventDetails()
			{
				EventBackground = this.DropCompletedBrush,
				EventName = nameof(UIElement.DropCompleted),
				AvailableFormats = string.Empty,
				AvailableStdFormats = string.Empty,
				RequestedOperation = string.Empty,
				Timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff")
			};

			this.AddToDragDropEvents(eventDetails);

			return;
		}

		/// <summary>
		/// Event handler for when the clear drag and drop events button is pressed.
		/// </summary>
		private void ClearDragDropEventsButton_Click(object sender, RoutedEventArgs e)
		{
			this.DragDropEvents.Clear();
			return;
		}
	}
}
