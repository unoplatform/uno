#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Webkit;
using Uno.Foundation.Logging;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Document-created scripts, <c>chrome.webview</c> messaging and <c>DOMContentLoaded</c> for the Android WebView,
/// built on <see cref="ScriptBridgeWebViewCapabilities"/>.
/// </summary>
/// <remarks>
/// <para>
/// The platform <c>WebView</c> has no document-start script API (only AndroidX <c>WebViewCompat.addDocumentStartJavaScript</c>,
/// which Uno does not reference), so two modes are combined:
/// <list type="bullet">
/// <item><see cref="WebViewScriptInjectionMode.BeforeParse"/> for documents the wrapper itself hands to the engine:
/// <c>text/html</c> main-frame responses served through <c>WebResourceRequested</c>, and <c>NavigateToString</c> content.
/// The bridge and the user scripts are inserted as <c>&lt;script&gt;</c> elements ahead of the page's own scripts.</item>
/// <item><see cref="WebViewScriptInjectionMode.Late"/> for every other document (pages loaded from the network or from
/// assets): when the page finishes loading, the scripts are evaluated and <c>DOMContentLoaded</c> is raised, right
/// before <c>NavigationCompleted</c>. The page's own scripts have already run by then.</item>
/// </list>
/// <see cref="CompleteNavigationAfterDocumentScripts"/> tells the two apart by probing for the bridge, so a
/// document whose inline scripts were blocked (for instance by a Content-Security-Policy) still gets the late path.
/// Child frames get no scripts. Switching to <c>addDocumentStartJavaScript</c> only needs a
/// <see cref="WebViewScriptInjectionMode.DocumentStart"/> mode and an <c>ApplyDocumentStartScripts</c> override in <c>AndroidScriptBridge</c>.
/// </para>
/// <para>
/// Page to host: the bridge posts through the <c>unoWebView</c> JavaScript interface, prefixing each message with a
/// per-view secret. <c>addJavascriptInterface</c> exposes <c>unoWebView</c> to every frame and origin and reports
/// neither, so the secret, which only the top-level document's bridge holds, is what marks a message as coming from
/// the top-level document (a same-origin child frame can still reach the parent's bridge, as on WebView2). Messages
/// without it come from direct <c>unoWebView.postMessage</c> calls, the documented legacy channel: they are still raised,
/// whatever frame sent them, but can never be taken for the bridge's own lifecycle messages.
/// <c>WebMessageReceived.Source</c> is the WebView's URL when the message is delivered, not the sender's.
/// </para>
/// </remarks>
internal partial class NativeWebViewWrapper : ISupportsDocumentCreatedScripts, ISupportsPostWebMessage, ISupportsDOMContentLoaded
{
	private const string JavascriptInterfaceName = "unoWebView";

	private const string BridgeProbeScript = "(function () { var c = window.chrome, w = c && c.webview; return !!(w && w.__uno); })();";

	private string _bridgeMessagePrefix = null!;
	private AndroidScriptBridge _scripts = null!;

	public event EventHandler? DOMContentLoaded
	{
		add => _scripts.DOMContentLoaded += value;
		remove => _scripts.DOMContentLoaded -= value;
	}

	private void InitializeScripting()
	{
		_bridgeMessagePrefix = "@@uno-webview/top/" + Guid.NewGuid().ToString("N") + "/";
		var channel = "(function (m) { " + JavascriptInterfaceName + ".postMessage(" + WebViewMessageBridge.ToJavaScriptStringLiteral(_bridgeMessagePrefix) + " + m); })";

		_scripts = new AndroidScriptBridge(this, channel);
		_scripts.EnsureDocumentStartScriptsInstalled();

		_webView.AddJavascriptInterface(new UnoWebViewHandler(this), JavascriptInterfaceName);
	}

	public Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		=> _scripts.AddScriptToExecuteOnDocumentCreatedAsync(javaScript, ct);

	public void RemoveScriptToExecuteOnDocumentCreated(string id)
		=> _scripts.RemoveScriptToExecuteOnDocumentCreated(id);

	public void PostWebMessageAsJson(string webMessageAsJson)
		=> _scripts.PostWebMessageAsJson(webMessageAsJson);

