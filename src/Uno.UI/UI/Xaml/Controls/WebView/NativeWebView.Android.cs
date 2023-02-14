using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NativeWebView : Android.Webkit.WebView
	{
		public NativeWebView() : base(ContextHelper.Current)
		{

		}
	}
}
