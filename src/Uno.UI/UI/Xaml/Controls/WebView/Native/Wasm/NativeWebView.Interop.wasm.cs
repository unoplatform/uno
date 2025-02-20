using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.UI.Xaml.Controls;

internal static partial class NativeWebView
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Windows.UI.Xaml.Controls.WebView.reload")]
		internal static partial void Reload(IntPtr htmlId);

		[JSImport("globalThis.Windows.UI.Xaml.Controls.WebView.stop")]
		internal static partial void Stop(IntPtr htmlId);

		[JSImport("globalThis.Windows.UI.Xaml.Controls.WebView.goBack")]
		internal static partial void GoBack(IntPtr htmlId);

		[JSImport("globalThis.Windows.UI.Xaml.Controls.WebView.goForward")]
		internal static partial void GoForward(IntPtr htmlId);

		[JSImport("globalThis.Windows.UI.Xaml.Controls.WebView.executeScriptAsync")]
		internal static partial string ExecuteScript(IntPtr htmlId, string script);

		[JSImport("globalThis.Windows.UI.Xaml.Controls.WebView.getDocumentTitle")]
		internal static partial string GetDocumentTitle(IntPtr htmlId);
	}
}
