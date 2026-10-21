#if NET10_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;

using DirectN;

using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Win32;

/// <remarks>
/// Every member here is affine to the thread owning the WebView2 HWND, like the rest of this class.
/// </remarks>
internal sealed partial class Win32NativeAotWebView : ISupportsDocumentCreatedScripts, ISupportsPostWebMessage, ISupportsDOMContentLoaded
{
	public event EventHandler? DOMContentLoaded;

	private void SubscribeToDocumentEvents(WeakReference<Win32NativeAotWebView> weakRef)
	{
		var eventToken = new WebView2.EventRegistrationToken();
		_nativeWebView.add_DOMContentLoaded(
			new WebView2.Utilities.CoreWebView2DOMContentLoadedEventHandler((_, _) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.DOMContentLoaded?.Invoke(target, EventArgs.Empty);
			}),
			ref eventToken
		).ThrowOnError();
	}

	public unsafe Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
	{
		var tcs = new TaskCompletionSource<string>();
		fixed (char* p_javaScript = javaScript)
		{
			_nativeWebView.AddScriptToExecuteOnDocumentCreated(
				new PWSTR(p_javaScript),
				new WebView2.Utilities.CoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler((errorCode, id) =>
				{
					if (errorCode.IsError)
					{
						tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("AddScriptToExecuteOnDocumentCreated failed."));
					}
					else
					{
						// The id is owned by the caller of the completion handler, so it is copied, not freed.
						tcs.TrySetResult(id.ToString() ?? string.Empty);
					}
				})
			).ThrowOnError();
		}

		return tcs.Task;
	}

	public unsafe void RemoveScriptToExecuteOnDocumentCreated(string id)
	{
		fixed (char* p_id = id)
			_nativeWebView.RemoveScriptToExecuteOnDocumentCreated(new PWSTR(p_id)).ThrowOnError();
	}

	public unsafe void PostWebMessageAsJson(string webMessageAsJson)
	{
		fixed (char* p_webMessageAsJson = webMessageAsJson)
			_nativeWebView.PostWebMessageAsJson(new PWSTR(p_webMessageAsJson)).ThrowOnError();
	}

	public unsafe void PostWebMessageAsString(string webMessageAsString)
	{
		fixed (char* p_webMessageAsString = webMessageAsString)
			_nativeWebView.PostWebMessageAsString(new PWSTR(p_webMessageAsString)).ThrowOnError();
	}
}

#endif
