#if __ANDROID__
using Android.Content;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void SetContent(DataPackage content)
		{
			if(content is null)
			{
				throw new ArgumentNullException("cannot SetContent - null parameter");
			}

			ClipData clipData=null;

			if (content.Text != null)
			{
				clipData = ClipData.NewPlainText("clipdata", content.Text);
			}

			if (content._uri != null)
			{
				clipData = ClipData.NewRawUri("clipdata", Android.Net.Uri.Parse(content._uri.ToString()));
			}

			if (content._html != null)
			{
				string plainText = "";
				if(content.Text != null)
				{
					plainText = content.Text;
				}
				else
				{
					// simple conversion from HTML to plaintext
					plainText = content._html;
					for(int tagStart = plainText.IndexOf("<"); tagStart > -1; tagStart = plainText.IndexOf("<", tagStart))
					{
						int tagEnd = plainText.IndexOf(">", tagStart);
						if(tagEnd <0)
						{
							break;	// end of loop - we have start, but without end
						}

						plainText = plainText.Remove(tagStart, tagEnd - tagStart + 1);
					}
				}
				clipData = ClipData.NewHtmlText("clipdata", plainText, content._html);
			}

			if(clipData is null)
			{
				throw new ArgumentException("cannot SetContent - no text, html nor uri data");
			}
			var manager = ContextHelper.Current.GetSystemService(Context.ClipboardService) as ClipboardManager;
			manager.PrimaryClip = clipData;
		}

		public static DataPackageView GetContent()
		{
			var manager = ContextHelper.Current.GetSystemService(Context.ClipboardService) as ClipboardManager;
			if(manager is null)
			{
				return null;
			}

			var clipData = manager.PrimaryClip;
			//if (!clipData.Description.HasMimeType(ClipDescription.MimetypeTextPlain))
			//	return null;

			string clipText = null;
			Uri clipUri = null;
			string clipHtml = null;
			for(int iLp = 0; iLp<clipData.ItemCount; iLp++)
			{
				var itemText = clipData.GetItemAt(iLp).Text;
				if(itemText != null)
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

			if(clipText != null)
			{
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text);
				clipView._text = clipText;
			}

			if (clipHtml != null)
			{
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Html);
				clipView._html = clipHtml;
			}

			if (clipUri != null)
			{ // now, both Uri and WebLink == "UniformResourceLocatorW", but it can change
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Uri);
				clipView._availableFormats.Add(Windows.ApplicationModel.DataTransfer.StandardDataFormats.WebLink);
				clipView._uri = clipUri;
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
