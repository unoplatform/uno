#nullable enable

extern alias WpfWebView;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal sealed class WpfNativeWebView : INativeWebView
{
	private WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2 _nativeWebView;
	private CoreWebView2 _coreWebView2;
	private List<Func<Task>> _actions = new();

	public WpfNativeWebView(WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2 nativeWebView, CoreWebView2 coreWebView2)
	{
		_coreWebView2 = coreWebView2;
		_nativeWebView = nativeWebView;
		nativeWebView.CoreWebView2InitializationCompleted += this.NativeWebView_CoreWebView2InitializationCompleted;
		nativeWebView.EnsureCoreWebView2Async();
	}

	private async void NativeWebView_CoreWebView2InitializationCompleted(object? sender, WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
	{
		foreach (var action in _actions)
		{
			await action();
		}

		_actions.Clear();
	}

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.ExecuteScriptAsync(script));

	public void GoBack()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.GoBack);

	public void GoForward()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.GoForward);

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
		// TODO: Use the arguments parameter
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.ExecuteScriptAsync(script));

	public void ProcessNavigation(Uri uri)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.CoreWebView2.Navigate(uri.ToString()));

	public void ProcessNavigation(string html)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.NavigateToString(html));

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
		=> ExecuteEnsuringCoreWebView2(() => ProcessNavigationCore(httpRequestMessage));

	private void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
	{
		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			// https://github.com/MicrosoftEdge/WebView2Feedback/issues/2250#issuecomment-1201765363
			// WebView2 doesn't like when you try to set some headers manually
			if (header.Key != "Host")
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
		}

		var request = _nativeWebView.CoreWebView2.Environment.CreateWebResourceRequest(httpRequestMessage.RequestUri!.ToString(), httpRequestMessage.Method.Method, httpRequestMessage.Content!.ReadAsStream(), builder.ToString());
		_nativeWebView.CoreWebView2.NavigateWithWebResourceRequest(request);
	}

	public void Reload()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.Reload);

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
	}

	public void Stop()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.Stop);

	private void ExecuteEnsuringCoreWebView2(Action action)
	{
		if (_nativeWebView.CoreWebView2 is not null)
		{
			action();
			return;
		}

		_actions.Add(() =>
		{
			action();
			return Task.CompletedTask;
		});
	}

	private Task<T> ExecuteEnsuringCoreWebView2<T>(Func<Task<T>> task)
	{
		if (_nativeWebView.CoreWebView2 is not null)
		{
			return task();
		}

		var tcs = new TaskCompletionSource<T>();
		_actions.Add(async () =>
		{
			tcs.SetResult(await task());
		});

		return tcs.Task;
	}
}
