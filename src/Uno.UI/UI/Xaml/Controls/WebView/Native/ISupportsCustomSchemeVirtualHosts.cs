#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebViews that serve virtual hosts through <see cref="WebViewVirtualHostMap.NativeScheme"/>
/// instead of intercepting <c>https</c>.
/// </summary>
/// <remarks>
/// Such a view navigates to, requests, and reports native URLs (<c>uno-app://0.0.0.1/...</c>). <c>CoreWebView2</c>
/// translates at the boundary, so the app only ever sees <c>https://0.0.0.1/...</c>: <c>Source</c>, <c>Navigate</c>,
/// <c>NavigationStarting</c>, <c>NavigationCompleted</c>, <c>NewWindowRequested</c>, <c>WebMessageReceived.Source</c>,
/// <c>WebResourceRequested</c> request URIs and filters.
/// </remarks>
internal interface ISupportsCustomSchemeVirtualHosts
{
	/// <summary>
	/// Hands the view the live map: hosts registered later show up in it.
	/// </summary>
	/// <remarks>
	/// Called when the view is attached to a <c>CoreWebView2</c>, before its first navigation. Folder mappings set while
	/// the view does not implement <see cref="ISupportsVirtualHostMapping"/> are in the map too
	/// (<see cref="WebViewVirtualHostMap.TryGetFolder"/>), and the view must serve them through the scheme.
	/// </remarks>
	void SetVirtualHostMap(WebViewVirtualHostMap map);
}
