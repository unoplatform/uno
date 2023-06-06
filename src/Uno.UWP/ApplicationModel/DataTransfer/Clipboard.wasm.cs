#nullable disable // Not supported by WinUI yet

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.Extensions.Specialized;
using Uno.Foundation;
using System.Threading;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.ApplicationModel.DataTransfer.Clipboard.NativeMethods;
#endif

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Uno.Utils.Clipboard";
#endif

		public static void Clear() => SetClipboardText(string.Empty);

		public static void SetContent(DataPackage/* ? */ content)
		{
			_ = Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.High,
				() => _ = SetContentAsync(content));
		}

		internal static async Task SetContentAsync(DataPackage/* ? */ content)
		{
			var data = content?.GetView(); // Freezes the DataPackage
			if (data?.Contains(StandardDataFormats.Text) ?? false)
			{
				var text = await data.GetTextAsync();
				SetClipboardText(text);
			}
		}

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			dataPackage.SetDataProvider(StandardDataFormats.Text, async ct => await GetClipboardText(ct));

			return dataPackage.GetView();
		}

		private static async Task<string> GetClipboardText(CancellationToken ct)
		{
#if NET7_0_OR_GREATER
			return await NativeMethods.GetTextAsync();
#else
			var command = $"{JsType}.getText();";
			var text = await WebAssemblyRuntime.InvokeAsync(command, ct);

			return text;
#endif
		}

		private static void SetClipboardText(string text)
		{
#if NET7_0_OR_GREATER
			NativeMethods.SetText(text);
#else
			var escapedText = WebAssemblyRuntime.EscapeJs(text);
			var command = $"{JsType}.setText(\"{escapedText}\");";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		private static void StartContentChanged()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StartContentChanged();
#else
			var command = $"{JsType}.startContentChanged()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		private static void StopContentChanged()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StopContentChanged();
#else
			var command = $"{JsType}.stopContentChanged()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchContentChanged()
		{
			OnContentChanged();
			return 0;
		}
	}
}
