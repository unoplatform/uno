#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using UIKit;

namespace Uno.UI.Extensions
{
	internal static class UIBarButtonItemExtensions
	{
		/// <summary>
		/// In some narrow circumstances (eg when the page is unloaded, but not always) setting CustomView to null can trigger a native exception,
		/// even if it's already null. To avoid this, skip redundantly setting it to null.
		/// </summary>
		public static void ClearCustomView(this UIBarButtonItem barButtonItem)
		{
			if (barButtonItem?.CustomView != null)
			{
				barButtonItem.CustomView = null;
			}
		}
	}
}
