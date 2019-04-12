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
	}
}
#endif
