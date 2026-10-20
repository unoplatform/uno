#nullable enable

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.MacOS;

/// <summary>
/// Document-created scripts, web messaging and <c>DOMContentLoaded</c> through the shared <c>chrome.webview</c> bridge,
/// and virtual hosts served through a <c>WKURLSchemeHandler</c>.
/// </summary>
internal partial class MacOSNativeWebView : ISupportsDocumentCreatedScripts, ISupportsPostWebMessage, ISupportsDOMContentLoaded, ISupportsCustomSchemeVirtualHosts
{
	private const string PostMessageFunction = "window.webkit.messageHandlers.unoWebView.postMessage";

	// Pending scheme tasks by native id. Not per view: WebKit may stop a task after the view left the handle map.
	private static readonly ConcurrentDictionary<nint, CancellationTokenSource> _schemeTasks = new();

	private ScriptBridge _scriptBridge = null!;
	private CustomSchemeResourceHandler _resourceHandler = null!;

	public event EventHandler? DOMContentLoaded
	{
		add => _scriptBridge.DOMContentLoaded += value;
		remove => _scriptBridge.DOMContentLoaded -= value;
	}

	private void InitializeScripting()
	{
		_scriptBridge = new ScriptBridge(this);
		_resourceHandler = new CustomSchemeResourceHandler(MatchesFilter, args => WebResourceRequested?.Invoke(this, args))
		{
			FolderBasePath = NativeUno.uno_application_is_bundled()
				? Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, "Resources")
				: AppDomain.CurrentDomain.BaseDirectory,
		};