	public void PostWebMessageAsString(string webMessageAsString)
		=> _scripts.PostWebMessageAsString(webMessageAsString);

	/// <summary>
	/// <see cref="WebViewScriptInjectionMode.BeforeParse"/>: inserts the bridge and the user scripts into an HTML string.
	/// </summary>
	internal string InjectDocumentScripts(string html) => _scripts.InjectIntoHtml(html);

	/// <summary>
	/// <see cref="WebViewScriptInjectionMode.BeforeParse"/> for an intercepted response body. Runs on the IO thread.
	/// </summary>
	private bool TryInjectDocumentScripts(byte[] body, string? charset, string url, out byte[] injected)
	{
		try
		{
			if (HtmlDocumentScriptInjector.TryInject(body, charset, _scripts.InjectIntoHtml, out injected))
			{
				return true;
			}

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"The encoding of {url} ('{charset}') is not available; document-created scripts run after the page loads.");
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to inject document-created scripts into {url}.", ex);
			}
		}

		injected = [];
		return false;
	}

	/// <summary>
	/// Called on the JavaBridge thread for every <c>unoWebView.postMessage</c> call, from any frame.
	/// </summary>
	internal void OnWebMessageReceived(string? message)
	{
		if (message is null)
		{
			return;
		}

		var isFromTopLevelBridge = message.StartsWith(_bridgeMessagePrefix, StringComparison.Ordinal);
		if (isFromTopLevelBridge)
		{
			message = message.Substring(_bridgeMessagePrefix.Length);
		}
		else if (WebViewMessageBridge.TryParseLifecycle(message, out _))
		{
			// Only the top-level bridge may report lifecycle events.
			return;
		}

		// One queue for messages and lifecycle events keeps their order, and the WebView's URL is read on the UI thread.
		_ = _coreWebView.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			if (isFromTopLevelBridge && _scripts.TryHandleBridgeMessage(message))
			{
				return;
			}

			if (_coreWebView.Settings.IsWebMessageEnabled)
			{
				_coreWebView.RaiseWebMessageReceived(message, _webView.Url);
			}
		});
	}

	/// <summary>
	/// Called on the UI thread when the main frame finished loading: runs the <see cref="WebViewScriptInjectionMode.Late"/>
	/// path if the document did not get the scripts before parsing, then raises <paramref name="navigationCompleted"/>.
	/// </summary>
	internal void CompleteNavigationAfterDocumentScripts(Action navigationCompleted)
		=> _ = CompleteNavigationAfterDocumentScriptsAsync(navigationCompleted);

	private async Task CompleteNavigationAfterDocumentScriptsAsync(Action navigationCompleted)
	{
		try
		{
			if (await EvaluateJavascriptAsync(BridgeProbeScript, CancellationToken.None) != "true")
			{
				await _scripts.OnDocumentReadyAsync(CancellationToken.None);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Failed to run document-created scripts after the page loaded.", ex);
			}
		}

		// Queued behind any DOMContentLoaded the page reported through the JavaBridge thread.
		_ = _coreWebView.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => navigationCompleted());
	}

	private Task<string?> EvaluateJavascriptAsync(string script, CancellationToken ct)
	{
		var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var registration = ct.Register(() => tcs.TrySetCanceled(ct));

		void Evaluate()
		{
			try
			{
				_webView.EvaluateJavascript(script, new ScriptResponse(value =>
				{
					registration.Dispose();
					tcs.TrySetResult(value);
				}));
			}
			catch (Exception ex)
			{
				registration.Dispose();
				tcs.TrySetException(ex);
			}
		}

		var dispatcher = _coreWebView.Owner.Dispatcher;
		if (dispatcher.HasThreadAccess)
		{
			Evaluate();
		}
		else
		{
			_ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, Evaluate);
		}

		return tcs.Task;
	}

	private sealed class AndroidScriptBridge(NativeWebViewWrapper owner, string channel)
		: ScriptBridgeWebViewCapabilities(channel, WebViewScriptInjectionMode.BeforeParse)
	{
		protected override Task<string?> EvaluateScriptAsync(string script, CancellationToken ct)
			=> owner.EvaluateJavascriptAsync(script, ct);
	}
}
