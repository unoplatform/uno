#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
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
		private const string PngMimeType = "image/png";

		private static readonly char[] _newLineChars = new[] { '\r', '\n' };

		// SetContent and Clear prepare their data asynchronously, so a later call can be ready to
		// write before an earlier one; the earlier one is dropped rather than overwriting it.
		private static int _writeGeneration;

		public static void Clear()
		{
			var generation = Interlocked.Increment(ref _writeGeneration);

			RunOnMainThread(async () =>
			{
				try
				{
					await NativeMethods.ClearAsync(generation);
				}
				catch (Exception e)
				{
					if (typeof(Clipboard).Log().IsEnabled(LogLevel.Error))
					{
						typeof(Clipboard).Log().Error("Failed to clear the clipboard", e);
					}
				}
			});
		}

		public static void SetContent(DataPackage content)
		{
			ArgumentNullException.ThrowIfNull(content);

			var data = content.GetView(); // Freezes the DataPackage
			var generation = Interlocked.Increment(ref _writeGeneration);

			RunOnMainThread(async () =>
			{
				try
				{
					await SetContentAsync(data, generation);
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

		private static async Task SetContentAsync(DataPackageView data, int generation)
		{
			// The browser only accepts the write inside the user activation SetContent was called
			// in, so it is issued now with the formats, which are known up front; the data follows
			// once it has been read.
			var hasUri = data.Contains(StandardDataFormats.WebLink) || data.Contains(StandardDataFormats.ApplicationLink);
			var formats = new List<ClipboardWriteFormat>();

			if (data.Contains(StandardDataFormats.Text) || hasUri)
			{
				formats.Add(new ClipboardWriteFormat { Type = PlainTextMimeType });
			}

			if (hasUri)
			{
				formats.Add(new ClipboardWriteFormat { Type = UriListMimeType, Custom = true });
			}

			if (data.Contains(StandardDataFormats.Html))
			{
				formats.Add(new ClipboardWriteFormat { Type = HtmlMimeType });
			}

			if (data.Contains(StandardDataFormats.Rtf))
			{
				formats.Add(new ClipboardWriteFormat { Type = RtfMimeType, Custom = true });
			}

			if (data.Contains(StandardDataFormats.Bitmap))
			{
				formats.Add(new ClipboardWriteFormat { Type = PngMimeType });
			}

			foreach (var formatId in data.AvailableFormats)
			{
				// Only string data can be written; what a provider yields is known once it has run.
				if (IsCustomFormat(formatId) && data.FindRawData(formatId) is string or DataProviderHandler)
				{
					formats.Add(new ClipboardWriteFormat { Type = formatId, Custom = true });
				}
			}

			NativeMethods.BeginWrite(generation, JsonHelper.Serialize(formats.ToArray(), ClipboardSerializationContext.Default));

			try
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
					entries.Add(new ClipboardWriteEntry { Type = UriListMimeType, Value = uriText });
				}

				if (data.Contains(StandardDataFormats.Html))
				{
					entries.Add(new ClipboardWriteEntry { Type = HtmlMimeType, Value = await data.GetHtmlFormatAsync() });
				}

				if (data.Contains(StandardDataFormats.Rtf))
				{
					entries.Add(new ClipboardWriteEntry { Type = RtfMimeType, Value = await data.GetRtfAsync() });
				}

				if (data.Contains(StandardDataFormats.StorageItems) && typeof(Clipboard).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(Clipboard).Log().Warn("Storage items cannot be written to the browser clipboard and were skipped.");
				}

				foreach (var formatId in data.AvailableFormats)
				{
					if (!IsCustomFormat(formatId))
					{
						continue;
					}

					try
					{
						if (await data.GetDataAsync(formatId) is string value)
						{
							entries.Add(new ClipboardWriteEntry { Type = formatId, Value = value });
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

				if (generation != Volatile.Read(ref _writeGeneration))
				{
					// A later SetContent or Clear has replaced this one, and dropped the write it issued.
					return;
				}

				var entriesJson = JsonHelper.Serialize(entries.ToArray(), ClipboardSerializationContext.Default);
				await NativeMethods.ResolveWriteAsync(generation, entriesJson, imageBytes, imageMimeType);
			}
			catch
			{
				NativeMethods.AbortWrite(generation);
				throw;
			}
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

		// A custom format is written under its id as MIME type, so one named like a standard
		// representation would collide with it and is left out.
		private static bool IsCustomFormat(string formatId)
		{
			if (IsStandardFormat(formatId))
			{
				return false;
			}

			if (IsReservedMimeType(formatId))
			{
				if (typeof(Clipboard).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(Clipboard).Log().Warn($"Custom format '{formatId}' is the MIME type of a standard format and was skipped; use the standard format instead.");
				}
				return false;
			}

			return true;
		}

		private static bool IsReservedMimeType(string formatId) =>
			formatId.Equals(PlainTextMimeType, StringComparison.OrdinalIgnoreCase) ||
			formatId.Equals(HtmlMimeType, StringComparison.OrdinalIgnoreCase) ||
			formatId.Equals(RtfMimeType, StringComparison.OrdinalIgnoreCase) ||
			formatId.Equals(UriListMimeType, StringComparison.OrdinalIgnoreCase) ||
			formatId.Equals(PngMimeType, StringComparison.OrdinalIgnoreCase);

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
			var snapshot = JsonHelper.Deserialize<ClipboardContentData>(
				NativeMethods.GetSnapshot(), ClipboardSerializationContext.Default);

			var package = new DataPackage();

			switch (snapshot.Status)
			{
				case ClipboardContentStatus.Paste:
				case ClipboardContentStatus.Own:
					// A recent paste gesture was captured, or the clipboard still holds the last
					// content written by this application: the content is known and the view
					// holds it, whatever happens to the clipboard afterwards.
					SetKnownContent(package, snapshot);
					break;

				case ClipboardContentStatus.Imminent:
					// A paste shortcut was just pressed; advertise everything and let the
					// providers resolve from the incoming paste event.
					AddPendingContent(package, snapshot.PasteShortcutTime, includeStorageItems: true);
					break;

				case ClipboardContentStatus.Unknown:
					// Advertise the formats the async clipboard API may provide.
					AddPendingContent(package, pasteShortcutTime: -1, includeStorageItems: false);
					break;

				default:
					// No clipboard API in this context and nothing captured: nothing can be read.
					break;
			}

			return package.GetView();
		}

		private static void SetKnownContent(DataPackage package, ClipboardContentData data)
		{
			var lease = data.Handles.Length > 0 ? new ClipboardHandleLease(data.Handles) : null;

			foreach (var entry in data.Texts)
			{
				if (entry.Type == UriListMimeType)
				{
					SetUriListContent(package, entry.Value);
				}
				else
				{
					package.SetData(ToFormatId(entry.Type), entry.Value);
				}
			}

			if (data.Files.Length > 0)
			{
				package.SetStorageItems(data.Files.Select(info => CreateStorageFile(info, lease)));
			}

			if (data.Image is { } image)
			{
				package.SetBitmap(RandomAccessStreamReference.CreateFromFile(CreateStorageFile(image, lease)));
			}
		}

		// The content is not known yet; all providers of this view share a single clipboard read.
		private static void AddPendingContent(DataPackage package, double pasteShortcutTime, bool includeStorageItems)
		{
			var lease = new ClipboardHandleLease();
			var content = new Lazy<Task<ClipboardContentData>>(
				() => GetClipboardContentAsync(pasteShortcutTime, lease),
				LazyThreadSafetyMode.ExecutionAndPublication);

			// Missing resolves to empty: browsers cannot distinguish an empty clipboard from an
			// empty string, so the absent/empty distinction does not exist on this platform.
			package.SetDataProvider(StandardDataFormats.Text, async ct => await GetTextValue(content, PlainTextMimeType) ?? "");
			package.SetDataProvider(StandardDataFormats.Html, async ct => await GetTextValue(content, HtmlMimeType) ?? "");

			package.SetDataProvider(StandardDataFormats.Bitmap, async ct =>
			{
				var data = await content.Value;
				if (data.Image is null)
				{
					throw new InvalidOperationException("The clipboard does not contain an image.");
				}

				// The image is registered as a native file handle on the JS side and streamed on demand.
				return RandomAccessStreamReference.CreateFromFile(CreateStorageFile(data.Image, lease));
			});

			if (includeStorageItems)
			{
				package.SetDataProvider(StandardDataFormats.StorageItems, async ct =>
				{
					// A paste gesture that carried no files resolves to an empty list rather than
					// failing, so optimistic paste handlers degrade to a graceful no-op.
					var data = await content.Value;
					return (IReadOnlyList<IStorageItem>)data.Files.Select(info => CreateStorageFile(info, lease)).ToList();
				});
			}
		}

		private static async Task<string?> GetTextValue(Lazy<Task<ClipboardContentData>> content, string mimeType)
		{
			var data = await content.Value;
			return data.Texts.FirstOrDefault(entry => entry.Type == mimeType)?.Value;
		}

		private static async Task<ClipboardContentData> GetClipboardContentAsync(double pasteShortcutTime, ClipboardHandleLease lease)
		{
			var data = JsonHelper.Deserialize<ClipboardContentData>(
				await NativeMethods.GetContentAsync(pasteShortcutTime), ClipboardSerializationContext.Default);

			lease.Add(data.Handles);

			return data.Status switch
			{
				ClipboardContentStatus.Denied => throw new UnauthorizedAccessException(
					"Access to the clipboard was denied by the browser. Reading the clipboard requires user permission or a paste gesture."),
				ClipboardContentStatus.Failed => throw new InvalidOperationException(
					"The browser failed to read the clipboard. See the browser console for details."),
				ClipboardContentStatus.Unavailable => throw new NotSupportedException(
					"The browser clipboard API is not available in this context. A secure context (HTTPS) is required."),
				_ => data,
			};
		}

		private static StorageFile CreateStorageFile(NativeStorageItemInfo info, ClipboardHandleLease? lease)
		{
			var file = StorageFile.GetFromNativeInfo(info);
			lease?.Own(file);
			return file;
		}

		private static string ToFormatId(string mimeType) => mimeType switch
		{
			PlainTextMimeType => StandardDataFormats.Text,
			HtmlMimeType => StandardDataFormats.Html,
			RtfMimeType => StandardDataFormats.Rtf,
			_ => mimeType, // Custom format ids pass through unchanged
		};

		// https://datatracker.ietf.org/doc/html/rfc2483#section-5
		private static void SetUriListContent(DataPackage package, string uriList)
		{
			var line = uriList
				.Split(_newLineChars, StringSplitOptions.RemoveEmptyEntries)
				.FirstOrDefault(line => !line.StartsWith('#'));

			// The list comes from another application; a malformed entry is dropped rather
			// than failing the whole view.
			if (!Uri.TryCreate(line, UriKind.Absolute, out var uri))
			{
				return;
			}

			// WinUI reserves WebLink for http and https; any other scheme is an application link.
			if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
			{
				package.SetWebLink(uri);
			}
			else
			{
				package.SetApplicationLink(uri);
			}
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

		// The files a view exposes are registered on the JS side so they can be streamed on
		// demand. The registrations are released once neither the view nor a storage item handed
		// out from it can be reached anymore, so a file the application holds on to keeps working.
		private sealed class ClipboardHandleLease
		{
			private static readonly ConditionalWeakTable<object, ClipboardHandleLease> _owners = new();

			private readonly List<string> _handles = new();

			public ClipboardHandleLease()
			{
			}

			public ClipboardHandleLease(IEnumerable<string> handles)
			{
				_handles.AddRange(handles);
			}

			public void Add(IEnumerable<string> handles)
			{
				lock (_handles)
				{
					_handles.AddRange(handles);
				}
			}

			// Keeps the lease alive for as long as the owner is.
			public void Own(object owner) => _owners.AddOrUpdate(owner, this);

			~ClipboardHandleLease()
			{
				string[] handles;
				lock (_handles)
				{
					handles = _handles.ToArray();
				}

				if (handles.Length > 0)
				{
					Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(() => NativeMethods.ReleaseHandles(string.Join(";", handles)));
				}
			}
		}
	}
}
