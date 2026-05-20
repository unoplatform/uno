extern alias mswebview2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Win32;
using NativeWebView = mswebview2::Microsoft.Web.WebView2.Core;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32NativeWebViewProvider(CoreWebView2 owner) : INativeWebViewProvider
{
	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var backend = Environment.GetEnvironmentVariable("UNO_WEBVIEW2_BACKEND")?.ToLowerInvariant();
		switch (backend?.Trim())
		{
#if NET10_0_OR_GREATER
			case "webview2aot":
				return CreateWin32NativeAotWebView(contentPresenter);
#endif // NET10_0_OR_GREATER
			case "microsoft.web.webview2":
				return CreateWin32NativeWebView(contentPresenter);
			case "":
			case null:
				break;
			default:
				typeof(Win32Host).LogError()?.Error($"Unsupported `UNO_WEBVIEW2_BACKEND` value `{backend}`! {SupportedUnoWebview2BackendValues}");
				break;
		}
		return CreateDefaultWebView(contentPresenter);
	}

#if NET10_0_OR_GREATER
	private const string SupportedUnoWebview2BackendValues = "Supported values: `webview2aot`, `microsoft.web.webview2`.";

	private INativeWebView CreateWin32NativeAotWebView(ContentPresenter contentPresenter)
		=> new Win32NativeAotWebView(owner, contentPresenter);

	private INativeWebView CreateDefaultWebView(ContentPresenter contentPresenter)
		=> CreateWin32NativeAotWebView(contentPresenter);
#else // !NET10_0_OR_GREATER
	private const string SupportedUnoWebview2BackendValues = "Supported value: `microsoft.web.webview2`.";

	private INativeWebView CreateDefaultWebView(ContentPresenter contentPresenter)
		=> CreateWin32NativeWebView(contentPresenter);
#endif // !NET10_0_OR_GREATER

	private INativeWebView CreateWin32NativeWebView(ContentPresenter contentPresenter)
	{
		try
		{
			Assembly.Load("Microsoft.Web.WebView2.Core");
		}
		catch (Exception)
		{
			typeof(Win32Host).LogError()?.Error($"Failed to load Microsoft.Web.WebView2.Core needed for WebView support. Make sure that WebView is included in the project's UnoFeatures. For more details, see https://aka.platform.uno/webview2 and https://aka.platform.uno/using-uno-sdk.");
			return null!;
		}
		return new Win32NativeWebView(owner, contentPresenter);
	}
}

