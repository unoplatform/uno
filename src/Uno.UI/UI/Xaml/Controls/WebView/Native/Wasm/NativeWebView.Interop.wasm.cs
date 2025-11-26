#nullable enable

using System;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

#if WASM_SKIA
using ElementId = System.String;
#else
using ElementId = System.IntPtr;
#endif

namespace __Microsoft.UI.Xaml.Controls;

internal static partial class NativeWebView
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.buildImports")]
		internal static partial void BuildImports(string assembly);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.initializeStyling")]
		internal static partial void InitializeStyling(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.reload")]
		internal static partial void Reload(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.stop")]
		internal static partial void Stop(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.goBack")]
		internal static partial void GoBack(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.goForward")]
		internal static partial void GoForward(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.executeScript")]
		internal static partial string? ExecuteScript(ElementId htmlId, string script);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.getDocumentTitle")]
		internal static partial string? GetDocumentTitle(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.getAttribute")]
		internal static partial string? GetAttribute(ElementId htmlId, string attribute);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.getPackageBase")]
		internal static partial string GetPackageBase();

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.setAttribute")]
		internal static partial void SetAttribute(ElementId htmlId, string attribute, string value);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.navigate")]
		internal static partial void Navigate(ElementId htmlId, string url);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.setupEvents")]
		internal static partial void SetupEvents(ElementId htmlId);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.WebView.cleanupEvents")]
		internal static partial void CleanupEvents(ElementId htmlId);
	}
}
