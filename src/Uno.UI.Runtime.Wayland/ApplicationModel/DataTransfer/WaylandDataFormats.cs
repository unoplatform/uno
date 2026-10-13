#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Uno.UI.Runtime.Wayland;

// Maps DataPackage formats to the MIME types exchanged through wl_data_source / wl_data_offer.
internal static class WaylandDataFormats
{
	// In order of preference when reading. UTF8_STRING, TEXT and STRING are the X11 targets that XWayland clients use.
	internal static readonly string[] TextMimeTypes = ["text/plain;charset=utf-8", "UTF8_STRING", "text/plain", "TEXT", "STRING"];
	internal const string HtmlMimeType = "text/html";
	internal static readonly string[] ImageMimeTypes = ["image/png", "image/jpeg"];

	// wl_data_device_manager.dnd_action
	internal const uint DndActionCopy = 1;
	internal const uint DndActionMove = 2;

	/// <summary>The MIME types under which <paramref name="view"/> can be sent, most specific first.</summary>
	internal static List<string> GetMimeTypes(DataPackageView view)
	{
		var formats = view.AvailableFormats;
		var mimeTypes = new List<string>();
		if (formats.Contains(StandardDataFormats.StorageItems))
		{
			mimeTypes.Add(WaylandFileUris.UriListMimeType);
			mimeTypes.Add(WaylandFileUris.GnomeCopiedFilesMimeType);
		}
		if (formats.Contains(StandardDataFormats.Bitmap))
		{
			mimeTypes.Add(ImageMimeTypes[0]);
		}
		if (formats.Contains(StandardDataFormats.Html))
		{
			mimeTypes.Add(HtmlMimeType);
		}
		if (formats.Contains(StandardDataFormats.Text) || formats.Contains(StandardDataFormats.Uri) || formats.Contains(StandardDataFormats.WebLink))
		{
			mimeTypes.AddRange(TextMimeTypes);
		}
		// Custom formats that already are MIME types are passed through as raw data.
		foreach (var format in formats)
		{
			if (format.Contains('/') && !mimeTypes.Contains(format))
			{
				mimeTypes.Add(format);
			}
		}
		return mimeTypes;
	}

	/// <summary>Serializes the content of <paramref name="view"/> for <paramref name="mimeType"/>; null if it has no such content.</summary>
	internal static async Task<byte[]?> GetBytesAsync(DataPackageView view, string mimeType)
	{
		var formats = view.AvailableFormats;
		if (formats.Contains(mimeType))
		{
			return await view.GetDataAsync(mimeType) switch
			{
				byte[] bytes => bytes,
				string text => await EncodeAsync(text, Encoding.UTF8),
				_ => null,
			};
		}
		if (mimeType is WaylandFileUris.UriListMimeType or WaylandFileUris.GnomeCopiedFilesMimeType && formats.Contains(StandardDataFormats.StorageItems))
		{
			var paths = (await view.GetStorageItemsAsync()).Select(item => item.Path).Where(path => !string.IsNullOrEmpty(path));
			var localPaths = paths.ToArray();
			return await Task.Run(() => Encoding.UTF8.GetBytes(mimeType == WaylandFileUris.UriListMimeType
				? WaylandFileUris.FormatUriList(localPaths)
				: WaylandFileUris.FormatGnomeCopiedFiles(localPaths)));
		}
		if (mimeType == ImageMimeTypes[0] && formats.Contains(StandardDataFormats.Bitmap))
		{
			var reference = await view.GetBitmapAsync();
			return await Task.Run(() => GetPngAsync(reference));
		}
		if (mimeType == HtmlMimeType && formats.Contains(StandardDataFormats.Html))
		{
			return await EncodeAsync(await view.GetHtmlFormatAsync(), Encoding.UTF8);
		}
		if (TextMimeTypes.Contains(mimeType))
		{
			var text = formats.Contains(StandardDataFormats.Text) ? await view.GetTextAsync()
				: formats.Contains(StandardDataFormats.Uri) ? (await view.GetUriAsync()).ToString()
				: formats.Contains(StandardDataFormats.WebLink) ? (await view.GetWebLinkAsync()).ToString()
				: null;
			return text is null ? null : await EncodeAsync(text, mimeType == "STRING" ? Encoding.Latin1 : Encoding.UTF8);
		}
		return null;
	}

	private static Task<byte[]> EncodeAsync(string text, Encoding encoding)
		=> Task.Run(() => encoding.GetBytes(text));

