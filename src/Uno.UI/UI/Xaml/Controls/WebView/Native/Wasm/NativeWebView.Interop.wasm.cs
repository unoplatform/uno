using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Microsoft.UI.Xaml.Controls;

internal static partial class NativeWebView
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.reload")]
		internal static partial void Reload(IntPtr htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.stop")]
		internal static partial void Stop(IntPtr htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.goBack")]
		internal static partial void GoBack(IntPtr htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.goForward")]
		internal static partial void GoForward(IntPtr htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.executeScript")]
		internal static partial string ExecuteScript(IntPtr htmlId, string script);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.getDocumentTitle")]
		internal static partial string GetDocumentTitle(IntPtr htmlId);
	}
}