internal partial class Win32NativeWebView : Win32NativeWebViewBase, ISupportsVirtualHostMapping, ISupportsWebResourceRequested, ISupportsUserAgent, ISupportsScriptEnabled, ISupportsZoomControl, ISupportsPostWebMessage, ISupportsDocumentCreatedScripts, ISupportsCookieManager, ISupportsPrint
{
	async Task<Stream> ISupportsPrint.PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct)
	{
		NativeWebView.CoreWebView2PrintSettings? nativeSettings = null;
		if (settings is not null)
		{
			nativeSettings = _nativeWebView.Environment.CreatePrintSettings();
			nativeSettings.Orientation = (NativeWebView.CoreWebView2PrintOrientation)(int)settings.Orientation;
			nativeSettings.ScaleFactor = settings.ScaleFactor;
			nativeSettings.MarginTop = settings.MarginTop;
			nativeSettings.MarginBottom = settings.MarginBottom;
			nativeSettings.MarginLeft = settings.MarginLeft;
			nativeSettings.MarginRight = settings.MarginRight;
			nativeSettings.ShouldPrintBackgrounds = settings.ShouldPrintBackgrounds;
			nativeSettings.PageWidth = settings.PageWidth;
			nativeSettings.PageHeight = settings.PageHeight;
		}

		var stream = await _nativeWebView.PrintToPdfStreamAsync(nativeSettings);
		return stream;
	}

	async Task<CoreWebView2PrintStatus> ISupportsPrint.ShowPrintUIAsync(CoreWebView2PrintDialogKind dialogKind, CancellationToken ct)
	{
		_nativeWebView.ShowPrintUI((NativeWebView.CoreWebView2PrintDialogKind)(int)dialogKind);
		await Task.Yield();
		return CoreWebView2PrintStatus.Succeeded;
	}

	async Task<IReadOnlyList<CoreWebView2Cookie>> ISupportsCookieManager.GetCookiesAsync(string uri, CancellationToken ct)
	{
		var nativeCookies = await _nativeWebView.CookieManager.GetCookiesAsync(uri);
		var result = new List<CoreWebView2Cookie>(nativeCookies.Count);
		foreach (var c in nativeCookies)
		{
			var cookie = new CoreWebView2Cookie(c.Name, c.Value, c.Domain, c.Path)
			{
				Expires = c.Expires == DateTime.MinValue ? -1d : (c.Expires - DateTime.UnixEpoch).TotalSeconds,
				IsHttpOnly = c.IsHttpOnly,
				IsSecure = c.IsSecure,
				SameSite = (CoreWebView2CookieSameSiteKind)(int)c.SameSite,
			};
			result.Add(cookie);
		}
		return result;
	}

	void ISupportsCookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie)
	{
		var native = _nativeWebView.CookieManager.CreateCookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path);
		if (cookie.Expires > 0)
		{
			native.Expires = DateTime.UnixEpoch.AddSeconds(cookie.Expires);
		}
		native.IsHttpOnly = cookie.IsHttpOnly;
		native.IsSecure = cookie.IsSecure;
		native.SameSite = (NativeWebView.CoreWebView2CookieSameSiteKind)(int)cookie.SameSite;
		_nativeWebView.CookieManager.AddOrUpdateCookie(native);
	}

	void ISupportsCookieManager.DeleteCookie(CoreWebView2Cookie cookie)
	{
		var native = _nativeWebView.CookieManager.CreateCookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path);
		_nativeWebView.CookieManager.DeleteCookie(native);
	}

	void ISupportsCookieManager.DeleteCookies(string name, string? uri) => _nativeWebView.CookieManager.DeleteCookies(name, uri);

	void ISupportsCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path)
		=> _nativeWebView.CookieManager.DeleteCookiesWithDomainAndPath(name, domain, path);

	void ISupportsCookieManager.DeleteAllCookies() => _nativeWebView.CookieManager.DeleteAllCookies();

	void ISupportsPostWebMessage.PostWebMessageAsJson(string json) => _nativeWebView.PostWebMessageAsJson(json);

	void ISupportsPostWebMessage.PostWebMessageAsString(string message) => _nativeWebView.PostWebMessageAsString(message);

	async Task<string> ISupportsDocumentCreatedScripts.AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		=> await _nativeWebView.AddScriptToExecuteOnDocumentCreatedAsync(javaScript);

	void ISupportsDocumentCreatedScripts.RemoveScriptToExecuteOnDocumentCreated(string id)
		=> _nativeWebView.RemoveScriptToExecuteOnDocumentCreated(id);

	public string? UserAgent
	{
		get => _nativeWebView?.Settings.UserAgent;
		set
		{
			if (_nativeWebView is { } nv && value is not null)
			{
				nv.Settings.UserAgent = value;
			}
		}
	}

	bool ISupportsScriptEnabled.IsScriptEnabled
	{
		get => _nativeWebView?.Settings.IsScriptEnabled ?? true;
		set { if (_nativeWebView is { } nv) nv.Settings.IsScriptEnabled = value; }
	}

	bool ISupportsZoomControl.IsZoomControlEnabled
	{
		get => _nativeWebView?.Settings.IsZoomControlEnabled ?? true;
		set { if (_nativeWebView is { } nv) nv.Settings.IsZoomControlEnabled = value; }
	}
	private readonly CoreWebView2 _coreWebView;
	private readonly NativeWebView.CoreWebView2 _nativeWebView;
	private readonly NativeWebView.CoreWebView2Controller _controller;
	private Dictionary<ulong, string> _navigationIdToUriMap = new();
	private string _documentTitle = string.Empty;

	public Win32NativeWebView(CoreWebView2 owner, ContentPresenter presenter)
	: base(presenter)
	{
		_coreWebView = owner;

		ForwardBackgroundToPresenter();

		var tcs = new TaskCompletionSource<NativeWebView.CoreWebView2Controller>();
		NativeDispatcher.Main.EnqueueAsync(async () =>
		{
			var customEnv = _coreWebView.CustomEnvironment;
			var customOptions = customEnv?.Options;
			var customControllerOptions = _coreWebView.CustomControllerOptions;

			var nativeEnvOptions = new NativeWebView.CoreWebView2EnvironmentOptions
			{
				AllowSingleSignOnUsingOSPrimaryAccount = customOptions?.AllowSingleSignOnUsingOSPrimaryAccount
					?? FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount
			};
			if (customOptions is not null)
			{
				if (!string.IsNullOrEmpty(customOptions.AdditionalBrowserArguments))
				{
					nativeEnvOptions.AdditionalBrowserArguments = customOptions.AdditionalBrowserArguments;
				}
				if (!string.IsNullOrEmpty(customOptions.Language))
				{
					nativeEnvOptions.Language = customOptions.Language;
				}
				if (!string.IsNullOrEmpty(customOptions.TargetCompatibleBrowserVersion))
				{
					nativeEnvOptions.TargetCompatibleBrowserVersion = customOptions.TargetCompatibleBrowserVersion;
				}
				nativeEnvOptions.ExclusiveUserDataFolderAccess = customOptions.ExclusiveUserDataFolderAccess;
				nativeEnvOptions.IsCustomCrashReportingEnabled = customOptions.IsCustomCrashReportingEnabled;
			}
			else if (!string.IsNullOrEmpty(FeatureConfiguration.WebView2.AdditionalBrowserArguments))
			{
				nativeEnvOptions.AdditionalBrowserArguments = FeatureConfiguration.WebView2.AdditionalBrowserArguments;
			}

			var browserFolder = customEnv?.BrowserExecutableFolder;
			var userDataFolder = !string.IsNullOrEmpty(customEnv?.UserDataFolder)
				? customEnv!.UserDataFolder
				: Path.Combine(ApplicationData.Current.LocalFolder.Path, "WebView2");
			var env = await NativeWebView.CoreWebView2Environment.CreateAsync(browserFolder, userDataFolder, nativeEnvOptions);
			if (customEnv is not null)
			{
				customEnv.BrowserVersionString = env.BrowserVersionString;
			}

			NativeWebView.CoreWebView2Controller controller;
			if (customControllerOptions is not null)
			{
				var nativeCtrlOptions = env.CreateCoreWebView2ControllerOptions();
				nativeCtrlOptions.IsInPrivateModeEnabled = customControllerOptions.IsInPrivateModeEnabled;
				if (!string.IsNullOrEmpty(customControllerOptions.ProfileName))
				{
					nativeCtrlOptions.ProfileName = customControllerOptions.ProfileName;
				}
				if (!string.IsNullOrEmpty(customControllerOptions.ScriptLocale))
				{
					nativeCtrlOptions.ScriptLocale = customControllerOptions.ScriptLocale;
				}
				controller = await env.CreateCoreWebView2ControllerAsync(Hwnd, nativeCtrlOptions);
			}
			else
			{
				controller = await env.CreateCoreWebView2ControllerAsync(Hwnd);
			}

			// Hide until NavigationCompleted to suppress the initial black frame.
			controller.IsVisible = false;

			if (Presenter.Background is SolidColorBrush { Color: { } color })
			{
				controller.DefaultBackgroundColor = Color.FromArgb(byte.MaxValue, color.R, color.G, color.B);
			}

			tcs.SetResult(controller);
		});

		while (!tcs.Task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		_controller = tcs.Task.Result;

		_nativeWebView = _controller.CoreWebView2;
		_nativeWebView.Settings.IsScriptEnabled = true;
		_nativeWebView.Settings.IsWebMessageEnabled = true;
		_nativeWebView.Settings.AreDefaultScriptDialogsEnabled = true;
		_nativeWebView.Settings.AreDevToolsEnabled = FeatureConfiguration.WebView2.EnableDevTools;
		_controller.Bounds = new Rectangle(0, 0, 500, 500);

		_nativeWebView.NavigationCompleted += EventHandlerBuilder<NativeWebView.CoreWebView2NavigationCompletedEventArgs>(static (@this, o, a) => @this.NativeWebView_NavigationCompleted(o, a));
		_nativeWebView.NewWindowRequested += EventHandlerBuilder<NativeWebView.CoreWebView2NewWindowRequestedEventArgs>(static (@this, o, a) => @this.NativeWebView_NewWindowRequested(o, a));
		_nativeWebView.SourceChanged += EventHandlerBuilder<NativeWebView.CoreWebView2SourceChangedEventArgs>(static (@this, o, a) => @this.NativeWebView_SourceChanged(o, a));
		_nativeWebView.WebMessageReceived += EventHandlerBuilder<NativeWebView.CoreWebView2WebMessageReceivedEventArgs>(static (@this, o, a) => @this.NativeWebView_WebMessageReceived(o, a));
		_nativeWebView.NavigationStarting += EventHandlerBuilder<NativeWebView.CoreWebView2NavigationStartingEventArgs>(static (@this, o, a) => @this.NativeWebView_NavigationStarting(o, a));
		_nativeWebView.HistoryChanged += EventHandlerBuilder<object>(static (@this, o, a) => @this.CoreWebView2_HistoryChanged(o, a));
		_nativeWebView.DocumentTitleChanged += EventHandlerBuilder<object>(static (@this, o, a) => @this.OnNativeTitleChanged(o, a));
		_nativeWebView.WebResourceRequested += NativeWebView2_WebResourceRequested;
		_nativeWebView.ContentLoading += EventHandlerBuilder<NativeWebView.CoreWebView2ContentLoadingEventArgs>(static (@this, _, e) =>
			@this._coreWebView.RaiseContentLoading(new CoreWebView2ContentLoadingEventArgs(e.IsErrorPage, e.NavigationId)));
		_nativeWebView.DOMContentLoaded += EventHandlerBuilder<NativeWebView.CoreWebView2DOMContentLoadedEventArgs>(static (@this, _, e) =>
			@this._coreWebView.RaiseDOMContentLoaded(new CoreWebView2DOMContentLoadedEventArgs(e.NavigationId)));
		UpdateDocumentTitle();
	}

	private void ForwardBackgroundToPresenter()
	{
		if (_coreWebView.Owner is Microsoft.UI.Xaml.Controls.WebView2 view)
		{
			Presenter.SetBinding(FrameworkElement.BackgroundProperty, new Binding()
			{
				Path = new(nameof(view.Background)),
				Source = view,
				Mode = BindingMode.OneWay
			});
		}
	}

	protected override void OnWindowSizeChanged()
	{
		if (_controller is not null)
		{
			PInvoke.GetClientRect(Hwnd, out var bounds);
			_controller.Bounds = bounds;
		}
	}

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	private void NativeWebView2_WebResourceRequested(object? sender, NativeWebView.CoreWebView2WebResourceRequestedEventArgs e)
	{
		WebResourceRequested?.Invoke(this, new(new Win32WebResourceRequestedEventArgsWrapper(e)));
	}

	private EventHandler<T> EventHandlerBuilder<T>(Action<Win32NativeWebView, object?, T> handler)
	{
		var weakRef = new WeakReference<Win32NativeWebView>(this);
		return (sender, args) =>
		{
			if (weakRef.TryGetTarget(out var target))
			{
				handler(target, sender, args);
			}
		};
	}

	public override string DocumentTitle
	=> _documentTitle;

	private void SetDocumentTitle(string value)
	{
		if (_documentTitle != value)
		{
			_documentTitle = value;
			_coreWebView.OnDocumentTitleChanged();
		}
	}

	private void NativeWebView_SourceChanged(object? sender, NativeWebView.CoreWebView2SourceChangedEventArgs e)
	{
		_coreWebView.Source = _nativeWebView.Source;
	}

	private void NativeWebView_WebMessageReceived(object? sender, NativeWebView.CoreWebView2WebMessageReceivedEventArgs e)
	{
		_coreWebView.RaiseWebMessageReceived(e.WebMessageAsJson);
	}

	private void NativeWebView_NavigationStarting(object? sender, NativeWebView.CoreWebView2NavigationStartingEventArgs e)
	{
		if (e.Uri is null)
		{
			return;
		}

		bool cancel;
		if (Uri.TryCreate(e.Uri, UriKind.RelativeOrAbsolute, out var uri))
		{
			_coreWebView.RaiseNavigationStarting(uri, out cancel);
		}
		else
		{
			_coreWebView.RaiseNavigationStarting(e.Uri, out cancel);
		}
		_coreWebView.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
		e.Cancel = cancel;
		_navigationIdToUriMap[e.NavigationId] = e.Uri;
	}

	private void NativeWebView_NavigationCompleted(object? sender, NativeWebView.CoreWebView2NavigationCompletedEventArgs e)
	{
		_controller.IsVisible = true;

		if (!_navigationIdToUriMap.TryGetValue(e.NavigationId, out var uriString))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Got NavigationCompleted for unknown navigation id");
			}
		}

		_navigationIdToUriMap.Remove(e.NavigationId);
		if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
		{
			if (e.WebErrorStatus == NativeWebView.CoreWebView2WebErrorStatus.ConnectionAborted)
			{
				_coreWebView.RaiseUnsupportedUriSchemeIdentified(uri, out _);
			}
			_coreWebView.RaiseNavigationCompleted(uri, e.IsSuccess, e.HttpStatusCode, (CoreWebView2WebErrorStatus)e.WebErrorStatus, shouldSetSource: false);
		}
		else
		{
			_coreWebView.RaiseNavigationCompleted(null, e.IsSuccess, e.HttpStatusCode, (CoreWebView2WebErrorStatus)e.WebErrorStatus, shouldSetSource: false);
		}
	}

	private void NativeWebView_NewWindowRequested(object? sender, NativeWebView.CoreWebView2NewWindowRequestedEventArgs e)
	{
		_coreWebView.RaiseNewWindowRequested(
			e.Uri,
			CoreWebView2.BlankUri,
			out var handled);

		e.Handled = handled;
	}

	private void OnNativeTitleChanged(object? sender, object e) => UpdateDocumentTitle();

	private void UpdateDocumentTitle()
	{
		SetDocumentTitle(_nativeWebView.DocumentTitle);
	}

	private void CoreWebView2_HistoryChanged(object? sender, object e)
	{
		_coreWebView.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
		_coreWebView.RaiseHistoryChanged();
	}

	public override Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
		=> _nativeWebView.ExecuteScriptAsync(script);

	public override void GoBack()
		=> _nativeWebView.GoBack();

	public override void GoForward()
		=> _nativeWebView.GoForward();

	public override Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		if (arguments is null || arguments.Length == 0)
		{
			return ExecuteScriptAsync($"{script}()", token);
		}

		var adjustedScript = new StringBuilder(script);
		adjustedScript.Append('(');

		for (int i = 0; i < arguments.Length; i++)
		{
			adjustedScript.Append('"');
			adjustedScript.Append(arguments[i]);
			adjustedScript.Append('"');

			if (i < arguments.Length - 1)
			{
				adjustedScript.Append(',');
			}
		}

		adjustedScript.Append(')');
		return ExecuteScriptAsync(adjustedScript.ToString(), token);
	}

	public override void ProcessNavigation(Uri uri) => _nativeWebView.Navigate(uri.ToString());

	public override void ProcessNavigation(string html) => _nativeWebView.NavigateToString(html);

	public override void ProcessNavigation(HttpRequestMessage httpRequestMessage) => ProcessNavigationCore(httpRequestMessage);

	private void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
	{
		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			if (header.Key != "Host")
			{
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
			}
		}

		var request = _nativeWebView.Environment.CreateWebResourceRequest(httpRequestMessage.RequestUri!.ToString(), httpRequestMessage.Method.Method, httpRequestMessage.Content?.ReadAsStream() ?? Stream.Null, builder.ToString());
		_nativeWebView.NavigateWithWebResourceRequest(request);
	}

	public override void Reload()
		=> _nativeWebView.Reload();

	public override void SetScrollingEnabled(bool isScrollingEnabled)
	{
	}

	public override void Stop()
		=> _nativeWebView.Stop();

	public void ClearVirtualHostNameToFolderMapping(string hostName)
		=> _nativeWebView.ClearVirtualHostNameToFolderMapping(hostName);

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
		=> _nativeWebView.SetVirtualHostNameToFolderMapping(hostName, folderPath, (NativeWebView.CoreWebView2HostResourceAccessKind)accessKind);

	public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
		=> _nativeWebView.AddWebResourceRequestedFilter(uri, (NativeWebView.CoreWebView2WebResourceContext)resourceContext, (NativeWebView.CoreWebView2WebResourceRequestSourceKinds)requestSourceKinds);

	public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
		=> _nativeWebView.RemoveWebResourceRequestedFilter(uri, (NativeWebView.CoreWebView2WebResourceContext)resourceContext, (NativeWebView.CoreWebView2WebResourceRequestSourceKinds)requestSourceKinds);
}