	private static async Task<byte[]?> GetPngAsync(RandomAccessStreamReference reference)
	{
		var stream = await reference.OpenReadAsync();
		using var ownedStream = reference.OwnsOpenedStream ? stream : null;
		var position = stream.Position;
		try
		{
			stream.Seek(0);
			using var buffer = new MemoryStream();
			await stream.AsStreamForRead().CopyToAsync(buffer);
			var bytes = buffer.ToArray();
			if (bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G']))
			{
				return bytes;
			}
			using var input = new MemoryStream(bytes);
			var codec = ImageEncoderDecoder.Current;
			if (!codec.TryDecode(input, null, null, out var frames))
			{
				return null;
			}
			using (frames)
			{
				var image = frames.Frames[0];
				var pixels = new byte[checked(image.PixelWidth * image.PixelHeight * 4)];
				image.CopyPixels(pixels);
				using var output = new MemoryStream();
				codec.Encode(output, pixels, image.PixelWidth, image.PixelHeight, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, BitmapEncoderFormat.Png, 100);
				return output.ToArray();
			}
		}
		finally
		{
			if (!reference.OwnsOpenedStream)
			{
				stream.Seek(position);
			}
		}
	}

	/// <summary>Adds the offered content to <paramref name="package"/>; data is received only when requested.</summary>
	internal static void Fill(DataPackage package, WaylandDataOffer offer)
	{
		var mimeTypes = offer.MimeTypes;
		foreach (var mimeType in mimeTypes)
		{
			package.SetDataProvider(mimeType, async cancellationToken => await ReadRequiredAsync(offer, mimeType, cancellationToken));
		}
		if (TextMimeTypes.FirstOrDefault(mimeTypes.Contains) is { } textMimeType)
		{
			var encoding = textMimeType == "STRING" ? Encoding.Latin1 : Encoding.UTF8;
			package.SetDataProvider(StandardDataFormats.Text, async cancellationToken => encoding.GetString(await ReadRequiredAsync(offer, textMimeType, cancellationToken)).TrimEnd('\0'));
		}
		if (mimeTypes.Contains(HtmlMimeType))
		{
			package.SetDataProvider(StandardDataFormats.Html, async cancellationToken => Encoding.UTF8.GetString(await ReadRequiredAsync(offer, HtmlMimeType, cancellationToken)));
		}
		if (mimeTypes.Contains(WaylandFileUris.UriListMimeType) || mimeTypes.Contains(WaylandFileUris.GnomeCopiedFilesMimeType))
		{
			package.SetDataProvider(StandardDataFormats.StorageItems, async cancellationToken => await ReadStorageItemsAsync(offer, mimeTypes, cancellationToken));
		}
		if (ImageMimeTypes.FirstOrDefault(mimeTypes.Contains) is { } imageMimeType)
		{
			package.SetDataProvider(StandardDataFormats.Bitmap, async cancellationToken =>
				RandomAccessStreamReference.CreateFromStream(new MemoryStream(await ReadRequiredAsync(offer, imageMimeType, cancellationToken)).AsRandomAccessStream()));
		}
	}

	// A failed transfer faults the DataPackageView getter (as WinUI does for unavailable data) instead of resolving empty content.
	private static async Task<byte[]> ReadRequiredAsync(WaylandDataOffer offer, string mimeType, CancellationToken cancellationToken)
		=> await offer.ReadAsync(mimeType, cancellationToken) ?? throw new IOException($"The Wayland data source did not deliver {mimeType}.");

	private static async Task<object> ReadStorageItemsAsync(WaylandDataOffer offer, IReadOnlyList<string> mimeTypes, CancellationToken cancellationToken)
	{
		var paths = mimeTypes.Contains(WaylandFileUris.UriListMimeType)
			? WaylandFileUris.ParseUriList(Encoding.UTF8.GetString(await ReadRequiredAsync(offer, WaylandFileUris.UriListMimeType, cancellationToken)))
			: WaylandFileUris.ParseGnomeCopiedFiles(Encoding.UTF8.GetString(await ReadRequiredAsync(offer, WaylandFileUris.GnomeCopiedFilesMimeType, cancellationToken)));
		var items = new List<IStorageItem>(paths.Count);
		foreach (var path in paths)
		{
			if (Directory.Exists(path))
			{
				items.Add(new StorageFolder(path));
			}
			else if (File.Exists(path))
			{
				items.Add(StorageFile.GetFileFromPath(path));
			}
		}
		return items;
	}

	internal static DataPackageOperation ToOperations(uint dndActions)
		=> ((dndActions & DndActionCopy) != 0 ? DataPackageOperation.Copy : DataPackageOperation.None)
			| ((dndActions & DndActionMove) != 0 ? DataPackageOperation.Move : DataPackageOperation.None);

	internal static uint ToDndActions(DataPackageOperation operations)
		=> ((operations & DataPackageOperation.Copy) != 0 ? DndActionCopy : 0)
			| ((operations & DataPackageOperation.Move) != 0 ? DndActionMove : 0);
}
