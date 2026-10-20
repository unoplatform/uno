#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// How a <see cref="ScriptBridgeWebViewCapabilities"/> gets its document-start scripts into a page.
/// </summary>
internal enum WebViewScriptInjectionMode
{
	/// <summary>
	/// The native view registers scripts that run at document start in every frame and navigation
	/// (<c>WKUserScript</c> at <c>AtDocumentStart</c>, AndroidX <c>addDocumentStartJavaScript</c>, WebKitGTK user scripts).
	/// </summary>
	DocumentStart,

	/// <summary>
	/// The native view cannot register scripts but controls the HTML it loads: it calls
	/// <see cref="ScriptBridgeWebViewCapabilities.InjectIntoHtml"/> before handing a document to the parser.
	/// </summary>
	BeforeParse,

	/// <summary>
	/// Last resort: the native view calls <see cref="ScriptBridgeWebViewCapabilities.OnDocumentReadyAsync"/> once the
	/// document is parsed (or the navigation completed), and the scripts run then, after the page's own scripts.
	/// </summary>
	Late,
}

/// <summary>
/// Implements document-created scripts, <c>PostWebMessage*</c> and <c>DOMContentLoaded</c> on top of
/// two primitives a native web view without <c>chrome.webview</c> can offer: document-start scripts and script evaluation.
/// </summary>
/// <remarks>
/// <para>
/// A native view derives from this class (or holds a small subclass and forwards the three capability interfaces to it).
/// The <see cref="WebViewMessageBridge"/> script is always the first document-start script, followed by the user's
/// scripts in the order they were added.
/// </para>
/// <para>
/// The native view must: call <see cref="EnsureDocumentStartScriptsInstalled"/> before its first navigation; route
/// every message received on the outbound channel through <see cref="TryHandleBridgeMessage"/> and raise
/// <c>WebMessageReceived</c> only for the messages it returns false for; and, in
/// <see cref="WebViewScriptInjectionMode.Late"/> mode, await <see cref="OnDocumentReadyAsync"/> before reporting
/// <c>NavigationCompleted</c>.
/// </para>
/// </remarks>
internal abstract partial class ScriptBridgeWebViewCapabilities : ISupportsDocumentCreatedScripts, ISupportsPostWebMessage, ISupportsDOMContentLoaded
{
	private readonly object _gate = new();
	private readonly List<KeyValuePair<string, string>> _userScripts = new();
	private int _lastScriptId;
	private bool _isInstalled;

	/// <param name="postMessageFunction">The outbound channel; see <see cref="WebViewMessageBridge.CreateScript"/>.</param>
	protected ScriptBridgeWebViewCapabilities(string postMessageFunction, WebViewScriptInjectionMode mode)
	{
		Mode = mode;

		// In Late mode the document has already been parsed when the bridge arrives, so OnDocumentReadyAsync raises
		// DOMContentLoaded instead.
		BridgeScript = WebViewMessageBridge.CreateScript(postMessageFunction, reportLifecycle: mode != WebViewScriptInjectionMode.Late);
	}

	public event EventHandler? DOMContentLoaded;

	public WebViewScriptInjectionMode Mode { get; }

	public string BridgeScript { get; }

	/// <summary>
	/// Evaluates <paramref name="script"/> in the top-level document, like <c>ExecuteScriptAsync</c>.
	/// </summary>
	protected abstract Task<string?> EvaluateScriptAsync(string script, CancellationToken ct);

	/// <summary>
	/// Replaces the native view's document-start scripts with <paramref name="scripts"/>, in order.
	/// </summary>
	/// <remarks>
	/// Required in <see cref="WebViewScriptInjectionMode.DocumentStart"/> mode. It is called synchronously from
	/// <see cref="AddScriptToExecuteOnDocumentCreatedAsync"/>, so a navigation started right after already runs the
	/// new set. Replacing the whole set (rather than adding and removing one script) keeps the order intact and maps
	/// onto <c>WKUserContentController.RemoveAllUserScripts</c>, which cannot remove a single script.
	/// </remarks>
	protected virtual void ApplyDocumentStartScripts(IReadOnlyList<string> scripts)
		=> throw new InvalidOperationException($"{GetType().Name} must override {nameof(ApplyDocumentStartScripts)} in {WebViewScriptInjectionMode.DocumentStart} mode.");

	/// <summary>
	/// The bridge followed by the user scripts, in the order they must run.
	/// </summary>
	public IReadOnlyList<string> GetDocumentStartScripts()
	{
		lock (_gate)
		{
			var scripts = new List<string>(_userScripts.Count + 1) { BridgeScript };
			foreach (var script in _userScripts)
			{
				scripts.Add(script.Value);
			}

			return scripts;
		}
	}

	/// <summary>
	/// Registers the bridge (and any queued user scripts) with the native view. Idempotent.
	/// </summary>
	public void EnsureDocumentStartScriptsInstalled()
	{
		lock (_gate)
		{
			if (_isInstalled)
			{
				return;
			}
		}

		ApplyIfDocumentStart();
	}

	public Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(javaScript);

		string id;
		lock (_gate)
		{
			id = (++_lastScriptId).ToString(CultureInfo.InvariantCulture);
			_userScripts.Add(new(id, javaScript));
		}

