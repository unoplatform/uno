#nullable enable

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal partial interface INativeWebView : IDisposable
{
	string DocumentTitle { get; }

	void GoBack();

	void GoForward();

	void Stop();

	void Reload();

	void ProcessNavigation(Uri uri);

	void ProcessNavigation(string html);

	void ProcessNavigation(HttpRequestMessage httpRequestMessage);

	Task<string?> ExecuteScriptAsync(string script, CancellationToken token);

	Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token);

	void SetScrollingEnabled(bool isScrollingEnabled);
}
