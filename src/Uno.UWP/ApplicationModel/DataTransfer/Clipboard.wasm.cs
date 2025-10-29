#nullable disable // Not supported by WinUI yet

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
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

			// Check if we have both text and HTML - if so, set them together
			if ((data?.Contains(StandardDataFormats.Html) ?? false) && (data?.Contains(StandardDataFormats.Text) ?? false))
			{
				var text = await data.GetTextAsync();
				var html = await data.GetHtmlFormatAsync();
				await SetClipboardHtml(text, html);
			}
			else if (data?.Contains(StandardDataFormats.Html) ?? false)
			{
				// HTML only - extract plain text from HTML
				var html = await data.GetHtmlFormatAsync();
				var text = StripHtmlTags(html);
				await SetClipboardHtml(text, html);
			}
			else if (data?.Contains(StandardDataFormats.Text) ?? false)
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

		private static void SetClipboardText(string text)
		{
			NativeMethods.SetText(text);
		}

		private static async Task SetClipboardHtml(string text, string html)
		{
			await NativeMethods.SetHtmlAsync(text, html);
		}

		private static string StripHtmlTags(string html)
		{
			if (string.IsNullOrEmpty(html))
			{
				return string.Empty;
			}

			// Simple HTML tag stripping - matches Android implementation approach
			return TagMatch().Replace(html, " ").Trim();
		}

		[GeneratedRegex("(<.*?>\\s*)+", RegexOptions.Singleline)]
		private static partial Regex TagMatch();

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
