#nullable disable // Not supported by WinUI yet

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
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
			return await NativeMethods.GetTextAsync();
		}

		private static void SetClipboardText(string text)
		{
			NativeMethods.SetText(text);
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
