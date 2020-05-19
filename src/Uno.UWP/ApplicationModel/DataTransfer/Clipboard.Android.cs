#if __ANDROID__
using Android.Content;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void SetContent(DataPackage content)
		{
			if (content is null)
			{
				throw new ArgumentNullException("cannot SetContent - null parameter");
			}

			ClipData clipData = null;

			if (content.Text != null)
			{
				clipData = ClipData.NewPlainText("clipdata", content.Text);
			}

			if (content.Uri != null)
			{
				clipData = ClipData.NewRawUri("clipdata", Android.Net.Uri.Parse(content.Uri.ToString()));
			}

			if (content.Html != null)
			{
				string plainText = "";
				if (content.Text != null)
				{
					plainText = content.Text;
				}
				else
				{
					// simple conversion from HTML to plaintext
					plainText = content.Html;
					for (int tagStart = plainText.IndexOf("<"); tagStart > -1; tagStart = plainText.IndexOf("<", tagStart))
					{
						int tagEnd = plainText.IndexOf(">", tagStart);
						if (tagEnd < 0)
						{
							break;  // end of loop - we have start, but without end
						}

						plainText = plainText.Remove(tagStart, tagEnd - tagStart + 1);
					}
				}
				clipData = ClipData.NewHtmlText("clipdata", plainText, content.Html);
			}

			if (clipData is null)
			{
				throw new ArgumentException("Cannot SetContent - no Text, HTML nor Uri data");
			}

			var manager = ContextHelper.Current.GetSystemService(Context.ClipboardService) as ClipboardManager;
			manager.PrimaryClip = clipData;
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
			for (int iLp = 0; iLp < clipData.ItemCount; iLp++)
			{
				var itemText = clipData.GetItemAt(iLp).Text;
				if (itemText != null)
				{
					clipText = itemText;
				}
				var itemUri = clipData.GetItemAt(iLp).Uri;
				if (itemUri != null)
				{
					clipUri = new Uri(itemUri.ToString());
				}
				var itemHtml = clipData.GetItemAt(iLp).HtmlText;
				if (itemText != null)
				{
					clipHtml = itemHtml;
				}
			}


			if (clipText is null && clipUri is null && clipHtml is null)
			{ // we don't have anything...
				return null;
			}


			// please, while changing it - synchronize it with DataPackage.GetView()
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
