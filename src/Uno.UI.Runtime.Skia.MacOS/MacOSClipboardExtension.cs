using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSClipboardExtension : IClipboardExtension
{
	private static readonly MacOSClipboardExtension _instance = new();

	private MacOSClipboardExtension()
	{
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(IClipboardExtension), _ => _instance);
		NativeUno.uno_clipboard_set_content_changed_callback(&ContentChangedCallback);
	}

	public event EventHandler<object>? ContentChanged;

	public void Clear() => NativeUno.uno_clipboard_clear();

	// nothing to do (natively) for macOS but it emits a ContentChanged event (on Windows)
	public void Flush() => ContentChanged?.Invoke(this, EventArgs.Empty);

	public DataPackageView GetContent()
	{
		var dataPackage = new DataPackage();

		var clipboard = new NativeClipboardData();
		NativeUno.uno_clipboard_get_content(ref clipboard);

		if (clipboard.HtmlContent is not null)
		{
			dataPackage.SetHtmlFormat(clipboard.HtmlContent);
		}
		if (clipboard.RtfContent is not null)
		{
			dataPackage.SetRtf(clipboard.RtfContent);
		}
		if (clipboard.TextContent is not null)
		{
			dataPackage.SetText(clipboard.TextContent);
		}
		if (clipboard.Uri is not null)
		{
			DataPackage.SeparateUri(
				clipboard.Uri,
				out var webLink,
				out var applicationLink);

			if (webLink is not null)
			{
				dataPackage.SetWebLink(new Uri(webLink));
			}

			if (applicationLink is not null)
			{
				dataPackage.SetApplicationLink(new Uri(applicationLink));
			}

			// Deprecated but still added for compatibility
			dataPackage.SetUri(new Uri(clipboard.Uri));
		}

		if (clipboard.BitmapPath is not null)
		{
			dataPackage.SetDataProvider(
				StandardDataFormats.Bitmap,
				_ =>
				{
					var image = File.OpenRead(clipboard.BitmapPath);
					return Task.FromResult<object>(new RandomAccessStreamReference(_
						=> Task.FromResult(image.AsRandomAccessStream().TrySetContentType(clipboard.BitmapFormat!))));
				});
		}
		else if (clipboard.BitmapData?.Length > 0)
		{
			dataPackage.SetDataProvider(
				StandardDataFormats.Bitmap,
				_ =>
				{
					var stream = new MemoryStream(clipboard.BitmapData);
					return Task.FromResult<object>(new RandomAccessStreamReference(_
						=> Task.FromResult(stream.AsRandomAccessStream().TrySetContentType(clipboard.BitmapFormat!))));
				});
		}

		if (clipboard.FileUrl is not null)
		{
			// Drag and drop will use temporary URLs similar to: file:///.file/id=1234567.1234567
			// Files may be very large, we never want to load them until they are needed.
			// Therefore, create a data provider used to asynchronously fetch the file.
			dataPackage.SetDataProvider(
				StandardDataFormats.StorageItems,
				async _ =>
				{
					var storageItems = new List<IStorageItem>
					{
						await StorageFile.GetFileFromPathAsync(clipboard.FileUrl)
					};

					return storageItems.AsReadOnly();
				});
		}

		return dataPackage.GetView();
	}

	public void SetContent(DataPackage content)
	{
		ArgumentNullException.ThrowIfNull(content, nameof(content));

		_ = CoreDispatcher.Main.RunAsync(
			CoreDispatcherPriority.High,
			async () => await SetContentAsync(content));
	}

	private static async Task SetContentAsync(DataPackage content)
	{
		var data = content.GetView();
		var clipboard = new NativeClipboardData();

		if (data.Contains(StandardDataFormats.Html))
		{
			clipboard.HtmlContent = await data.GetHtmlFormatAsync();
		}
		if (data.Contains(StandardDataFormats.Rtf))
		{
			clipboard.RtfContent = await data.GetRtfAsync();
		}
		if (data.Contains(StandardDataFormats.Text))
		{
			clipboard.TextContent = await data.GetTextAsync();
		}

		if (data.Contains(StandardDataFormats.Bitmap))
		{
			await using var stream = (await (await data.GetBitmapAsync()).OpenReadAsync()).AsStream();
			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms);
			clipboard.BitmapData = ms.ToArray();
		}

		var uri = DataPackage.CombineUri(
			data.Contains(StandardDataFormats.WebLink) ? (await data.GetWebLinkAsync()).ToString() : null,
			data.Contains(StandardDataFormats.ApplicationLink) ? (await data.GetApplicationLinkAsync()).ToString() : null,
			data.Contains(StandardDataFormats.Uri) ? (await data.GetUriAsync()).ToString() : null);
		clipboard.Uri = string.IsNullOrEmpty(uri) ? null : uri;

		if (data.Contains(StandardDataFormats.StorageItems) ||
			data.Contains(StandardDataFormats.UserActivityJsonArray))
		{
			if (typeof(MacOSClipboardExtension).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(MacOSClipboardExtension).Log().Warn($"StandardDataFormats.[StorageItems | UserActivityJsonArray] are not supported on this platform");
			}
		}

		NativeUno.uno_clipboard_set_content(ref clipboard);
	}

	public void StartContentChanged() => NativeUno.uno_clipboard_start_content_changed();

	public void StopContentChanged() => NativeUno.uno_clipboard_stop_content_changed();

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void ContentChangedCallback() => _instance.ContentChanged?.Invoke(_instance, EventArgs.Empty);
}
