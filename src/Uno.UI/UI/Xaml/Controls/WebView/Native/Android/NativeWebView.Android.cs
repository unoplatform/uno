using Uno.UI;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class NativeWebView : Android.Webkit.WebView
{
	public NativeWebView() : base(ContextHelper.Current)
	{
	}
}
