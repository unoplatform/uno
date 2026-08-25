#nullable enable

namespace Microsoft.Web.WebView2.Core;

/// <remarks>
/// The static members of <see cref="CoreWebView2Environment"/> answer "which browser is installed", so they must
/// work before any <see cref="CoreWebView2"/> exists. That rules out <see cref="INativeWebViewProvider"/>, which
/// is registered with a <see cref="CoreWebView2"/> owner.
/// </remarks>
internal interface ICoreWebView2EnvironmentStaticsExtension
{
	/// <param name="browserExecutableFolder">null or empty uses the installed browser.</param>
	string GetAvailableBrowserVersionString(string? browserExecutableFolder);

	int CompareBrowserVersionString(string browserVersionString1, string browserVersionString2);
}
