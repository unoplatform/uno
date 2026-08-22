#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.Helpers.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;

using NativeMethods = __Windows.ApplicationModel.DataTransfer.Clipboard.NativeMethods;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string PlainTextMimeType = "text/plain";
		private const string HtmlMimeType = "text/html";
		private const string RtfMimeType = "text/rtf";
		private const string UriListMimeType = "text/uri-list";

		private static readonly char[] _newLineChars = new[] { '\r', '\n' };

		public static void Clear() =>
			RunOnMainThread(async () =>
			{
				try
				{
					await NativeMethods.ClearAsync();
				}
				catch (Exception e)
				{
					if (typeof(Clipboard).Log().IsEnabled(LogLevel.Error))
					{
						typeof(Clipboard).Log().Error("Failed to clear the clipboard", e);
					}
				}
			});

		public static void SetContent(DataPackage content)
		{
			ArgumentNullException.ThrowIfNull(content);

			var data = content.GetView(); // Freezes the DataPackage

			RunOnMainThread(async () =>
			{
				try
				{
					await SetContentAsync(data);
				}
				catch (Exception e)
				{
					if (typeof(Clipboard).Log().IsEnabled(LogLevel.Error))
					{
						typeof(Clipboard).Log().Error("Failed to write to the clipboard", e);
					}
				}
			});
		}

		// Starting the operation synchronously when possible keeps the write inside the
		// transient user activation the browser clipboard API requires.
		private static void RunOnMainThread(Func<Task> asyncAction)
		{
			if (Uno.UI.Dispatching.NativeDispatcher.Main.HasThreadAccess)
			{
				_ = asyncAction();
			}
			else
			{
				Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(
					() => _ = asyncAction(),
					Uno.UI.Dispatching.NativeDispatcherPriority.High);
			}
		}

		internal static async Task SetContentAsync(DataPackageView data)
		{
			var entries = new List<ClipboardWriteEntry>();

			var uriText = await GetUriFallbackText(data);

			var text = data.Contains(StandardDataFormats.Text)
				? await data.GetTextAsync()
				: uriText;

			if (text is not null)
			{
				entries.Add(new ClipboardWriteEntry { Type = PlainTextMimeType, Value = text });
			}

			if (uriText is not null)
			{
				// Round-trips GetWebLinkAsync/GetUriAsync through GetContent (browsers have no
				// dedicated link format); on Chromium this also transfers as a web custom format.
				entries.Add(new ClipboardWriteEntry { Type = UriListMimeType, Value = uriText, Custom = true });
			}

			if (data.Contains(StandardDataFormats.Html))
			{
				entries.Add(new ClipboardWriteEntry { Type = HtmlMimeType, Value = await data.GetHtmlFormatAsync() });
			}

			if (data.Contains(StandardDataFormats.Rtf))
			{
				entries.Add(new ClipboardWriteEntry { Type = RtfMimeType, Value = await data.GetRtfAsync(), Custom = true });
			}

			if (data.Contains(StandardDataFormats.StorageItems) && typeof(Clipboard).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Clipboard).Log().Warn("Storage items cannot be written to the browser clipboard and were skipped.");
			}

			foreach (var formatId in data.AvailableFormats)
			{
				if (IsStandardFormat(formatId))
				{
					continue;
				}

				try
				{
					if (await data.GetDataAsync(formatId) is string value)
					{
						entries.Add(new ClipboardWriteEntry { Type = formatId, Value = value, Custom = true });
					}
					else if (typeof(Clipboard).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(Clipboard).Log().Warn($"Only string data can be written to the clipboard for custom format '{formatId}'.");
					}
				}
				catch (Exception e)
				{
					if (typeof(Clipboard).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(Clipboard).Log().Warn($"Failed to retrieve the data for custom format '{formatId}'.", e);
					}
				}
			}

			var imageBytes = Array.Empty<byte>();
			var imageMimeType = string.Empty;
			if (data.Contains(StandardDataFormats.Bitmap))
			{
				(imageBytes, imageMimeType) = await ReadBitmapAsync(data);
			}

			var entriesJson = JsonHelper.Serialize(entries.ToArray(), ClipboardSerializationContext.Default);
			await NativeMethods.SetContentAsync(entriesJson, imageBytes, imageMimeType);
		}

		// WinUI exposes URIs as dedicated formats; browsers can only carry them as text.
		private static async Task<string?> GetUriFallbackText(DataPackageView data)
		{
			var uri = DataPackage.CombineUri(
				data.Contains(StandardDataFormats.WebLink) ? (await data.GetWebLinkAsync())?.ToString() : null,
				data.Contains(StandardDataFormats.ApplicationLink) ? (await data.GetApplicationLinkAsync())?.ToString() : null,
				null);

			return string.IsNullOrEmpty(uri) ? null : uri;
		}

		private static bool IsStandardFormat(string formatId) =>
			formatId == StandardDataFormats.Text ||
			formatId == StandardDataFormats.Html ||
			formatId == StandardDataFormats.Rtf ||
			formatId == StandardDataFormats.Bitmap ||
			formatId == StandardDataFormats.StorageItems ||
			formatId == StandardDataFormats.Uri || // Same id as WebLink
			formatId == StandardDataFormats.ApplicationLink ||
			formatId == StandardDataFormats.UserActivityJsonArray;

		private static async Task<(byte[] Bytes, string MimeType)> ReadBitmapAsync(DataPackageView data)
		{
			var reference = await data.GetBitmapAsync();
			using var ras = await reference.OpenReadAsync();

			if (ras.Size > int.MaxValue)
			{
				throw new NotSupportedException("Clipboard image is too large.");
			}

			using var stream = ras.AsStreamForRead();
			var bytes = new byte[(int)ras.Size];
			await stream.ReadExactlyAsync(bytes);

			return (bytes, GetImageMimeType(ras, bytes));
		}

		public static DataPackageView GetContent()
		{
			var formats = JsonHelper.Deserialize<ClipboardSnapshotFormats>(
				NativeMethods.GetSnapshotFormats(), ClipboardSerializationContext.Default);

			var package = new DataPackage();

			// All providers of this view share a single clipboard read, resolved against the
			// same source the advertised formats were derived from.
			var fromPaste = formats.PasteFormats is not null || formats.PasteImminent;
			var content = new Lazy<Task<ClipboardContentData>>(
				() => GetClipboardContentAsync(fromPaste),
				LazyThreadSafetyMode.ExecutionAndPublication);

			if (formats.PasteFormats is { } pasteFormats)
			{
				// A recent paste gesture was captured; its formats are known exactly.
				foreach (var mimeType in pasteFormats)
				{
					AddTextProvider(package, content, mimeType);
				}

				if (formats.PasteHasFiles)
				{
					AddStorageItemsProvider(package, content);
				}

				if (formats.PasteHasImage)
				{
					AddBitmapProvider(package, content);
				}
			}
			else if (formats.PasteImminent)
			{
				// A paste shortcut was just pressed; advertise everything and let the
				// providers resolve from the incoming paste event.
				AddTextProvider(package, content, PlainTextMimeType);
				AddTextProvider(package, content, HtmlMimeType);
				AddBitmapProvider(package, content);
				AddStorageItemsProvider(package, content);
			}
			else if (formats.OwnFormats is { } ownFormats)
			{
				// The clipboard still holds the last content written by this application.
				foreach (var mimeType in ownFormats)
				{
					if (mimeType.StartsWith("image/", StringComparison.Ordinal))
					{
						AddBitmapProvider(package, content);
					}
					else
					{
						AddTextProvider(package, content, mimeType);
					}
				}
			}
			else
			{
				// Unknown clipboard state: advertise the formats the async clipboard API may provide.
				AddTextProvider(package, content, PlainTextMimeType);
				AddTextProvider(package, content, HtmlMimeType);
				AddBitmapProvider(package, content);
			}

			return package.GetView();
		}

		private static void AddTextProvider(DataPackage package, Lazy<Task<ClipboardContentData>> content, string mimeType)
		{
			if (mimeType == UriListMimeType)
			{
				// https://datatracker.ietf.org/doc/html/rfc2483#section-5
				package.SetDataProvider(StandardDataFormats.WebLink, async ct =>
				{
					var uri = (await GetTextValue(content, mimeType) ?? "")
						.Split(_newLineChars, StringSplitOptions.RemoveEmptyEntries)
						.FirstOrDefault(line => !line.StartsWith('#'));

					return uri is null
						? throw new InvalidOperationException("The clipboard uri-list does not contain a URI.")
						: new Uri(uri);
				});
				return;
			}

			var formatId = mimeType switch
			{
				PlainTextMimeType => StandardDataFormats.Text,
				HtmlMimeType => StandardDataFormats.Html,
				RtfMimeType => StandardDataFormats.Rtf,
				_ => mimeType, // Custom format ids pass through unchanged
			};

			// Missing resolves to empty: browsers cannot distinguish an empty clipboard from an
			// empty string, so the absent/empty distinction does not exist on this platform.
			package.SetDataProvider(formatId, async ct => await GetTextValue(content, mimeType) ?? "");
		}

		private static async Task<string?> GetTextValue(Lazy<Task<ClipboardContentData>> content, string mimeType)
		{
			var data = await content.Value;
			return data.Texts.FirstOrDefault(entry => entry.Type == mimeType)?.Value;
		}

		private static void AddBitmapProvider(DataPackage package, Lazy<Task<ClipboardContentData>> content) =>
			package.SetDataProvider(StandardDataFormats.Bitmap, async ct =>
			{
				var data = await content.Value;
				if (data.Image is null)
				{
					throw new InvalidOperationException("The clipboard does not contain an image.");
				}

				// The image is registered as a native file handle on the JS side and streamed on demand.
				return RandomAccessStreamReference.CreateFromFile(StorageFile.GetFromNativeInfo(data.Image));
			});

		private static void AddStorageItemsProvider(DataPackage package, Lazy<Task<ClipboardContentData>> content) =>
			package.SetDataProvider(StandardDataFormats.StorageItems, async ct =>
			{
				// A paste gesture that carried no files resolves to an empty list rather than
				// failing, so optimistic paste handlers degrade to a graceful no-op.
				var data = await content.Value;
				return (IReadOnlyList<IStorageItem>)data.Files.Select(StorageFile.GetFromNativeInfo).ToList();
			});

		private static async Task<ClipboardContentData> GetClipboardContentAsync(bool fromPaste)
		{
			var data = JsonHelper.Deserialize<ClipboardContentData>(
				await NativeMethods.GetContentAsync(fromPaste), ClipboardSerializationContext.Default);

			return data.Status switch
			{
				"denied" => throw new UnauthorizedAccessException(
					"Access to the clipboard was denied by the browser. Reading the clipboard requires user permission or a paste gesture."),
				"unavailable" => throw new NotSupportedException(
					"The browser clipboard API is not available in this context. A secure context (HTTPS) is required."),
				_ => data,
			};
		}

		private static string GetImageMimeType(IRandomAccessStreamWithContentType ras, byte[] data)
		{
			if (!string.IsNullOrEmpty(ras.ContentType))
			{
				return ras.ContentType;
			}

			if (data == null || data.Length == 0)
			{
				// Even if data is empty, return a generic image MIME type so JS clipboard logic
				// (which filters on "image/") can handle the entry consistently.
				return "image/png";
			}

			// PNG signature: 89 50 4E 47 0D 0A 1A 0A
			if (data.Length >= 8 &&
				data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
				data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
			{
				return "image/png";
			}

			// JPEG signature: FF D8 FF
			if (data.Length >= 3 &&
				data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
			{
				return "image/jpeg";
			}

			// BMP signature: 42 4D
			if (data.Length >= 2 &&
				data[0] == 0x42 && data[1] == 0x4D)
			{
				return "image/bmp";
			}

			// GIF signature: 47 49 46 38 ("GIF8")
			if (data.Length >= 4 &&
				data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
			{
				return "image/gif";
			}

			// WebP signature: "RIFF"...."WEBP"
			if (data.Length >= 12 &&
				data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && // "RIFF"
				data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)   // "WEBP"
			{
				return "image/webp";
			}

			// Fallback when the format is unknown: use a generic image MIME type so that
			// the JS clipboard side (which only accepts "image/*") can still consume it.
			return "image/png";
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
