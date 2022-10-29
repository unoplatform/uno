#nullable disable // Not supported by WinUI yet

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Windows.Storage.Streams;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string JsType = "Uno.Utils.Clipboard";

		public static void Clear() => SetClipboardText(string.Empty);

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			// Implementation notes:
			//
			// In Javascript, the only clipboard API available is `read`, which is asynchronous.
			// There is no other way for the application to enumerate available data types.
			//
			// We therefore set a data provider for every supported type and then run
			// `InitializeFromNativeClipboardAsync` as soon as possible. This functions
			// asynchronously reads the browser's clipboard and check which data types are available
			// and removes the data providers for unavailable types.
			//
			// Before the `DataPackage` is completely initialized, C# code might wrongly think
			// that the `DataPackage` actually contains data (text, rich text, bitmaps,...) while in
			// reality there isn't. In such case, reading clipboard data from the `DataPackageView` will
			// return empty values (string.Empty, or an empty stream for bitmaps).
			//
			// As calls to `InitializeFromNativeClipboardAsync` are included in each data provider,
			// to avoid false positives, the C# code can simply call any asynchrnous `DataPackageView` function
			// to ensure initialization of the WASM `DataPackage`:
			//
			// var dataPackageView = Clipboard.GetContent();
			// _ = await dataPackageView.GetTextAsync();
			// if (dataPackageView.Contains("The desired data format"))
			// { // Do work here.

			dataPackage.SetDataProvider(StandardDataFormats.Text, async (ct) =>
				{ await dataPackage.InitializeFromNativeClipboardAsync(ct); return await GetClipboardText(ct); });

			dataPackage.SetDataProvider(StandardDataFormats.Rtf, async (ct) =>
				{ await dataPackage.InitializeFromNativeClipboardAsync(ct); return await GetClipboardRichText("text/rtf", ct); });

			dataPackage.SetDataProvider(StandardDataFormats.Html, async (ct) =>
				{ await dataPackage.InitializeFromNativeClipboardAsync(ct); return await GetClipboardRichText("text/html", ct); });

			// Currently, on most browsers the only supported bitmap type on Clipboard is "image/png".
			dataPackage.SetDataProvider(StandardDataFormats.Bitmap, async (ct) =>
				{ await dataPackage.InitializeFromNativeClipboardAsync(ct); return await GetClipboardBitmap("image/png", ct); });

			_ = Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.High,
				() => dataPackage.InitializeFromNativeClipboardAsync());

			return dataPackage.GetView();
		}

		public static void SetContent(DataPackage/* ? */ content)
		{
			_ = Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.High,
				() => _ = SetContentAsync(content));
		}

		internal static async Task SetContentAsync(DataPackage/* ? */ content)
		{
			var data = content?.GetView(); // Freezes the DataPackage
			using (var marshalledDataList = new CompositeDisposable())
			{
				if (data?.Contains(StandardDataFormats.Text) ?? false)
				{
					var text = await data.GetTextAsync();
					// Special case for text: `SetClipboardText` calls `writeText` which
					// is more widely supported. Furthermore, it has a workaround using the
					// `copy` event for non-HTTPS pages.
					// If the code below succeeds, the data would be overwritten anyway.
					SetClipboardText(text);

					marshalledDataList.Add(new MarshalledDataWithMime(text, "text/plain"));
				}
				// Most browsers don't support MIME types other than plain, html and png.
				// if (data?.Contains(StandardDataFormats.Rtf) ?? false)
				// {
				//	var text = await data.GetRtfAsync();
				//	marshalledDataList.Add(new MarshalledDataWithMime(text, "text/rtf"));
				// }
				if (data?.Contains(StandardDataFormats.Html) ?? false)
				{
					var text = await data.GetHtmlFormatAsync();
					marshalledDataList.Add(new MarshalledDataWithMime(text, "text/html"));
				}
				if (data?.Contains(StandardDataFormats.Bitmap) ?? false)
				{
					var streamReference = await data.GetBitmapAsync();
					var randomAccessStream = await streamReference.OpenReadAsync();
					var sourceStream = randomAccessStream.AsStreamForRead();
					var memoryStream = new MemoryStream();

					await sourceStream.CopyToAsync(memoryStream);

					marshalledDataList.Add(new MarshalledDataWithMime(memoryStream.ToArray(), "image/png"));
				}
				// This JSON serializer does not recognize MarshalledDataWithMime in the IDisposable collection.
				// We therefore have to explicitly cast it to a list.
				// This should not be expensive as the list is really small.
				var json = JsonHelper.Serialize(marshalledDataList.Cast<MarshalledDataWithMime>().ToList());
				var command = $"{JsType}.setData({json});";
				await WebAssemblyRuntime.InvokeAsync(command);
			}
		}

		private static async Task<string> GetClipboardText(CancellationToken ct)
		{
			var command = $"{JsType}.getText();";
			var text = await WebAssemblyRuntime.InvokeAsync(command, ct);

			return text;
		}

		private static void SetClipboardText(string text)
		{
			var escapedText = WebAssemblyRuntime.EscapeJs(text);
			var command = $"{JsType}.setText(\"{escapedText}\");";
			WebAssemblyRuntime.InvokeJS(command);
		}

		private static async Task<string> GetClipboardRichText(string mime, CancellationToken ct)
		{
			using (var data = await GetClipboardData(mime, ct))
			{
				return data?.ToString() ?? string.Empty;
			}
		}

		private static Task<RandomAccessStreamReference> GetClipboardBitmap(string mime, CancellationToken ct)
		{
			return Task.FromResult(new RandomAccessStreamReference(async (ct) =>
			{
				MemoryStream memoryStream;

				using (var data = await GetClipboardData(mime, ct))
				{
					memoryStream = new MemoryStream(data?.ToArray() ?? Array.Empty<byte>());
				}

				return new RandomAccessStreamOverStream(memoryStream).TrySetContentType();
			}));
		}

		private static async Task<MarshalledData> GetClipboardData(string mime, CancellationToken ct)
		{
			var escapedMime = WebAssemblyRuntime.EscapeJs(mime);
			var json = await WebAssemblyRuntime.InvokeAsync($"{JsType}.getDataAsJson(\"{escapedMime}\")", ct);

			if (json == null)
			{
				return null;
			}

			return JsonHelper.Deserialize<MarshalledData>(json);
		}

		private static void StartContentChanged()
		{
			var command = $"{JsType}.startContentChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		private static void StopContentChanged()
		{
			var command = $"{JsType}.stopContentChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		public static int DispatchContentChanged()
		{
			OnContentChanged();
			return 0;
		}
	}
}
