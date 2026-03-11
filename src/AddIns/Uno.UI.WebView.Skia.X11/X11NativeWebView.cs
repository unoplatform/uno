using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using GLib;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Logging;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Xaml.Controls;
using WebKit;
using Action = System.Action;
using Application = Gtk.Application;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Thread = System.Threading.Thread;
using Value = JavaScript.Value;
using Window = Gtk.Window;

[assembly: ApiExtension(
	typeof(INativeWebViewProvider),
	typeof(Uno.UI.WebView.Skia.X11.X11NativeWebViewProvider),
	ownerType: typeof(CoreWebView2),
	operatingSystemCondition: "linux")]

namespace Uno.UI.WebView.Skia.X11;

public class X11NativeWebViewProvider(CoreWebView2 coreWebView2) : INativeWebViewProvider
{
	INativeWebView INativeWebViewProvider.CreateNativeWebView(ContentPresenter contentPresenter) => new X11NativeWebView(coreWebView2, contentPresenter);
}

public class X11NativeWebView : INativeWebView
{
	[ThreadStatic] private static bool _isGtkThread;
	private static readonly Exception? _initException;
	private static readonly bool _usingWebKit2Gtk41;

	private readonly CoreWebView2 _coreWebView;
	private readonly ContentPresenter _presenter;
	private readonly Window _window;
	private readonly WebKit.WebView _webview;
	private readonly string _title = $"Uno WebView {Random.Shared.Next()}";

	private bool _dontRaiseNextNavigationCompleted;

