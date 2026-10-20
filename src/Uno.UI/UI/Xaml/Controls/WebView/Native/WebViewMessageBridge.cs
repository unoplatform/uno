#nullable enable

using System;
using System.Globalization;
using System.Text;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Lifecycle notifications the <see cref="WebViewMessageBridge"/> script reports through the page's outbound channel.
/// </summary>
internal enum WebViewBridgeLifecycleEvent
{
	/// <summary>
	/// A reserved envelope this version does not know. It must still be swallowed, never surfaced as a web message.
	/// </summary>
	Unknown,

	/// <summary>
	/// The top-level document has been parsed.
	/// </summary>
	DOMContentLoaded,
}

/// <summary>
/// Generates the JavaScript that gives a page <c>window.chrome.webview</c> on native web views that lack it
/// (WebKit, Android WebView, WebKitGTK).
/// </summary>
/// <remarks>
/// <para>
/// Page to host: <c>chrome.webview.postMessage(value)</c> sends <c>JSON.stringify(value)</c> through the outbound
/// channel, so the host receives text it passes unchanged to <c>CoreWebView2.RaiseWebMessageReceived</c> as
/// <c>WebMessageAsJson</c> (a string stays a string: <c>TryGetWebMessageAsString</c> returns it).
/// </para>
/// <para>
/// Host to page: <see cref="CreateDispatchScript"/> builds a call to <c>window.chrome.webview.__uno.dispatch(isJson, payload)</c>,
/// which delivers a <c>MessageEvent</c> whose <c>data</c> is the string, or the parsed JSON value, to the
/// <c>message</c> listeners.
/// </para>
/// <para>
/// Lifecycle notifications use the reserved <see cref="LifecyclePrefix"/>. No <c>JSON.stringify</c> output starts
/// with <c>@</c>, so a page message can never be mistaken for one. Platforms must pass every inbound message through
/// <see cref="TryParseLifecycle"/> first and drop the reserved ones.
/// </para>
/// </remarks>
internal static class WebViewMessageBridge
{
	/// <summary>
	/// Prefix of the reserved messages the bridge sends on its own behalf.
	/// </summary>
	internal const string LifecyclePrefix = "@@uno-webview/lifecycle/";

	private const string PostMessageToken = "__UNO_POST__";
	private const string ReportLifecycleToken = "__UNO_REPORT_LIFECYCLE__";
	private const string LifecyclePrefixToken = "__UNO_LIFECYCLE_PREFIX__";

	// Kept as ES5 so it runs on old WebKit and Android WebView builds. It is idempotent: a second injection in the same
	// document (frames, late injection, re-registration) sees chrome.webview and stops. It never replaces an existing
	// chrome.webview and gives up when window.chrome cannot be extended.
	private const string BridgeTemplate =
		"""
		(function () {
			'use strict';
			var w = window;
			var c = w.chrome;
			if (c === undefined || c === null) {
				c = {};
				try { w.chrome = c; } catch (e) { return; }
				if (w.chrome !== c) { return; }
			} else if (typeof c !== 'object' && typeof c !== 'function') {
				return;
			}
			if (c.webview !== undefined && c.webview !== null) {
				return;
			}
			var send = function (m) { __UNO_POST__(m); };
			var listeners = [];
			var indexOf = function (l) {
				for (var i = 0; i < listeners.length; i++) {
					if (listeners[i] === l) { return i; }
				}
				return -1;
			};
			var webview = {
				postMessage: function (value) {
					var json = JSON.stringify(value);
					send(json === undefined ? 'null' : json);
				},
				addEventListener: function (type, listener) {
					if (type === 'message' && listener && indexOf(listener) < 0) {
						listeners.push(listener);
					}
				},
				removeEventListener: function (type, listener) {
					var i = type === 'message' ? indexOf(listener) : -1;
					if (i >= 0) {
						listeners.splice(i, 1);
					}
				}
			};
			var createEvent = function (data) {
				if (typeof MessageEvent === 'function') {
					try { return new MessageEvent('message', { data: data }); } catch (e) { }
				}
				return { type: 'message', data: data };
			};
			var dispatch = function (isJson, payload) {
				var event = createEvent(isJson ? JSON.parse(payload) : payload);
				var current = listeners.slice();
				for (var i = 0; i < current.length; i++) {
					var l = current[i];
					try {
						if (typeof l === 'function') {
							l.call(webview, event);
						} else if (typeof l.handleEvent === 'function') {
							l.handleEvent(event);
						}
					} catch (e) {
						setTimeout(function () { throw e; }, 0);
					}
				}
			};
			Object.defineProperty(webview, '__uno', { value: { dispatch: dispatch, version: 1 } });
			try { c.webview = webview; } catch (e) { return; }
			if (c.webview !== webview) { return; }
			if (__UNO_REPORT_LIFECYCLE__ && w.top === w && typeof document !== 'undefined' && document.readyState === 'loading') {
				var reported = false;
				document.addEventListener('DOMContentLoaded', function () {
					if (!reported) {
						reported = true;
						send(__UNO_LIFECYCLE_PREFIX__ + 'DOMContentLoaded');
					}
				});
			}
		})();
		""";

