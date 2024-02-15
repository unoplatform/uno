#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;

using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSClipboardExtension : IClipboardExtension
{
	public static MacOSClipboardExtension Instance = new();

	private MacOSClipboardExtension()
	{
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(IClipboardExtension), o => Instance);
		NativeUno.uno_clipboard_set_content_changed_callback(&ContentChangedCallback);
	}

#pragma warning disable CS0067
	public event EventHandler<object>? ContentChanged;
#pragma warning restore CS0067

	public void Clear() => NativeUno.uno_clipboard_clear();

	public void Flush()
	{
		// nothing to do (natively) for macOS but it emits a ContentChanged event (on Windows)
		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	public DataPackageView GetContent()
	{
		var dataPackage = new DataPackage();

		NativeUno.uno_clipboard_get_content(out var html, out var rtf, out var text, out var uri, out var fileUrl);

		if (html is not null)
		{
			dataPackage.SetHtmlFormat(html);
		}
		if (rtf is not null)
		{
			dataPackage.SetRtf(rtf);
		}
		if (text is not null)
		{
			dataPackage.SetText(text);
		}
		if (uri is not null)
		{
			DataPackage.SeparateUri(
				uri,
				out string? webLink,
				out string? applicationLink);

			if (webLink is not null)
			{
				dataPackage.SetWebLink(new Uri(webLink));
			}

			if (applicationLink is not null)
			{
				dataPackage.SetApplicationLink(new Uri(applicationLink));
			}

			// Deprecated but still added for compatibility
			dataPackage.SetUri(new Uri(uri));
		}
		if (fileUrl is not null)
		{
			// Drag and drop will use temporary URLs similar to: file:///.file/id=1234567.1234567
			// Files may be very large, we never want to load them until they are needed.
			// Therefore, create a data provider used to asynchronously fetch the file.
			dataPackage.SetDataProvider(
				StandardDataFormats.StorageItems,
				async cancellationToken =>
				{
					var file = await StorageFile.GetFileFromPathAsync(fileUrl);

					var storageItems = new List<IStorageItem>();
					storageItems.Add(file);

					return storageItems.AsReadOnly();
				});
		}

		return dataPackage.GetView();
	}

	public void SetContent(DataPackage content)
	{
		if (content is null)
		{
			throw new ArgumentNullException(nameof(content));
		}

		_ = CoreDispatcher.Main.RunAsync(
			CoreDispatcherPriority.High,
			async () => await SetContentAsync(content));
	}

	internal static async Task SetContentAsync(DataPackage content)
	{
		var data = content.GetView();
		if (data is null)
		{
			return;
		}

		string? html = null;
		if (data.Contains(StandardDataFormats.Html))
		{
			html = await data.GetHtmlFormatAsync();
		}
		string? rtf = null;
		if (data.Contains(StandardDataFormats.Rtf))
		{
			rtf = await data.GetRtfAsync();
		}
		string? text = null;
		if (data.Contains(StandardDataFormats.Text))
		{
			text = await data.GetTextAsync();
		}

		var uri = DataPackage.CombineUri(
			data.Contains(StandardDataFormats.WebLink) ? (await data.GetWebLinkAsync()).ToString() : null,
			data.Contains(StandardDataFormats.ApplicationLink) ? (await data.GetApplicationLinkAsync()).ToString() : null,
			data.Contains(StandardDataFormats.Uri) ? (await data.GetUriAsync()).ToString() : null);

		if (data.Contains(StandardDataFormats.Bitmap) ||
			data.Contains(StandardDataFormats.StorageItems) ||
			data.Contains(StandardDataFormats.UserActivityJsonArray))
		{
			if (typeof(MacOSClipboardExtension).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(MacOSClipboardExtension).Log().Warn($"StandardDataFormats.[Bitmap | StorageItems | UserActivityJsonArray] are not supported on this platform");
			}
		}

		NativeUno.uno_clipboard_set_content(html, rtf, text, uri.Length > 0 ? uri : null);
	}

	public void StartContentChanged() => NativeUno.uno_clipboard_start_content_changed();

	public void StopContentChanged() => NativeUno.uno_clipboard_stop_content_changed();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void ContentChangedCallback()
	{
		Instance.ContentChanged?.Invoke(Instance, EventArgs.Empty);
	}
}
