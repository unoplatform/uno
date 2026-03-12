#nullable disable // Not supported by WinUI yet

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Uno.Extensions.Specialized;
using Uno.Foundation;
using System.Threading;

using NativeMethods = __Windows.ApplicationModel.DataTransfer.Clipboard.NativeMethods;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void Clear() => SetClipboardText(string.Empty);

		public static void SetContent(DataPackage/* ? */ content)
		{
			Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(
				() => _ = SetContentAsync(content),
				Uno.UI.Dispatching.NativeDispatcherPriority.High);
		}

		internal static async Task SetContentAsync(DataPackage/* ? */ content)
		{
			var data = content?.GetView(); // Freezes the DataPackage

			var hasBitmap = data?.Contains(StandardDataFormats.Bitmap) ?? false;
			var hasHtml = data?.Contains(StandardDataFormats.Html) ?? false;
			var hasText = data?.Contains(StandardDataFormats.Text) ?? false;

			if (hasBitmap)
			{
				var bitmapRef = await data.GetBitmapAsync();
				await SetClipboardBitmap(bitmapRef);
			}
			else if (hasHtml)
			{
				var html = await data.GetHtmlFormatAsync();
				// Get text for fallback - either from explicit text or extract from HTML
				var text = hasText
					? await data.GetTextAsync()
					: "";

				await SetClipboardHtml(html, text);
			}
			else if (hasText)
			{
				var text = await data.GetTextAsync();
				SetClipboardText(text);
			}
		}

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			dataPackage.SetDataProvider(StandardDataFormats.Text, async ct => await GetClipboardText(ct));
			dataPackage.SetDataProvider(StandardDataFormats.Html, async ct => await GetClipboardHtml(ct));
			dataPackage.SetDataProvider(StandardDataFormats.Bitmap, async ct => await GetClipboardBitmap(ct));

			return dataPackage.GetView();
		}

		private static async Task<string> GetClipboardText(CancellationToken ct)
		{
			return await NativeMethods.GetTextAsync();
		}

		private static async Task<string> GetClipboardHtml(CancellationToken ct)
		{
			return await NativeMethods.GetHtmlAsync();
		}

		private static async Task<RandomAccessStreamReference> GetClipboardBitmap(CancellationToken ct)
		{
			var base64 = await NativeMethods.GetImageAsync();
			if (string.IsNullOrEmpty(base64))
			{
				return null;
			}

			var bytes = Convert.FromBase64String(base64);
			var ras = new InMemoryRandomAccessStream();
			var stream = ras.AsStreamForWrite();
			{
				stream.Write(bytes, 0, bytes.Length);
				stream.Flush();

				stream.Position = 0;
			}

			return RandomAccessStreamReference.CreateFromStream(ras);
		}

		private static void SetClipboardText(string text)
		{
			NativeMethods.SetText(text);
		}

		private static async Task SetClipboardHtml(string html, string text)
		{
			await NativeMethods.SetHtmlAsync(html, text);
		}

		private static async Task SetClipboardBitmap(RandomAccessStreamReference reference)
		{
			using var ras = await reference.OpenReadAsync();
			using var stream = ras.AsStreamForRead();

			var buffer = new MemoryStream((int)ras.Size);
			stream.CopyTo(buffer);

			var base64 = Convert.ToBase64String(buffer.ToArray());
			var mimeType = string.IsNullOrEmpty(ras.ContentType) ? "image/png" : ras.ContentType;

			await NativeMethods.SetImageAsync(base64, mimeType);
		}

		private static void StartContentChanged()
		{
			NativeMethods.StartContentChanged();
		}

		private static void StopContentChanged()
		{
			NativeMethods.StopContentChanged();
		}

		[JSExport]
		internal static int DispatchContentChanged()
		{
			OnContentChanged();
			return 0;
		}
	}
}