		ApplyIfDocumentStart();
		return Task.FromResult(id);
	}

	public void RemoveScriptToExecuteOnDocumentCreated(string id)
	{
		bool removed;
		lock (_gate)
		{
			removed = _userScripts.RemoveAll(s => s.Key == id) > 0;
		}

		if (removed)
		{
			ApplyIfDocumentStart();
		}
	}

	public void PostWebMessageAsString(string webMessageAsString)
	{
		ArgumentNullException.ThrowIfNull(webMessageAsString);

		Dispatch(WebViewMessageBridge.CreateDispatchScript(isJson: false, webMessageAsString));
	}

	/// <exception cref="ArgumentException"><paramref name="webMessageAsJson"/> is not valid JSON, as WebView2 reports.</exception>
	public void PostWebMessageAsJson(string webMessageAsJson)
	{
		ArgumentNullException.ThrowIfNull(webMessageAsJson);

		try
		{
			using var _ = JsonDocument.Parse(webMessageAsJson);
		}
		catch (JsonException e)
		{
			throw new ArgumentException("The message is not valid JSON.", nameof(webMessageAsJson), e);
		}

		Dispatch(WebViewMessageBridge.CreateDispatchScript(isJson: true, webMessageAsJson));
	}

	/// <summary>
	/// Handles the bridge's reserved messages.
	/// </summary>
	/// <returns>true when the message was reserved; the native view must then not raise it as a web message.</returns>
	public bool TryHandleBridgeMessage(string? message)
	{
		if (!WebViewMessageBridge.TryParseLifecycle(message, out var kind))
		{
			return false;
		}

		if (kind == WebViewBridgeLifecycleEvent.DOMContentLoaded)
		{
			RaiseDOMContentLoaded();
		}

		return true;
	}

	/// <summary>
	/// <see cref="WebViewScriptInjectionMode.Late"/> mode: runs the document-start scripts in the parsed document, in
	/// order, then raises <see cref="DOMContentLoaded"/>. A failing script does not stop the next ones.
	/// </summary>
	public async Task OnDocumentReadyAsync(CancellationToken ct)
	{
		foreach (var script in GetDocumentStartScripts())
		{
			try
			{
				await EvaluateScriptAsync(script, ct);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("A document-created script failed in late injection.", e);
				}
			}
		}

		RaiseDOMContentLoaded();
	}

	/// <summary>
	/// <see cref="WebViewScriptInjectionMode.BeforeParse"/> mode: inserts the document-start scripts, one
	/// <c>&lt;script&gt;</c> element each, ahead of the page's content.
	/// </summary>
	/// <remarks>
	/// Pass decoded text: the scripts go right after the <c>&lt;head&gt;</c> start tag (else after <c>&lt;html&gt;</c>,
	/// else after the doctype, else at the start; before the first <c>&lt;script&gt;</c> if one comes earlier), which can push a late <c>&lt;meta charset&gt;</c> past the
	/// 1024-byte prescan of a byte stream. Separate elements keep a throwing script from stopping the next one and keep
	/// top-level declarations global, as on WebView2.
	/// </remarks>
	public string InjectIntoHtml(string html)
	{
		ArgumentNullException.ThrowIfNull(html);

		var scripts = new StringBuilder();
		foreach (var script in GetDocumentStartScripts())
		{
			scripts.Append("<script>").Append(EscapeForInlineScript(script)).Append("</script>");
		}

		return html.Insert(FindInsertionIndex(html), scripts.ToString());
	}

	protected void RaiseDOMContentLoaded() => DOMContentLoaded?.Invoke(this, EventArgs.Empty);

	/// <summary>
	/// Makes script text safe inside an inline <c>&lt;script&gt;</c> element.
	/// </summary>
	/// <remarks>
	/// <c>&lt;/script</c> and <c>&lt;!--</c> can only occur in JavaScript inside strings, template literals, regular
	/// expressions or comments, where <c>&lt;\/script</c> and <c>&lt;\!--</c> mean the same thing.
	/// </remarks>
	internal static string EscapeForInlineScript(string script)
		=> InlineScriptBreakers().Replace(script, m => m.Value[0] == '/' ? "\\" + m.Value : "<\\!--");

	private static int FindInsertionIndex(string html)
	{
		var index = 0;
		foreach (var regex in new[] { HeadStartTag(), HtmlStartTag(), Doctype() })
		{
			if (regex.Match(html) is { Success: true } match)
			{
				index = match.Index + match.Length;
				break;
			}
		}

		// A page script ahead of <head> (or of <html> when there is no head) must still run after the injected ones.
		var firstScript = ScriptStartTag().Match(html, 0, index);
		return firstScript.Success ? firstScript.Index : index;
	}

	private void ApplyIfDocumentStart()
	{
		lock (_gate)
		{
			_isInstalled = true;
		}

		if (Mode == WebViewScriptInjectionMode.DocumentStart)
		{
			ApplyDocumentStartScripts(GetDocumentStartScripts());
		}
	}

	private void Dispatch(string script) => _ = DispatchAsync(script);

	private async Task DispatchAsync(string script)
	{
		try
		{
			await EvaluateScriptAsync(script, CancellationToken.None);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Failed to deliver a web message to the page.", e);
			}
		}
	}

	// Matches the "/script" of "</script" (so the replacement keeps the "<") and "<!--".
	[GeneratedRegex(@"(?<=<)/script|<!--", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex InlineScriptBreakers();

	[GeneratedRegex(@"<head(?=[\s/>])[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex HeadStartTag();

	[GeneratedRegex(@"<html(?=[\s/>])[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex HtmlStartTag();

	[GeneratedRegex(@"^\uFEFF?\s*<!doctype[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex Doctype();

	[GeneratedRegex(@"<script(?=[\s/>])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex ScriptStartTag();
}
