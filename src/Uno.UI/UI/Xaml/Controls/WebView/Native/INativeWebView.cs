using System;
using System.Net.Http;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWebView
{
	void GoBack();
	void GoForward();
	void Stop();
	void Reload();

	void ProcessNavigation(Uri uri);

	void ProcessNavigation(string html);

	void ProcessNavigation(HttpRequestMessage httpRequestMessage);
}
