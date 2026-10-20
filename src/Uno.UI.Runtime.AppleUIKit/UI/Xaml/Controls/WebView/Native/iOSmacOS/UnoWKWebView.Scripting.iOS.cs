#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using WebKit;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Document-created scripts, web messaging and <c>DOMContentLoaded</c> through the shared <c>chrome.webview</c> bridge.
/// </summary>
partial class UnoWKWebView : ISupportsDocumentCreatedScripts, ISupportsPostWebMessage, ISupportsDOMContentLoaded
{
	private const string PostMessageFunction = "window.webkit.messageHandlers." + WebMessageHandlerName + ".postMessage";

	private ScriptBridge? _scriptBridge;

	private ScriptBridge Bridge => _scriptBridge ??= new ScriptBridge(this);

	public event EventHandler? DOMContentLoaded
	{
		add => Bridge.DOMContentLoaded += value;
		remove => Bridge.DOMContentLoaded -= value;
	}

	public Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		=> Bridge.AddScriptToExecuteOnDocumentCreatedAsync(javaScript, ct);

	public void RemoveScriptToExecuteOnDocumentCreated(string id)
		=> Bridge.RemoveScriptToExecuteOnDocumentCreated(id);

	public void PostWebMessageAsJson(string webMessageAsJson)
		=> Bridge.PostWebMessageAsJson(webMessageAsJson);

	public void PostWebMessageAsString(string webMessageAsString)
		=> Bridge.PostWebMessageAsString(webMessageAsString);

	public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
	{
		if (message.Name != WebMessageHandlerName || message.Body is not NSString body)
		{
			return;
		}

		var frame = message.FrameInfo;

		// WebView2 raises CoreWebView2.WebMessageReceived for the top-level document only.
		if (frame is { MainFrame: false })
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Ignoring a web message from the frame '{frame.Request?.Url?.AbsoluteString}'.");
			}

			return;
		}

		var text = body.ToString();
		if (Bridge.TryHandleBridgeMessage(text))
		{
			return;
		}

		_coreWebView?.RaiseWebMessageReceived(text, frame?.Request?.Url?.AbsoluteString);
	}

	private sealed class ScriptBridge(UnoWKWebView owner) : ScriptBridgeWebViewCapabilities(PostMessageFunction, WebViewScriptInjectionMode.DocumentStart)
	{
		protected override Task<string?> EvaluateScriptAsync(string script, CancellationToken ct)
			=> ((INativeWebView)owner).ExecuteScriptAsync(script, ct);

		protected override void ApplyDocumentStartScripts(IReadOnlyList<string> scripts)
		{
			var controller = owner.Configuration.UserContentController;
			controller.RemoveAllUserScripts();

			foreach (var script in scripts)
			{
				// All frames, like WebView2's AddScriptToExecuteOnDocumentCreated.
				controller.AddUserScript(new WKUserScript(new NSString(script), WKUserScriptInjectionTime.AtDocumentStart, isForMainFrameOnly: false));
			}
		}
	}
}