		_scriptBridge.EnsureDocumentStartScriptsInstalled();
	}

	public Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		=> _scriptBridge.AddScriptToExecuteOnDocumentCreatedAsync(javaScript, ct);

	public void RemoveScriptToExecuteOnDocumentCreated(string id)
		=> _scriptBridge.RemoveScriptToExecuteOnDocumentCreated(id);

	public void PostWebMessageAsJson(string webMessageAsJson)
		=> _scriptBridge.PostWebMessageAsJson(webMessageAsJson);

	public void PostWebMessageAsString(string webMessageAsString)
		=> _scriptBridge.PostWebMessageAsString(webMessageAsString);

	public void SetVirtualHostMap(WebViewVirtualHostMap map) => _resourceHandler.Map = map;

	private void OnScriptMessage(string message, string? frameUrl, bool isMainFrame)
	{
		// WebView2 raises CoreWebView2.WebMessageReceived for the top-level document only.
		if (!isMainFrame)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Ignoring a web message from the frame '{frameUrl}'.");
			}

			return;
		}

		if (_scriptBridge.TryHandleBridgeMessage(message))
		{
			return;
		}

		_owner.RaiseWebMessageReceived(message, frameUrl);
	}

	private bool MatchesFilter(string url, CoreWebView2WebResourceContext context)
	{
		foreach (var filter in _webResourceFilters)
		{
			if (filter.MatchesUrl(url) && filter.MatchesContext(context))
			{
				return true;
			}
		}

		return false;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe void SchemeTaskStartCallback(nint handle, nint taskId, sbyte* url, sbyte* method, sbyte* headersJson, byte* body, nint bodyLength, sbyte* mainDocumentUrl)
	{
		try
		{
			var request = new CustomSchemeRequest(
				Marshal.PtrToStringUTF8((IntPtr)url) ?? "",
				Marshal.PtrToStringUTF8((IntPtr)method) ?? "GET",
				ParseRequestHeaders(headersJson == null ? null : Marshal.PtrToStringUTF8((IntPtr)headersJson)),
				body == null || bodyLength <= 0 ? null : new ReadOnlySpan<byte>(body, checked((int)bodyLength)).ToArray(),
				mainDocumentUrl == null ? null : Marshal.PtrToStringUTF8((IntPtr)mainDocumentUrl));

			if (GetWebView(handle) is not { } webview)
			{
				NativeUno.uno_webview_scheme_task_fail(taskId, "The WebView is not available.");
				return;
			}

			var cts = new CancellationTokenSource();
			_schemeTasks[taskId] = cts;
			_ = webview.ServeSchemeTaskAsync(taskId, request, cts);
		}
		catch (Exception e)
		{
			if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSNativeWebView).Log().Error("Failed to start a scheme task.", e);
			}

			NativeUno.uno_webview_scheme_task_fail(taskId, e.Message);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void SchemeTaskStopCallback(nint handle, nint taskId)
	{
		try
		{
			if (_schemeTasks.TryRemove(taskId, out var cts))
			{
				cts.Cancel();
			}
		}
		catch (Exception e)
		{
			if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSNativeWebView).Log().Error("Failed to stop a scheme task.", e);
			}
		}
	}

	private async Task ServeSchemeTaskAsync(nint taskId, CustomSchemeRequest request, CancellationTokenSource cts)
	{
		try
		{
			var response = await _resourceHandler.HandleAsync(request, cts.Token);

			// A stopped task is gone from the map; native code ignores it too, this only saves the copy.
			if (_schemeTasks.TryRemove(taskId, out _))
			{
				CompleteSchemeTask(taskId, response);
			}
		}
		catch (OperationCanceledException) when (cts.IsCancellationRequested)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"The request for '{request.Uri}' was stopped.");
			}
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to serve '{request.Uri}'.", e);
			}

			_schemeTasks.TryRemove(taskId, out _);
			NativeUno.uno_webview_scheme_task_fail(taskId, e.Message);
		}

		// The token source is not disposed: a concurrent stop may still cancel it, and it owns no timer.
	}

	private static unsafe void CompleteSchemeTask(nint taskId, WebResourceResponseSnapshot response)
	{
		var headers = SerializeResponseHeaders(response.Headers);
		fixed (byte* body = response.Body)
		{
			NativeUno.uno_webview_scheme_task_complete(taskId, response.StatusCode, headers, body, response.Body.Length);
		}
	}

	internal static IReadOnlyList<KeyValuePair<string, string>> ParseRequestHeaders(string? json)
	{
		var headers = new List<KeyValuePair<string, string>>();
		if (string.IsNullOrEmpty(json))
		{
			return headers;
		}

		using var document = JsonDocument.Parse(json);
		if (document.RootElement.ValueKind == JsonValueKind.Object)
		{
			foreach (var property in document.RootElement.EnumerateObject())
			{
				headers.Add(new(property.Name, property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? "" : property.Value.ToString()));
			}
		}

		return headers;
	}

	// [["name","value"],...]: keeps repeated names, which a JSON object could not.
	internal static string SerializeResponseHeaders(IReadOnlyList<KeyValuePair<string, string>> headers)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			writer.WriteStartArray();
			foreach (var header in headers)
			{
				writer.WriteStartArray();
				writer.WriteStringValue(header.Key);
				writer.WriteStringValue(header.Value);
				writer.WriteEndArray();
			}
			writer.WriteEndArray();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private static string SerializeScripts(IReadOnlyList<string> scripts)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			writer.WriteStartArray();
			foreach (var script in scripts)
			{
				writer.WriteStringValue(script);
			}
			writer.WriteEndArray();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private sealed class ScriptBridge(MacOSNativeWebView owner) : ScriptBridgeWebViewCapabilities(PostMessageFunction, WebViewScriptInjectionMode.DocumentStart)
	{
		protected override Task<string?> EvaluateScriptAsync(string script, CancellationToken ct)
			=> owner.ExecuteScriptAsync(script, ct);

		protected override void ApplyDocumentStartScripts(IReadOnlyList<string> scripts)
		{
			if (owner.TryGetHandle("set the document scripts of", out var handle))
			{
				NativeUno.uno_webview_set_user_scripts(handle, SerializeScripts(scripts));
			}
		}
	}
}
