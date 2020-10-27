#if __ANDROID__
#nullable disable // Not supported by WinUI yet

using Android.Content;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string ClipboardDataLabel = nameof(Clipboard);

		public static void SetContent(DataPackage/* ? */ content)
		{
			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			// Notes:
			// 1. We don't want to change the SetContent signature to async so
			//    async code is run on the CoreDispatcher.
			// 2. All async code is run in the same task to avoid potential threading concerns.
			//    Otherwise, it would be possible to set the OS clipboard data (code at the end)
			//    before one or more of the data formats is ready.
			CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				() => SetContentAsync(content));
		}

		internal static async Task SetContentAsync(DataPackage content)
		{
			var data = content?.GetView();

			var items = new List<ClipData.Item>();
			var mimeTypes = new List<string>();

			if (data?.Contains(StandardDataFormats.Text) ?? false)
			{
				var text = await data.GetTextAsync();

				items.Add(new ClipData.Item(text));
				mimeTypes.Add("text/plaintext");
			}

			if (data != null)
			{
				var uri = DataPackage.CombineUri(
					data.Contains(StandardDataFormats.WebLink) ? (await data.GetWebLinkAsync()).ToString() : null,
					data.Contains(StandardDataFormats.ApplicationLink) ? (await data.GetApplicationLinkAsync()).ToString() : null,
					data.Contains(StandardDataFormats.Uri) ? (await data.GetUriAsync()).ToString() : null);

				if (string.IsNullOrEmpty(uri) == false)
				{
					var androidUri = Android.Net.Uri.Parse(uri);

					items.Add(new ClipData.Item(androidUri));
					mimeTypes.Add("text/uri-list");
				}
			}

			if (data?.Contains(StandardDataFormats.Html) ?? false)
			{
				var html = await data.GetHtmlFormatAsync();

				// Matches all tags
				Regex regex = new Regex("(<.*?>\\s*)+", RegexOptions.Singleline);
				// Replace tags by spaces and trim
				var plainText = regex.Replace(html, " ").Trim();

				items.Add(new ClipData.Item(plainText, html));
				mimeTypes.Add("text/html");
			}

			// Set all the data formats to the Android clipboard
			if (items.Count > 0)
			{
				ClipData clipData = new ClipData(
					new ClipDescription(ClipboardDataLabel, mimeTypes.ToArray()),
					items[0]);

				for (int itemIndex = 1; itemIndex < items.Count; itemIndex++)
				{
					clipData.AddItem(items[itemIndex]);
				}

				var manager = ContextHelper.Current.GetSystemService(Context.ClipboardService) as ClipboardManager;
				if (manager is null)
				{
					return;
				}

				manager.PrimaryClip = clipData;
			}
			else
			{
				// Clear clipboard
				Clear();
			}
		}

		public static DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			var manager = ContextHelper.Current.GetSystemService(Context.ClipboardService) as ClipboardManager;
			if (manager is null)
			{
				return dataPackage.GetView();
			}

			var clipData = manager.PrimaryClip;

			Uri/* ? */ clipApplicationLink = null;
			string/* ? */ clipHtml = null;
			string/* ? */ clipText = null;
			Uri/* ? */ clipUri = null;
			Uri/* ? */ clipWebLink = null;

			// Extract all the standard data format information from the clipboard.
			// Each format can only be used once; therefore, the last occurrence of the format will be the one used.
			if (clipData != null)
			{
				for (int itemIndex = 0; itemIndex < clipData.ItemCount; itemIndex++)
				{
					var item = clipData.GetItemAt(itemIndex);

					if (item != null)
					{
						var itemText = item.Text;
						if (itemText != null)
						{
							clipText = itemText;
						}

						var itemUriStr = item.Uri?.ToString();
						if (itemUriStr != null)
						{
							DataPackage.SeparateUri(
								itemUriStr,
								out string webLink,
								out string applicationLink);

							clipWebLink         = webLink != null ? new Uri(webLink) : null;
							clipApplicationLink = applicationLink != null ? new Uri(applicationLink) : null;
							clipUri             = new Uri(itemUriStr); // Deprecated but still added for compatibility
						}

						var itemHtml = item.HtmlText;
						if (itemHtml != null)
						{
							clipHtml = itemHtml;
						}
					}
				}
			}

			// Add standard data formats to the data package.
			// This can be done synchronously on Android since the data is already available from above.
			if (clipApplicationLink != null)
			{
				dataPackage.SetApplicationLink(clipApplicationLink);
			}

			if (clipHtml != null)
			{
				dataPackage.SetHtmlFormat(clipHtml);
			}

			if (clipText != null)
			{
				dataPackage.SetText(clipText);
			}

			if (clipUri != null)
			{
				dataPackage.SetUri(clipUri);
			}

			if (clipWebLink != null)
			{
				dataPackage.SetWebLink(clipWebLink);
			}

			return dataPackage.GetView();
		}

		public static void Clear()
		{
			if (ContextHelper.Current.GetSystemService(Context.ClipboardService) is ClipboardManager manager)
			{
				var clipData = ClipData.NewPlainText("", "");
				manager.PrimaryClip = clipData;
			}
		}

		private static void StartContentChanged()
		{
			if (ContextHelper.Current.GetSystemService(Context.ClipboardService) is ClipboardManager manager)
			{
				manager.PrimaryClipChanged += Manager_PrimaryClipChanged;
			}
		}

		private static void StopContentChanged()
		{
			if (ContextHelper.Current.GetSystemService(Context.ClipboardService) is ClipboardManager manager)
			{
				manager.PrimaryClipChanged -= Manager_PrimaryClipChanged;
			}
		}

		private static void Manager_PrimaryClipChanged(object sender, EventArgs e)
		{
			OnContentChanged();
		}
	}
}
#endif
