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
			var manager = (ClipboardManager)ContextHelper.Current.GetSystemService(Context.ClipboardService);

			manager.Text = content.Text;
		}
	}
}
#endif