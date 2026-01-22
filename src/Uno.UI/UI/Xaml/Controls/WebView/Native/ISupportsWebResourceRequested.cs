#nullable enable

using System;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

internal interface ISupportsWebResourceRequested
{
	event EventHandler<CoreWebView2WebResourceRequestedEventArgs> WebResourceRequested;

	void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds);

	void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds);
}