	[DllImport("libwebkit2gtk-4.1.so", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr webkit_uri_request_get_http_headers(IntPtr request);

	[DllImport("libsoup-3.0.so", CallingConvention = CallingConvention.Cdecl)]
	private static extern void soup_message_headers_append(IntPtr hdrs, string name, string value);

	[DllImport("libgdk-3.so", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr gdk_x11_window_get_xid(IntPtr window);

	static X11NativeWebView()
	{
		try
		{
			// webkit2gtk-4.1 adds support for exposing libsoup objects, most importantly in webkit_uri_request_get_http_headers.
			// Gtk# by default loads libwebkit2gtk-4.0 (+ libsoup-2.0), but we want libwebkit2gtk-4.1 (+ libsoup-3.0), so what
			// we do is that before WebKitGtk# makes its dlopen calls, we load libwebkit2gtk-4.1 and put it where the handle to
			// libwebkit2gtk-4.0 will be expected to be, so that when WebKitGtk# attempts to dlopen(libwebkit2gtk-4.0), it will
			// find the handle already there and will just use it instead.
			if (!NativeLibrary.TryLoad("libwebkit2gtk-4.1.so", typeof(X11NativeWebView).Assembly, DllImportSearchPath.UserDirectories, out var webkitgtk41handle))
			{
				if (typeof(X11NativeWebView).Log().IsEnabled(LogLevel.Error))
				{
					typeof(X11NativeWebView).Log().Error($"libwebkit2gtk-4.1 was not found. Attempting to call {nameof(ProcessNavigation)} with an {nameof(HttpRequestMessage)} instance will crash the process.");
				}
			}
			else
			{
				Assembly assembly = Assembly.Load("WebKitGtkSharp");
				Type glibraryType = assembly.GetType("GLibrary") ?? throw new NullReferenceException("Couldn't find GLibrary in WebKitGtkSharp");
				Type libraryType = assembly.GetType("Library") ?? throw new NullReferenceException("Couldn't find Library in WebKitGtkSharp");
				object webkitLibraryEnum = Enum.ToObject(libraryType, 11);
				FieldInfo field = glibraryType.GetField("_libraries", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new NullReferenceException("Couldn't find a field named _libraries in GLibrary");
				object libraries = field.GetValue(null) ?? throw new NullReferenceException("Couldn't read the static field named _libraries in GLibrary");
				var setItemMethod = libraries.GetType().GetMethod("set_Item") ?? throw new NullReferenceException("Couldn't find a method named set_Item in _libraries");
				setItemMethod.Invoke(libraries, [webkitLibraryEnum, webkitgtk41handle]);
				_usingWebKit2Gtk41 = true;
			}

			if (!WebKit.Global.IsSupported)
			{
				_initException = new PlatformNotSupportedException("libwebkit2gtk-4.0 is not found. Make sure that WebKit2GTK 4.0 and GTK 3 are installed. See https://aka.platform.uno/webview2 for more information");
				return;
			}

			GLib.ExceptionManager.UnhandledException += args =>
			{
				if (typeof(X11NativeWebView).Log().IsEnabled(LogLevel.Error))
				{
					typeof(X11NativeWebView).Log().Error("GLib exception", args.ExceptionObject as Exception);
				}
			};

			new Thread(() =>
			{
				_isGtkThread = true;
				Application.Init();
				Application.Run();
			})
			{
				IsBackground = true,
				Name = "X11 WebKitGTK thread"
			}.Start();
		}
		catch (TypeInitializationException e)
		{
			_initException = e;
			if (typeof(X11NativeWebView).Log().IsEnabled(LogLevel.Error))
			{
				typeof(X11NativeWebView).Log().Error("Unable to initialize Gtk, visit https://aka.platform.uno/gtk-install for more information.", e);
			}
		}
	}

	public X11NativeWebView(CoreWebView2 coreWebView2, ContentPresenter presenter)
	{
		if (_initException is { })
		{
			throw _initException;
		}

		_coreWebView = coreWebView2;
		_presenter = presenter;

		(_window, _webview) = RunOnGtkThread(() =>
		{
			var window = new Window(Gtk.WindowType.Toplevel);
			window.Title = _title;
			window.Decorated = false;
			var webview = new WebKit.WebView();
			webview.Settings.EnableSmoothScrolling = true;
			webview.Settings.EnableJavascript = true;
			webview.Settings.AllowFileAccessFromFileUrls = true;
#if DEBUG
			webview.Settings.EnableDeveloperExtras = true;
#endif
			return (window, webview);
		});

		var xid = RunOnGtkThread(() =>
		{
			_webview.LoadChanged += WebViewOnLoadChanged;
			_webview.LoadFailed += WebViewOnLoadFailed;
			_webview.UserContentManager.RegisterScriptMessageHandler("unoWebView");
			_webview.UserContentManager.ScriptMessageReceived += UserContentManagerOnScriptMessageReceived;
			_webview.AddNotification(WebViewNotificationHandler);
			_window.Add(_webview);
			_webview.ShowAll();
			_window.Realize(); // creates the Gdk window (and the X11 window) without showing it
			return gdk_x11_window_get_xid(_window.Window.Handle);
		});

		presenter.Content = new X11NativeWindow(xid);
		if (presenter.IsInLiveTree)
		{
			RunOnGtkThread(() => _window.ShowAll());
		}

		presenter.Loaded += (_, _) => RunOnGtkThread(() => _window.ShowAll());
		presenter.Unloaded += (_, _) => RunOnGtkThread(() => _window.Hide());
	}

	~X11NativeWebView()
	{
		RunOnGtkThread(() => _window.Close());
	}

	public string DocumentTitle => RunOnGtkThread(() => _webview.Title);

	private static T RunOnGtkThread<T>(Func<T> func)
	{
		if (_isGtkThread)
		{
			return func();
		}
		else
		{
			var tcs = new TaskCompletionSource<T>();
			GLib.Idle.Add(() =>
			{
				tcs.SetResult(func());
				return false;
			});
			return tcs.Task.Result;
		}
	}

	private static void RunOnGtkThread(Action func)
	{
		if (_isGtkThread)
		{
			func();
		}
		else
		{
			var tcs = new TaskCompletionSource();
			GLib.Idle.Add(() =>
			{
				func();
				tcs.SetResult();
				return false;
			});
			tcs.Task.Wait();
		}
	}

	public void GoBack() => RunOnGtkThread(() => _webview.GoBack());
	public void GoForward() => RunOnGtkThread(() => _webview.GoForward());
	public void Stop() => RunOnGtkThread(() => _webview.StopLoading());
	public void Reload() => RunOnGtkThread(() => _webview.Reload());

	public void ProcessNavigation(Uri uri)
	{
		if (_coreWebView.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			var relativePath = uri.PathAndQuery;
			var baseUrl = Package.Current.InstalledPath;
			RunOnGtkThread(() => _webview.LoadUri($"file://{Path.Join(baseUrl, folderName, relativePath)}"));
		}
		else
		{
			ProcessNavigation(new HttpRequestMessage(HttpMethod.Get, uri));
		}
	}

	public void ProcessNavigation(string html) => RunOnGtkThread(() => { _webview.LoadHtml(html); });

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage) => RunOnGtkThread(() =>
	{
		var url = httpRequestMessage.RequestUri?.ToString();
		if (url is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(ProcessNavigation)} received an {nameof(HttpRequestMessage)} with a null uri.");
			}
			return;
		}

		var request = new URIRequest(url);

		if (_usingWebKit2Gtk41)
		{
			var headers = webkit_uri_request_get_http_headers(request.Handle);
			foreach (var header in httpRequestMessage.Headers)
			{
				// a header name can have multiple values
				foreach (var val in header.Value)
				{
					soup_message_headers_append(headers, header.Key, val);
				}
			}
		}

		_webview.LoadRequest(request);
	});

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		var tcs = new TaskCompletionSource<string?>();
		_webview.RunJavascript(script, null, (wv, res) =>
		{
			// INCREDIBLY IMPORTANT NOTES
			// Read JSValue only once. Each time result.JsValue is read, it increments the ref count
			// of the native object and you'll get "Unexpected number of toggle-refs.
			// g_object_add_toggle_ref() must be paired with g_object_remove_toggle_ref()". This also
			// means that if you're investing something related to the native ref-counting like a
			// double free, make sure that you don't read JsValue in the Debugger, or even hover over
			// it, because if you do, the ref count will be incremented and the double free will go away.
			// Instead, log JavaScript.Value.RefCount using reflection.
			// Console.WriteLine($"Ref count: {typeof(Value).GetProperty("RefCount", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(jsval)}");
			try
			{
				var result = ((WebKit.WebView)wv).RunJavascriptFinish(res);
				var jsval = result.JsValue;
				tcs.SetResult(JsValueToString(jsval));
				result.Dispose();
				// There is a WebKitGtkSharp bug that causes a double free if you let both result and result.JSValue finalize
				// It seems like JSValue gets freed as a part of the owning result, so if you let both of their finalizers run,
				// you'll get a double free.
				GC.SuppressFinalize(jsval);
			}
			catch (GException e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(ExecuteScriptAsync)} threw an exception", e);
				}
				tcs.SetException(e);
			}
		});
		return tcs.Task;
	}

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		// JsonSerializer.Serialize safely escapes quotes and concatenates the arguments (with a comma) to be passed to eval
		// the [1..^1] part is to remove [ and ].
		var argumentString = arguments is not null ? JsonSerializer.Serialize(arguments)[1..^1] : "";
		return ExecuteScriptAsync($"{script}({argumentString})", token);
	}

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(SetScrollingEnabled)} is not supported on the X11 target.");
		}
	}

	private void WebViewOnLoadChanged(object o, LoadChangedArgs args)
	{
		switch (args.LoadEvent)
		{
			case LoadEvent.Started:
				{
					if (Uri.TryCreate(_webview.Uri, UriKind.Absolute, out var uri))
					{
						_presenter.DispatcherQueue.TryEnqueue(() =>
						{
							_coreWebView.RaiseNavigationStarting(uri, out var cancel);
							if (cancel)
							{
								GLib.Idle.Add(() =>
								{
									_webview.StopLoading();
									return false;
								});
							}
						});
					}
				}
				break;
			case LoadEvent.Redirected:
				break;
			case LoadEvent.Committed:
				break;
			case LoadEvent.Finished:
				{
					var (canGoBack, canGoForward, uriString) = (_webview.CanGoBack(), _webview.CanGoForward(), _webview.Uri);
					if (_dontRaiseNextNavigationCompleted)
					{
						_dontRaiseNextNavigationCompleted = false;
					}
					else
					{
						_presenter.DispatcherQueue.TryEnqueue(() =>
						{
							_coreWebView.SetHistoryProperties(canGoBack, canGoForward);
							_coreWebView.RaiseHistoryChanged();
							Uri.TryCreate(uriString, UriKind.Absolute, out var uri);
							_presenter.DispatcherQueue.TryEnqueue(() =>
							{
								_coreWebView.RaiseNavigationCompleted(uri, isSuccess: true, httpStatusCode: 200, errorStatus: CoreWebView2WebErrorStatus.Unknown, shouldSetSource: true);
							});
						});
					}
				}
				break;
		}
	}

	private void WebViewOnLoadFailed(object o, LoadFailedArgs args)
	{
		_dontRaiseNextNavigationCompleted = true;
		Uri.TryCreate(args.FailingUri, UriKind.Absolute, out var uri);
		_presenter.DispatcherQueue.TryEnqueue(() =>
		{
			_coreWebView.RaiseNavigationCompleted(uri, isSuccess: false, httpStatusCode: 0, errorStatus: CoreWebView2WebErrorStatus.Unknown, shouldSetSource: true);
		});
	}

	private void WebViewNotificationHandler(object o, NotifyArgs args)
	{
		if (args.Property == "title")
		{
			_coreWebView.OnDocumentTitleChanged();
		}
	}

	private void UserContentManagerOnScriptMessageReceived(object o, ScriptMessageReceivedArgs args)
	{
		var result = args.JsResult;
		var value = result.JsValue;
		var str = JsValueToString(value);
		result.Dispose();
		GC.SuppressFinalize(value); // see comments in ExecuteScriptAsync
		_presenter.DispatcherQueue.TryEnqueue(() =>
		{
			_coreWebView.RaiseWebMessageReceived(str);
		});
	}

	private string JsValueToString(Value value)
	{
		if (value.IsNull)
		{
			return "null";
		}
		else if (value.IsUndefined)
		{
			return "undefined";
		}
		else if (value.IsString)
		{
			// We do this to get string quoting to be closer to other platforms
			// Regex.Unescape negates the double escaping due to Encode + ToJson
			return Regex.Unescape(System.Text.Json.JsonEncodedText.Encode(value.ToJson(0), JavaScriptEncoder.UnsafeRelaxedJsonEscaping).ToString());
		}
		return value.ToJson(0);
	}
}