	/// <summary>
	/// Creates the bridge script.
	/// </summary>
	/// <param name="postMessageFunction">
	/// A JavaScript callee expression the bridge invokes as <c>&lt;postMessageFunction&gt;(message)</c> with one string.
	/// It is inserted verbatim, so member expressions keep their receiver: <c>window.webkit.messageHandlers.unoWebView.postMessage</c>
	/// (WebKit), <c>unoWebView.postMessage</c> (Android <c>addJavascriptInterface</c>), or a wrapper such as
	/// <c>(function (m) { window.parent.postMessage(m, '*'); })</c> (WASM iframe).
	/// </param>
	/// <param name="reportLifecycle">
	/// Whether the top-level document reports DOMContentLoaded through the channel. It only fires when the bridge is
	/// installed while the document is still loading.
	/// </param>
	public static string CreateScript(string postMessageFunction, bool reportLifecycle)
	{
		if (string.IsNullOrWhiteSpace(postMessageFunction))
		{
			throw new ArgumentException("A JavaScript function expression is required.", nameof(postMessageFunction));
		}

		return BridgeTemplate
			.Replace(PostMessageToken, postMessageFunction.Trim(), StringComparison.Ordinal)
			.Replace(ReportLifecycleToken, reportLifecycle ? "true" : "false", StringComparison.Ordinal)
			.Replace(LifecyclePrefixToken, ToJavaScriptStringLiteral(LifecyclePrefix), StringComparison.Ordinal);
	}

	/// <summary>
	/// Creates the script that delivers a host message to the page's <c>message</c> listeners.
	/// </summary>
	/// <param name="isJson">true when <paramref name="payload"/> is JSON text to parse; false for a plain string.</param>
	/// <remarks>
	/// The payload always travels as a string literal, JSON included, so the page parses it with <c>JSON.parse</c>
	/// rather than evaluating it as code. The script is a no-op when the bridge is missing.
	/// </remarks>
	public static string CreateDispatchScript(bool isJson, string payload)
	{
		ArgumentNullException.ThrowIfNull(payload);

		return "(function () { var c = window.chrome, w = c && c.webview, u = w && w.__uno; " +
			"if (u && typeof u.dispatch === 'function') { u.dispatch(" + (isJson ? "true" : "false") + ", " +
			ToJavaScriptStringLiteral(payload) + "); } })();";
	}

	/// <summary>
	/// Recognizes the bridge's reserved lifecycle messages.
	/// </summary>
	/// <returns>true when <paramref name="message"/> is reserved and must not be raised as a web message.</returns>
	public static bool TryParseLifecycle(string? message, out WebViewBridgeLifecycleEvent kind)
	{
		if (message is null || !message.StartsWith(LifecyclePrefix, StringComparison.Ordinal))
		{
			kind = default;
			return false;
		}

		kind = message.AsSpan(LifecyclePrefix.Length).SequenceEqual(nameof(WebViewBridgeLifecycleEvent.DOMContentLoaded))
			? WebViewBridgeLifecycleEvent.DOMContentLoaded
			: WebViewBridgeLifecycleEvent.Unknown;
		return true;
	}

	/// <summary>
	/// Encodes <paramref name="value"/> as a double-quoted JavaScript string literal that is also valid JSON.
	/// </summary>
	/// <remarks>
	/// Every UTF-16 code unit outside printable ASCII is escaped as <c>\uXXXX</c>: U+2028/U+2029 (line terminators
	/// in older engines), lone surrogates (preserved, not replaced) and pairs (round trip as two units).
	/// <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> and <c>'</c> are escaped too, so the literal is safe inside an inline
	/// <c>&lt;script&gt;</c> (no <c>&lt;/script&gt;</c> or <c>&lt;!--</c>) and in single-quoted contexts.
	/// </remarks>
	public static string ToJavaScriptStringLiteral(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var builder = new StringBuilder(value.Length + 2);
		builder.Append('"');

		foreach (var ch in value)
		{
			switch (ch)
			{
				case '"':
					builder.Append("\\\"");
					break;
				case '\\':
					builder.Append("\\\\");
					break;
				case '\n':
					builder.Append("\\n");
					break;
				case '\r':
					builder.Append("\\r");
					break;
				case '\t':
					builder.Append("\\t");
					break;
				case '<' or '>' or '&' or '\'':
				case < ' ' or >= '\u007F':
					builder.Append("\\u").Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
					break;
				default:
					builder.Append(ch);
					break;
			}
		}

		builder.Append('"');
		return builder.ToString();
	}
}
