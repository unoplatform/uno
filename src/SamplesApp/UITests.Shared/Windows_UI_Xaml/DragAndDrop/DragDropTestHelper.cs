using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	internal class DragDropTestHelper
	{
		private readonly TextBlock _output;

		public DragDropTestHelper(TextBlock output)
		{
			_output = output;
		}

		public void SubscribeDragEvents(FrameworkElement elt)
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
				=> _output.Text += $"[{GetName(snd)}] COMPLETED {e.DropResult}\r\n";
		}

		public void SubscribeDropEvents(FrameworkElement elt)
		{
			elt.DragEnter += GetHandler("ENTER");
			elt.DragOver += GetHandler("OVER");
			elt.DragLeave += GetHandler("LEAVE");
			elt.Drop += GetHandler("DROP");

			DragEventHandler GetHandler(string evt) => (snd, args) =>
			{
				Log(snd, evt, args.DataView);
				args.AcceptedOperation = DataPackageOperation.Copy;
			};
		}

		private void Log(object snd, string evt, DataPackageView data)
			=> _output.Text += $"[{GetName(snd)}] {evt} {GetData(data)}\r\n";

		private static string GetName(object uiElt)
			=> uiElt is null ? "--null--" : (uiElt as FrameworkElement)?.Name ?? uiElt.GetType().Name;

		private static string GetData(DataPackageView data)
			=> $"<{string.Join('|', data.AvailableFormats)}>";
	}
}
