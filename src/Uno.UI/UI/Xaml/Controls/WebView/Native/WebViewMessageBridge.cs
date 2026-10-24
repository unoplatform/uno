#nullable enable

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

internal static class WebViewMessageBridge
{
	internal static string CreateScript(string postMessage) => $$"""
		(function () {
			window.chrome = window.chrome || {};
			if (window.chrome.webview && window.chrome.webview.__unoDispatchMessage) { return; }
			var webview = window.chrome.webview = new EventTarget();
			webview.postMessage = function (message) { {{postMessage}} };
			webview.__unoDispatchMessage = function (data) {
				var event = new MessageEvent('message', { data: data });
				Object.defineProperty(event, 'source', { value: webview });
				window.setTimeout(function () { webview.dispatchEvent(event); }, 0);
			};
		})();
		""";

	internal static void PostMessage(INativeWebView webView, string payload, bool isJson)
	{
		var literal = $"\"{JsonEncodedText.Encode(payload)}\"";
		var data = isJson ? $"JSON.parse({literal})" : literal;
		_ = webView.ExecuteScriptAsync($"window.chrome.webview.__unoDispatchMessage({data});", CancellationToken.None)
			.ContinueWith(
				static (task, state) =>
				{
					var log = state!.Log();
					if (log.IsEnabled(LogLevel.Error))
					{
						log.Error("Unable to post a WebView message.", task.Exception!.GetBaseException());
					}
				},
				webView,
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
	}
}
