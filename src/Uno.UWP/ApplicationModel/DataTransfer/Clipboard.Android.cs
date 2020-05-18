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
			if (ContextHelper.Current.GetSystemService(Context.ClipboardService) is ClipboardManager manager)
			{
				manager.Text = content?.Text;
			}
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
