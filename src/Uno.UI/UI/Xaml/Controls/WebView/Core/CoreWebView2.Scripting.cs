#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	private const string AddScriptToExecuteOnDocumentCreatedMemberName = "AddScriptToExecuteOnDocumentCreatedAsync(string javaScript)";
	private const string RemoveScriptToExecuteOnDocumentCreatedMemberName = "RemoveScriptToExecuteOnDocumentCreated(string id)";
	private const string PostWebMessageAsJsonMemberName = "PostWebMessageAsJson(string webMessageAsJson)";
	private const string PostWebMessageAsStringMemberName = "PostWebMessageAsString(string webMessageAsString)";
	private const string DOMContentLoadedMemberName = "event DOMContentLoaded";

	private readonly List<PendingDocumentCreatedScript> _pendingDocumentCreatedScripts = new();
	private ISupportsDOMContentLoaded? _domContentLoadedSupport;
	private TypedEventHandler<CoreWebView2, CoreWebView2DOMContentLoadedEventArgs>? _domContentLoaded;

	/// <summary>
	/// DOMContentLoaded is raised when the initial HTML document has been parsed.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2DOMContentLoadedEventArgs> DOMContentLoaded
	{
		add
		{
			// Keeps the generated stub's diagnostic on platforms that cannot raise the event.
			if (_nativeWebView is not null and not ISupportsDOMContentLoaded)
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(CoreWebView2TypeName, DOMContentLoadedMemberName);
			}

			_domContentLoaded += value;
		}
		remove => _domContentLoaded -= value;
	}

	/// <summary>
	/// Adds the provided JavaScript to a list of scripts that should be run after the global object has been created,
	/// but before the HTML document has been parsed and before any other script included by the HTML document is run.
	/// </summary>
	/// <returns>The ID of the script, to pass to <see cref="RemoveScriptToExecuteOnDocumentCreated(string)"/>.</returns>
	/// <remarks>
	/// Before the native WebView exists the script is queued, and the operation completes once the native WebView
	/// has registered it, ahead of its first navigation.
	/// </remarks>
	public IAsyncOperation<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript)
	{
		if (javaScript is null)
		{
			throw new ArgumentNullException(nameof(javaScript));
		}

		if (_nativeWebView is null)
		{
			var pending = new PendingDocumentCreatedScript(javaScript);
			_pendingDocumentCreatedScripts.Add(pending);
			return AsyncOperation.FromTask(ct => WaitForPendingScriptAsync(pending, ct));
		}

		var native = RequireCapability<ISupportsDocumentCreatedScripts>(CoreWebView2TypeName, AddScriptToExecuteOnDocumentCreatedMemberName);
		return AsyncOperation.FromTask(ct => native.AddScriptToExecuteOnDocumentCreatedAsync(javaScript, ct));
	}

	/// <summary>
	/// Removes the script corresponding to the specified ID from the list of scripts run on document creation.
	/// </summary>
	public void RemoveScriptToExecuteOnDocumentCreated(string id)
	{
		if (_nativeWebView is ISupportsDocumentCreatedScripts native)
		{
			native.RemoveScriptToExecuteOnDocumentCreated(id);
		}
		else
		{
			ReportUnavailable(RemoveScriptToExecuteOnDocumentCreatedMemberName);
		}
	}

	/// <summary>
	/// Posts the specified JSON message to the top level document in this WebView.
	/// </summary>
	public void PostWebMessageAsJson(string webMessageAsJson)
	{
		if (_nativeWebView is ISupportsPostWebMessage native)
		{
			native.PostWebMessageAsJson(webMessageAsJson);
		}
		else
		{
			ReportUnavailable(PostWebMessageAsJsonMemberName);
		}
	}

	/// <summary>
	/// Posts a message that is a simple string rather than a JSON string representation of a JavaScript object.
	/// </summary>
	public void PostWebMessageAsString(string webMessageAsString)
	{
		if (_nativeWebView is ISupportsPostWebMessage native)
		{
			native.PostWebMessageAsString(webMessageAsString);
		}
		else
		{
			ReportUnavailable(PostWebMessageAsStringMemberName);
		}
	}

	internal void RaiseDOMContentLoaded()
	{
		var args = new CoreWebView2DOMContentLoadedEventArgs((ulong)Interlocked.Read(ref _navigationId));

		if (_owner.Dispatcher.HasThreadAccess)
		{
			_domContentLoaded?.Invoke(this, args);
		}
		else
		{
			_ = _owner.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => _domContentLoaded?.Invoke(this, args));
		}
	}

	/// <summary>
	/// Mirrors the generated stub (a not-implemented report) when the platform lacks the capability; before the
	/// native WebView exists there is no document to act on, so the call is dropped.
	/// </summary>
	private void ReportUnavailable(string memberName)
	{
		if (_nativeWebView is null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"{CoreWebView2TypeName}.{memberName} was called before the native WebView was initialized and is ignored.");
			}
		}
		else
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(CoreWebView2TypeName, memberName);
		}
	}

	private void AttachDOMContentLoadedSupport()
	{
		if (_nativeWebView is ISupportsDOMContentLoaded supports)
		{
			_domContentLoadedSupport = supports;
			supports.DOMContentLoaded += OnNativeDOMContentLoaded;
		}
	}

	private void DetachDOMContentLoadedSupport()
	{
		if (_domContentLoadedSupport is { })
		{
			_domContentLoadedSupport.DOMContentLoaded -= OnNativeDOMContentLoaded;
			_domContentLoadedSupport = null;
		}
	}

	private void OnNativeDOMContentLoaded(object? sender, EventArgs e) => RaiseDOMContentLoaded();

	private void ApplyPendingDocumentCreatedScripts()
	{
		if (_nativeWebView is null || _pendingDocumentCreatedScripts.Count == 0)
		{
			return;
		}

		var pendingScripts = _pendingDocumentCreatedScripts.ToArray();
		_pendingDocumentCreatedScripts.Clear();

		foreach (var pending in pendingScripts)
		{
			if (pending.Completion.Task.IsCompleted)
			{
				continue;
			}

			if (_nativeWebView is ISupportsDocumentCreatedScripts native)
			{
				pending.IsForwarded = true;
				_ = ForwardPendingScriptAsync(native, pending);
			}
			else
			{
				pending.Completion.TrySetException(CapabilityUnavailable(CoreWebView2TypeName, AddScriptToExecuteOnDocumentCreatedMemberName));
			}
		}
	}

	private static async Task ForwardPendingScriptAsync(ISupportsDocumentCreatedScripts native, PendingDocumentCreatedScript pending)
	{
		try
		{
			pending.Completion.TrySetResult(await native.AddScriptToExecuteOnDocumentCreatedAsync(pending.Script, CancellationToken.None));
		}
		catch (Exception e)
		{
			pending.Completion.TrySetException(e);
		}
	}

	private async Task<string> WaitForPendingScriptAsync(PendingDocumentCreatedScript pending, CancellationToken ct)
	{
		// Cancelling withdraws a script that is still queued (ApplyPendingDocumentCreatedScripts skips it); once
		// forwarded, the native registration stands and the operation reports its id.
		using var registration = ct.Register(() =>
		{
			if (!pending.IsForwarded)
			{
				pending.Completion.TrySetCanceled(ct);
			}
		});

		return await pending.Completion.Task;
	}

	private sealed class PendingDocumentCreatedScript(string script)
	{
		public string Script { get; } = script;

		public bool IsForwarded { get; set; }

		public TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}
