using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample(
		Description =
			"This automated test validate that basic setup of in-app drag and drop works properly. "
			+ "You can select any item in the left (pink) column than drag and drop it over the right (blue) one to validate behavior.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_Basics : UserControl
	{
		public DragDrop_Basics()
		{
			this.InitializeComponent();

			SubscribeDragEvents(BasicDragSource);
			SubscribeDragEvents(TextDragSource);
			SubscribeDragEvents(LinkDragSource);
			SubscribeDragEvents(ImageDragSource);

			SubscribeDropEvents(DropTarget);
		}

		private void SubscribeDragEvents(FrameworkElement elt)
		{
			elt.DragStarting += (snd, e) =>
			{
				switch (snd)
				{
					case Image img when img.Source is BitmapImage bmp:
						e.Data.SetBitmap(RandomAccessStreamReference.CreateFromUri(bmp.UriSource));
						break;

					case TextBlock text:
						e.Data.SetText(text.Text);
						break;

					case HyperlinkButton link:
						e.Data.SetWebLink(link.NavigateUri);
						break;
				}

				Log(snd, "STARTING", e.Data.GetView());
			};

			elt.DropCompleted += (snd, e)
				=> Output.Text += $"[{GetName(snd)}] COMPLETED {e.DropResult}\r\n"; ;
		}

		private void SubscribeDropEvents(FrameworkElement elt)
		{
			elt.DragEnter += GetHandler("ENTER");
			elt.DragOver += GetHandler("OVER");
			elt.DragLeave += GetHandler("LEAVE");
			elt.Drop += GetHandler("DROP");

			DragEventHandler GetHandler(string evt) => (snd, args) =>
			{
				if (snd == args.OriginalSource)
				{
					Log(snd, evt, args.DataView);
					args.AcceptedOperation = DataPackageOperation.Copy;
				}
			};
		}

		private void Log(object snd, string evt, DataPackageView data)
			=> Output.Text += $"[{GetName(snd)}] {evt} {GetData(data)}\r\n";

		private static string GetName(object uiElt)
			=> uiElt is null ? "--null--" : (uiElt as FrameworkElement)?.Name ?? uiElt.GetType().Name;

		private static string GetData(DataPackageView data)
			=> $"<{string.Join("|", data.AvailableFormats)}>";
	}
}
