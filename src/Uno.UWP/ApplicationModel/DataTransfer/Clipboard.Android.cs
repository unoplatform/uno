#if __ANDROID__
using Android.Content;
using Android.Text.Method;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uno.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string ClipboardDataLabel = nameof(Clipboard);

		public static void SetContent(DataPackage content)
		{
			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			var items = new List<ClipData.Item>();
			var mimeTypes = new List<string>();
			if (content.Text != null)
			{
				items.Add(new ClipData.Item(content.Text));
				mimeTypes.Add("text/plaintext");
			}
			
			if (content.Uri != null)
			{
				var androidUri = Android.Net.Uri.Parse(content.Uri.ToString());
				items.Add(new ClipData.Item(androidUri));
				mimeTypes.Add("text/uri-list");
			}
			
			if (content.Html != null)
			{
				// Matches all tags
				Regex regex = new Regex("(<.*?>\\s*)+", RegexOptions.Singleline);
				// Replace tags by spaces and trim
				var plainText = regex.Replace(content.Html, " ").Trim();

				items.Add(new ClipData.Item(plainText, content.Html));
				mimeTypes.Add("text/html");
			}

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
			var manager = ContextHelper.Current.GetSystemService(Context.ClipboardService) as ClipboardManager;
			if (manager is null)
			{
				return null;
			}

			var clipData = manager.PrimaryClip;

			string clipText = null;
			Uri clipUri = null;
			string clipHtml = null;
			for (int itemIndex = 0; itemIndex < clipData.ItemCount; itemIndex++)
			{
				var itemText = clipData.GetItemAt(itemIndex).Text;
				if (itemText != null)
				{
					clipText = itemText;
				}
				var itemUri = clipData.GetItemAt(itemIndex).Uri;
				if (itemUri != null)
				{
					clipUri = new Uri(itemUri.ToString());
				}
				var itemHtml = clipData.GetItemAt(itemIndex).HtmlText;
				if (itemText != null)
				{
					clipHtml = itemHtml;
				}
			}

			var clipView = new DataPackageView();

			if (clipText != null)
			{
				clipView.SetFormatTask(StandardDataFormats.Text, Task.FromResult(clipText));
			}

			if (clipHtml != null)
			{
				clipView.SetFormatTask(StandardDataFormats.Html, Task.FromResult(clipHtml));
			}

			if (clipUri != null)
			{
				clipView.SetFormatTask(StandardDataFormats.Uri, Task.FromResult(clipUri));
				clipView.SetFormatTask(StandardDataFormats.WebLink, Task.FromResult(clipUri));
			}

			return clipView;
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
