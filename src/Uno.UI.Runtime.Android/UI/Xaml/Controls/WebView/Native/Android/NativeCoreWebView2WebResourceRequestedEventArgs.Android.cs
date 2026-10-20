#if __ANDROID__ || ANDROID_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// Android-specific implementation for WebResourceRequested event args.
/// </summary>
/// <remarks>
/// The event is raised on the UI thread while the WebView's IO thread waits in <see cref="WaitForCompletion"/>: the
/// request completes once the handlers have returned and every deferral has been completed.
/// </remarks>
internal partial class NativeCoreWebView2WebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private readonly IWebResourceRequest _nativeRequest;
	private readonly ManualResetEventSlim _completed = new(false);
	private NativeCoreWebView2WebResourceRequest? _request;
	private INativeWebResourceResponse? _response;

	// One for the handlers still running, plus one per deferral not yet completed.
	private int _pendingOperations = 1;

	internal NativeCoreWebView2WebResourceRequestedEventArgs(
		IWebResourceRequest nativeRequest,
		CoreWebView2WebResourceContext resourceContext)
	{
		_nativeRequest = nativeRequest;
		_resourceContext = resourceContext;
	}

	public INativeWebResourceRequest Request
		=> _request ??= new NativeCoreWebView2WebResourceRequest(_nativeRequest);

	/// <summary>
	/// Any response: <see cref="NativeCoreWebView2WebResourceResponse"/> or the managed one that
	/// <c>CoreWebView2Environment.CreateWebResourceResponse</c> creates. It is read once the request completes.
	/// </summary>
	public INativeWebResourceResponse? Response
	{
		get => Volatile.Read(ref _response);
		set => Volatile.Write(ref _response, value);
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	// Android reports no worker requests to shouldInterceptRequest (service workers have their own client).
	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> CoreWebView2WebResourceRequestSourceKinds.Document;

	public Deferral GetDeferral()
	{
		Interlocked.Increment(ref _pendingOperations);

		var completed = 0;
		return new Deferral(() =>
		{
			if (Interlocked.Exchange(ref completed, 1) == 0)
			{
				CompleteOperation();
			}
		});
	}

	/// <summary>
	/// Indicates whether headers have been modified and require re-fetching.
	/// </summary>
	internal bool RequiresRefetch => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the effective headers for re-fetching if headers were modified.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();

	/// <summary>
	/// Called once the event handlers have returned (or thrown).
	/// </summary>
	internal void OnHandlersReturned() => CompleteOperation();

	/// <summary>
	/// Blocks the calling thread (never the UI thread) until the request completes.
	/// </summary>
	/// <returns>false when <paramref name="timeout"/> elapsed or <paramref name="ct"/> was cancelled first.</returns>
	internal bool WaitForCompletion(TimeSpan timeout, CancellationToken ct)
	{
		try
		{
			return _completed.Wait(timeout, ct);
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}

	private void CompleteOperation()
	{
		if (Interlocked.Decrement(ref _pendingOperations) == 0)
		{
			_completed.Set();
		}
	}
}
#endif
