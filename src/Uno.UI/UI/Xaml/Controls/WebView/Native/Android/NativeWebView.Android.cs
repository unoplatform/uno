using Uno.UI;
using Uno.UI.Xaml.Controls;

using Android.Webkit;

namespace Microsoft.UI.Xaml.Controls;

public partial class NativeWebView : WebView
{
	public NativeWebView() : base(ContextHelper.Current)
	{
	}
}
