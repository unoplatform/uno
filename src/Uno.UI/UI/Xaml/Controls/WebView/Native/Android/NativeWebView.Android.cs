using Uno.UI;
using Uno.UI.Xaml.Controls;

using AWebView = Android.Webkit.WebView;

namespace Microsoft.UI.Xaml.Controls;

public partial class NativeWebView : AWebView
{
	public NativeWebView() : base(ContextHelper.Current)
	{
	}
}
