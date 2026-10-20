#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using WebKit;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Virtual hosts served through a <c>WKURLSchemeHandler</c> on <see cref="WebViewVirtualHostMap.NativeScheme"/>.
/// </summary>
partial class UnoWKWebView : ISupportsCustomSchemeVirtualHosts
{
	private CustomSchemeResourceHandler? _resourceHandler;

	private CustomSchemeResourceHandler ResourceHandler => _resourceHandler ??= new CustomSchemeResourceHandler(
		MatchesAnyFilter,
		args => WebResourceRequested?.Invoke(this, args))
	{
		FolderBasePath = NSBundle.MainBundle.ResourcePath,
	};

	public void SetVirtualHostMap(WebViewVirtualHostMap map) => ResourceHandler.Map = map;

	// WebKit only accepts scheme handlers before the WKWebView is created.
	private static WKWebViewConfiguration CreateConfiguration(VirtualHostSchemeHandler schemeHandler)
	{
		var configuration = new WKWebViewConfiguration();
		configuration.SetUrlSchemeHandler(schemeHandler, WebViewVirtualHostMap.NativeScheme);
		return configuration;
	}

	internal static bool IsVirtualHostScheme(string? scheme)
		=> string.Equals(scheme, WebViewVirtualHostMap.NativeScheme, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Forwards each task to the owning view's <see cref="CustomSchemeResourceHandler"/>.
	/// </summary>
	/// <remarks>
	/// WebKit calls both methods on the main thread and throws if a stopped task is messaged, so a task is only answered
	/// on the main thread and only while it is still in <see cref="_tasks"/>.
	/// </remarks>
	internal sealed class VirtualHostSchemeHandler : NSObject, IWKUrlSchemeHandler
	{
		private readonly Dictionary<IntPtr, CancellationTokenSource> _tasks = new();
		private WeakReference<UnoWKWebView>? _owner;

		public UnoWKWebView? Owner
		{
			get => _owner is not null && _owner.TryGetTarget(out var owner) ? owner : null;
			set => _owner = value is null ? null : new(value);
		}

		[Export("webView:startURLSchemeTask:")]
		public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
		{
			var key = urlSchemeTask.Handle;
			var cts = new CancellationTokenSource();
			_tasks[key] = cts;

			if (Owner is not { } owner)
			{
				Fail(key, urlSchemeTask, "The WebView is not available.");
				return;
			}

			_ = ServeAsync(owner, key, urlSchemeTask, cts.Token);
		}

		[Export("webView:stopURLSchemeTask:")]
		public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
		{
			if (_tasks.Remove(urlSchemeTask.Handle, out var cts))
			{
				cts.Cancel();
			}
		}

		private async Task ServeAsync(UnoWKWebView owner, IntPtr key, IWKUrlSchemeTask task, CancellationToken ct)
		{
			try
			{
				var response = await owner.ResourceHandler.HandleAsync(CreateRequest(task.Request), ct);
				OnMainThread(() => Complete(key, task, response));
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				// Stopped by WebKit: the task must not be messaged anymore.
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to serve '{task.Request.Url?.AbsoluteString}'.", e);
				}

				OnMainThread(() => Fail(key, task, e.Message));
			}
		}

		private static CustomSchemeRequest CreateRequest(NSUrlRequest request)
		{
			var headers = new List<KeyValuePair<string, string>>();
			if (request.Headers is { } nativeHeaders)
			{
				foreach (var pair in nativeHeaders)
				{
					headers.Add(new(pair.Key.ToString(), pair.Value?.ToString() ?? ""));
				}
			}

			return new CustomSchemeRequest(
				request.Url?.AbsoluteString ?? "",
				request.HttpMethod ?? "GET",
				headers,
				ReadBody(request),
				request.MainDocumentURL?.AbsoluteString);
		}

		private static byte[]? ReadBody(NSUrlRequest request)
		{
			if (request.Body is { Length: > 0 } body)
			{
				return body.ToArray();
			}

			if (request.BodyStream is not { } stream)
			{
				return null;
			}

			var buffer = new byte[16384];
			using var data = new MemoryStream();
			stream.Open();
			try
			{
				nint read;
				while ((read = stream.Read(buffer, (nuint)buffer.Length)) > 0)
				{
					data.Write(buffer, 0, (int)read);
				}
			}
			finally
			{
				stream.Close();
			}

			return data.ToArray();
		}

		private void Complete(IntPtr key, IWKUrlSchemeTask task, WebResourceResponseSnapshot response)
		{
			if (!_tasks.Remove(key))
			{
				return;
			}

			// Repeated names are folded into one field: NSHTTPURLResponse holds one value per name.
			var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var header in response.Headers)
			{
				fields[header.Key] = fields.TryGetValue(header.Key, out var existing) ? existing + ", " + header.Value : header.Value;
			}

			var headers = new NSMutableDictionary<NSString, NSString>();
			foreach (var field in fields)
			{
				headers[new NSString(field.Key)] = new NSString(field.Value);
			}

			try
			{
				using var urlResponse = new NSHttpUrlResponse(task.Request.Url, response.StatusCode, "HTTP/1.1", headers);
				task.DidReceiveResponse(urlResponse);
				task.DidReceiveData(NSData.FromArray(response.Body));
				task.DidFinish();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Failed to answer '{task.Request.Url?.AbsoluteString}'.", e);
				}
			}
		}

		private void Fail(IntPtr key, IWKUrlSchemeTask task, string message)
		{
			if (!_tasks.Remove(key))
			{
				return;
			}

			try
			{
				var userInfo = NSDictionary.FromObjectsAndKeys(
					new NSObject[] { new NSString(message), new NSString(task.Request.Url?.AbsoluteString ?? "") },
					new NSObject[] { NSError.LocalizedDescriptionKey, new NSString("NSErrorFailingURLStringKey") });
				task.DidFailWithError(new NSError(NSError.NSUrlErrorDomain, (nint)(long)NSUrlError.ResourceUnavailable, userInfo));
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Failed to fail '{task.Request.Url?.AbsoluteString}'.", e);
				}
			}
		}

		private static void OnMainThread(Action action)
		{
			if (NSThread.IsMain)
			{
				action();
			}
			else
			{
				DispatchQueue.MainQueue.DispatchAsync(action);
			}
		}
	}
}
