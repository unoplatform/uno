#nullable disable // Not supported by WinUI yet

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.Extensions.Specialized;
using Uno.Foundation;
using System.Threading;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string JsType = "Uno.Utils.Clipboard";

		public static void Clear() => SetClipboardText(string.Empty);

		public static void SetContent(DataPackage/* ? */ content)
		{
			Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.High,
				() => SetContentAsync(content));
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
