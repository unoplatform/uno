#nullable enable

using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	private readonly WebViewVirtualHostMap _virtualHostMap = new();

	/// <summary>
	/// The virtual hosts of this WebView. Only translated when the native view implements <see cref="ISupportsCustomSchemeVirtualHosts"/>.
	/// </summary>
	internal WebViewVirtualHostMap VirtualHostMap => _virtualHostMap;

	// Null when the native view reports public URLs itself: every translation below is then the identity.
	private WebViewVirtualHostMap? ActiveVirtualHostMap
		=> _nativeWebView is ISupportsCustomSchemeVirtualHosts ? _virtualHostMap : null;

	private Uri ToNativeUri(Uri uri) => ActiveVirtualHostMap?.ToNative(uri) ?? uri;

	private Uri? ToPublicUri(Uri? uri) => uri is null ? null : ActiveVirtualHostMap?.ToPublic(uri) ?? uri;

	private string? ToPublicUriString(string? uri) => uri is null ? null : ActiveVirtualHostMap?.ToPublic(uri) ?? uri;

	private string ToNativeFilter(string filter) => ActiveVirtualHostMap?.ToNativeFilter(filter) ?? filter;

	private void AttachVirtualHostMap() => (_nativeWebView as ISupportsCustomSchemeVirtualHosts)?.SetVirtualHostMap(_virtualHostMap);

	private CoreWebView2WebResourceRequestedEventArgs ToPublicArgs(CoreWebView2WebResourceRequestedEventArgs args)
		=> ActiveVirtualHostMap is { } map
			? new CoreWebView2WebResourceRequestedEventArgs(new PublicUriWebResourceRequestedEventArgs(args.NativeArgsInterface, map))
			: args;

	/// <summary>
	/// Shows a native request with its public URI; everything else, the response included, goes to the native args.
	/// </summary>
	internal sealed class PublicUriWebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
	{
		public PublicUriWebResourceRequestedEventArgs(INativeWebResourceRequestedEventArgs inner, WebViewVirtualHostMap map)
		{
			Inner = inner;
			Request = new PublicUriWebResourceRequest(inner.Request, map);
		}

		public INativeWebResourceRequestedEventArgs Inner { get; }

		public INativeWebResourceRequest Request { get; }

		public INativeWebResourceResponse? Response
		{
			get => Inner.Response;
			set => Inner.Response = value;
		}

		public CoreWebView2WebResourceContext ResourceContext => Inner.ResourceContext;

		public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => Inner.RequestedSourceKind;

		public Deferral GetDeferral() => Inner.GetDeferral();
	}

	private sealed class PublicUriWebResourceRequest(INativeWebResourceRequest inner, WebViewVirtualHostMap map) : INativeWebResourceRequest
	{
		public string Uri
		{
			get => map.ToPublic(inner.Uri);
			set => inner.Uri = map.ToNative(value);
		}

		public string Method
		{
			get => inner.Method;
			set => inner.Method = value;
		}

		public IRandomAccessStream Content
		{
			get => inner.Content;
			set => inner.Content = value;
		}

		public INativeHttpRequestHeaders Headers => inner.Headers;
	}
}
