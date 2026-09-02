#if NET10_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Web.WebView2.Core;

using DirectN;

using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed partial class Win32NativeAotWebView
{
	private WebView2.ICoreWebView2Settings2 _settings = null!;
	private WebView2.EventRegistrationToken _navigationCompletedToken;
	private WebView2.EventRegistrationToken _newWindowRequestedToken;
	private WebView2.EventRegistrationToken _sourceChangedToken;
	private WebView2.EventRegistrationToken _webMessageReceivedToken;
	private WebView2.EventRegistrationToken _navigationStartingToken;
	private WebView2.EventRegistrationToken _historyChangedToken;
	private WebView2.EventRegistrationToken _documentTitleChangedToken;
	private WebView2.EventRegistrationToken _webResourceRequestedToken;
	private WebView2.EventRegistrationToken _contentLoadingToken;
	private WebView2.EventRegistrationToken _domContentLoadedToken;
	private bool _eventsRegistered;
	private bool _isClosed;

	private WebView2.CoreWebView2EnvironmentOptions CreateEnvironmentOptions()
	{
		var options = new WebView2.CoreWebView2EnvironmentOptions();
		var customOptions = _coreWebView.CustomEnvironment?.Options;

		options.put_AllowSingleSignOnUsingOSPrimaryAccount(
			(customOptions?.AllowSingleSignOnUsingOSPrimaryAccount
				?? FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount)
			? BOOL.TRUE
			: BOOL.FALSE).ThrowOnError();

		if (customOptions is not null)
		{
			SetNativeString(customOptions.AdditionalBrowserArguments, options.put_AdditionalBrowserArguments);
			SetNativeString(customOptions.Language, options.put_Language);
			SetNativeString(customOptions.TargetCompatibleBrowserVersion, options.put_TargetCompatibleBrowserVersion);
			options.put_ExclusiveUserDataFolderAccess(customOptions.ExclusiveUserDataFolderAccess ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
			options.put_IsCustomCrashReportingEnabled(customOptions.IsCustomCrashReportingEnabled ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		}
		else
		{
			SetNativeString(FeatureConfiguration.WebView2.AdditionalBrowserArguments, options.put_AdditionalBrowserArguments);
		}

		return options;
	}

	private void CreateController(
		WebView2.ICoreWebView2Environment environment,
		WebView2.ICoreWebView2CreateCoreWebView2ControllerCompletedHandler handler)
	{
		if (_coreWebView.CustomEnvironment is { } customEnvironment)
		{
			environment.get_BrowserVersionString(out var browserVersion).ThrowOnError();
			customEnvironment.BrowserVersionString = browserVersion.ToString() ?? string.Empty;
		}

		if (_coreWebView.CustomControllerOptions is not { } customControllerOptions)
		{
			environment.CreateCoreWebView2Controller(new HWND((IntPtr)Hwnd.Value), handler).ThrowOnError();
			return;
		}

		if (environment is not WebView2.ICoreWebView2Environment10 environment10)
		{
			throw new NotSupportedException("The installed WebView2 runtime does not support custom controller options.");
		}

		environment10.CreateCoreWebView2ControllerOptions(out var nativeOptions).ThrowOnError();
		nativeOptions.put_IsInPrivateModeEnabled(customControllerOptions.IsInPrivateModeEnabled ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		SetNativeString(customControllerOptions.ProfileName, nativeOptions.put_ProfileName);

		if (!string.IsNullOrEmpty(customControllerOptions.ScriptLocale))
		{
			if (nativeOptions is not WebView2.ICoreWebView2ControllerOptions2 nativeOptions2)
			{
				throw new NotSupportedException("The installed WebView2 runtime does not support ScriptLocale.");
			}

			SetNativeString(customControllerOptions.ScriptLocale, nativeOptions2.put_ScriptLocale);
		}

		environment10.CreateCoreWebView2ControllerWithOptions(
			new HWND((IntPtr)Hwnd.Value),
			nativeOptions,
			handler).ThrowOnError();
	}

	string? ISupportsUserAgent.UserAgent
	{
		get
		{
			_settings.get_UserAgent(out var value).ThrowOnError();
			return value.ToString();
		}
		set => SetNativeString(value, _settings.put_UserAgent);
	}

	bool ISupportsScriptEnabled.IsScriptEnabled
	{
		get
		{
			BOOL value = default;
			_settings.get_IsScriptEnabled(ref value).ThrowOnError();
			return value.Value != 0;
		}
		set => _settings.put_IsScriptEnabled(value ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
	}

	bool ISupportsZoomControl.IsZoomControlEnabled
	{
		get
		{
			BOOL value = default;
			_settings.get_IsZoomControlEnabled(ref value).ThrowOnError();
			return value.Value != 0;
		}
		set => _settings.put_IsZoomControlEnabled(value ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
	}

	void ISupportsPostWebMessage.PostWebMessageAsJson(string json) =>
		InvokeWithNativeString(json, _nativeWebView.PostWebMessageAsJson);

	void ISupportsPostWebMessage.PostWebMessageAsString(string message) =>
		InvokeWithNativeString(message, _nativeWebView.PostWebMessageAsString);

	async Task<string> ISupportsDocumentCreatedScripts.AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
		unsafe
		{
			fixed (char* p_javaScript = javaScript)
			{
				_nativeWebView.AddScriptToExecuteOnDocumentCreated(
					new PWSTR(p_javaScript),
					new WebView2.Utilities.CoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler((errorCode, result) =>
					{
						if (errorCode.IsError)
						{
							tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("Unable to add the document-created script."));
						}
						else
						{
							tcs.TrySetResult(result.ToString() ?? string.Empty);
						}
					})).ThrowOnError();
			}
		}

		return await tcs.Task;
	}

	void ISupportsDocumentCreatedScripts.RemoveScriptToExecuteOnDocumentCreated(string id) =>
		InvokeWithNativeString(id, _nativeWebView.RemoveScriptToExecuteOnDocumentCreated);

	async Task<IReadOnlyList<CoreWebView2Cookie>> ISupportsCookieManager.GetCookiesAsync(string uri, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var tcs = new TaskCompletionSource<IReadOnlyList<CoreWebView2Cookie>>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
		var manager = GetCookieManager();

		unsafe
		{
			fixed (char* p_uri = uri)
			{
				manager.GetCookies(
					new PWSTR(p_uri),
					new WebView2.Utilities.CoreWebView2GetCookiesCompletedHandler((errorCode, result) =>
					{
						if (errorCode.IsError)
						{
							tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("Unable to get WebView2 cookies."));
							return;
						}

						try
						{
							uint count = 0;
							result.get_Count(ref count).ThrowOnError();
							var cookies = new List<CoreWebView2Cookie>((int)count);
							for (uint index = 0; index < count; index++)
							{
								result.GetValueAtIndex(index, out var nativeCookie).ThrowOnError();
								cookies.Add(ConvertCookie(nativeCookie));
							}
							tcs.TrySetResult(cookies);
						}
						catch (Exception error)
						{
							tcs.TrySetException(error);
						}
					})).ThrowOnError();
			}
		}

		return await tcs.Task;
	}

	void ISupportsCookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie) =>
		GetCookieManager().AddOrUpdateCookie(CreateNativeCookie(cookie)).ThrowOnError();

	void ISupportsCookieManager.DeleteCookie(CoreWebView2Cookie cookie) =>
		GetCookieManager().DeleteCookie(CreateNativeCookie(cookie)).ThrowOnError();

	void ISupportsCookieManager.DeleteCookies(string name, string? uri)
	{
		unsafe
		{
			var actualUri = uri ?? string.Empty;
			fixed (char* p_name = name)
			fixed (char* p_uri = actualUri)
			{
				GetCookieManager().DeleteCookies(new PWSTR(p_name), new PWSTR(p_uri)).ThrowOnError();
			}
		}
	}

	void ISupportsCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path)
	{
		unsafe
		{
			fixed (char* p_name = name)
			fixed (char* p_domain = domain)
			fixed (char* p_path = path)
			{
				GetCookieManager().DeleteCookiesWithDomainAndPath(
					new PWSTR(p_name),
					new PWSTR(p_domain),
					new PWSTR(p_path)).ThrowOnError();
			}
		}
	}

	void ISupportsCookieManager.DeleteAllCookies() => GetCookieManager().DeleteAllCookies().ThrowOnError();

	async Task<Stream> ISupportsPrint.PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var nativeSettings = CreatePrintSettings(settings);
		var tcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

		_nativeWebView.PrintToPdfStream(
			nativeSettings,
			new WebView2.Utilities.CoreWebView2PrintToPdfStreamCompletedHandler((errorCode, result) =>
			{
				if (errorCode.IsError)
				{
					tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("Unable to print the WebView2 content to PDF."));
				}
				else
				{
					try
					{
						tcs.TrySetResult(AotStreamHelpers.ConvertIStream(result).AsStreamForRead());
					}
					catch (Exception error)
					{
						tcs.TrySetException(error);
					}
				}
			})).ThrowOnError();

		return await tcs.Task;
	}

	Task<CoreWebView2PrintStatus> ISupportsPrint.ShowPrintUIAsync(CoreWebView2PrintDialogKind dialogKind, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		_nativeWebView.ShowPrintUI((WebView2.COREWEBVIEW2_PRINT_DIALOG_KIND)(int)dialogKind).ThrowOnError();
		return Task.FromResult(CoreWebView2PrintStatus.Succeeded);
	}

	void ISupportsClose.Close()
	{
		if (_isClosed)
		{
			return;
		}

		_isClosed = true;
		if (_eventsRegistered)
		{
			RemoveEvent("NavigationCompleted", () => _nativeWebView.remove_NavigationCompleted(_navigationCompletedToken));
			RemoveEvent("NewWindowRequested", () => _nativeWebView.remove_NewWindowRequested(_newWindowRequestedToken));
			RemoveEvent("SourceChanged", () => _nativeWebView.remove_SourceChanged(_sourceChangedToken));
			RemoveEvent("WebMessageReceived", () => _nativeWebView.remove_WebMessageReceived(_webMessageReceivedToken));
			RemoveEvent("NavigationStarting", () => _nativeWebView.remove_NavigationStarting(_navigationStartingToken));
			RemoveEvent("HistoryChanged", () => _nativeWebView.remove_HistoryChanged(_historyChangedToken));
			RemoveEvent("DocumentTitleChanged", () => _nativeWebView.remove_DocumentTitleChanged(_documentTitleChangedToken));
			RemoveEvent("WebResourceRequested", () => _nativeWebView.remove_WebResourceRequested(_webResourceRequestedToken));
			RemoveEvent("ContentLoading", () => _nativeWebView.remove_ContentLoading(_contentLoadingToken));
			RemoveEvent("DOMContentLoaded", () => _nativeWebView.remove_DOMContentLoaded(_domContentLoadedToken));
			_eventsRegistered = false;
		}

		try
		{
			_controller.Close().ThrowOnError();
		}
		finally
		{
			DestroyWindow();
		}
	}

	private WebView2.ICoreWebView2CookieManager GetCookieManager()
	{
		_nativeWebView.get_CookieManager(out var manager).ThrowOnError();
		return manager;
	}

	private WebView2.ICoreWebView2Cookie CreateNativeCookie(CoreWebView2Cookie cookie)
	{
		var manager = GetCookieManager();
		WebView2.ICoreWebView2Cookie nativeCookie;
		unsafe
		{
			fixed (char* p_name = cookie.Name)
			fixed (char* p_value = cookie.Value)
			fixed (char* p_domain = cookie.Domain)
			fixed (char* p_path = cookie.Path)
			{
				manager.CreateCookie(
					new PWSTR(p_name),
					new PWSTR(p_value),
					new PWSTR(p_domain),
					new PWSTR(p_path),
					out nativeCookie).ThrowOnError();
			}
		}

		if (!cookie.IsSession)
		{
			nativeCookie.put_Expires(cookie.Expires).ThrowOnError();
		}
		nativeCookie.put_IsHttpOnly(cookie.IsHttpOnly ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		nativeCookie.put_IsSecure(cookie.IsSecure ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		nativeCookie.put_SameSite((WebView2.COREWEBVIEW2_COOKIE_SAME_SITE_KIND)(int)cookie.SameSite).ThrowOnError();
		return nativeCookie;
	}

	private static CoreWebView2Cookie ConvertCookie(WebView2.ICoreWebView2Cookie nativeCookie)
	{
		nativeCookie.get_Name(out var name).ThrowOnError();
		nativeCookie.get_Value(out var value).ThrowOnError();
		nativeCookie.get_Domain(out var domain).ThrowOnError();
		nativeCookie.get_Path(out var path).ThrowOnError();

		double expires = default;
		BOOL isHttpOnly = default;
		BOOL isSecure = default;
		BOOL isSession = default;
		WebView2.COREWEBVIEW2_COOKIE_SAME_SITE_KIND sameSite = default;
		nativeCookie.get_Expires(ref expires).ThrowOnError();
		nativeCookie.get_IsHttpOnly(ref isHttpOnly).ThrowOnError();
		nativeCookie.get_IsSecure(ref isSecure).ThrowOnError();
		nativeCookie.get_IsSession(ref isSession).ThrowOnError();
		nativeCookie.get_SameSite(ref sameSite).ThrowOnError();

		return new CoreWebView2Cookie(
			name.ToString() ?? string.Empty,
			value.ToString() ?? string.Empty,
			domain.ToString() ?? string.Empty,
			path.ToString() ?? "/")
		{
			Expires = isSession.Value != 0 ? -1d : expires,
			IsHttpOnly = isHttpOnly.Value != 0,
			IsSecure = isSecure.Value != 0,
			SameSite = (CoreWebView2CookieSameSiteKind)(int)sameSite,
		};
	}

	private WebView2.ICoreWebView2PrintSettings CreatePrintSettings(CoreWebView2PrintSettings? settings)
	{
		_nativeWebView.get_Environment(out var environment).ThrowOnError();
		if (environment is not WebView2.ICoreWebView2Environment10 environment10)
		{
			throw new NotSupportedException("The installed WebView2 runtime does not support print settings.");
		}

		environment10.CreatePrintSettings(out var nativeSettings).ThrowOnError();
		if (settings is null)
		{
			return nativeSettings;
		}

		nativeSettings.put_Orientation((WebView2.COREWEBVIEW2_PRINT_ORIENTATION)(int)settings.Orientation).ThrowOnError();
		nativeSettings.put_ScaleFactor(settings.ScaleFactor).ThrowOnError();
		nativeSettings.put_MarginTop(settings.MarginTop).ThrowOnError();
		nativeSettings.put_MarginBottom(settings.MarginBottom).ThrowOnError();
		nativeSettings.put_MarginLeft(settings.MarginLeft).ThrowOnError();
		nativeSettings.put_MarginRight(settings.MarginRight).ThrowOnError();
		nativeSettings.put_PageWidth(settings.PageWidth).ThrowOnError();
		nativeSettings.put_PageHeight(settings.PageHeight).ThrowOnError();
		nativeSettings.put_ShouldPrintBackgrounds(settings.ShouldPrintBackgrounds ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		nativeSettings.put_ShouldPrintHeaderAndFooter(settings.ShouldPrintHeaderAndFooter ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		nativeSettings.put_ShouldPrintSelectionOnly(settings.ShouldPrintSelectionOnly ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
		SetNativeString(settings.FooterUri, nativeSettings.put_FooterUri);
		SetNativeString(settings.HeaderTitle, nativeSettings.put_HeaderTitle);

		if (nativeSettings is not WebView2.ICoreWebView2PrintSettings2 nativeSettings2)
		{
			throw new NotSupportedException("The installed WebView2 runtime does not support the requested print settings.");
		}

		nativeSettings2.put_Copies(settings.Copies).ThrowOnError();
		nativeSettings2.put_Collation((WebView2.COREWEBVIEW2_PRINT_COLLATION)(int)settings.Collation).ThrowOnError();
		nativeSettings2.put_ColorMode((WebView2.COREWEBVIEW2_PRINT_COLOR_MODE)(int)settings.ColorMode).ThrowOnError();
		nativeSettings2.put_Duplex((WebView2.COREWEBVIEW2_PRINT_DUPLEX)(int)settings.Duplex).ThrowOnError();
		nativeSettings2.put_MediaSize((WebView2.COREWEBVIEW2_PRINT_MEDIA_SIZE)(int)settings.MediaSize).ThrowOnError();
		nativeSettings2.put_PagesPerSide(settings.PagesPerSide).ThrowOnError();
		SetNativeString(settings.PageRanges, nativeSettings2.put_PageRanges);
		SetNativeString(settings.PrinterName, nativeSettings2.put_PrinterName);
		return nativeSettings;
	}

	private void RemoveEvent(string eventName, Func<HRESULT> remove)
	{
		var result = remove();
		if (result.IsError && this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Unable to unregister the WebView2 {eventName} event: {result.GetException()?.Message ?? result.ToString()}");
		}
	}

	private static unsafe void InvokeWithNativeString(string value, Func<PWSTR, HRESULT> action)
	{
		fixed (char* p_value = value)
		{
			action(new PWSTR(p_value)).ThrowOnError();
		}
	}

	private static unsafe void SetNativeString(string? value, Func<PWSTR, HRESULT> setter)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		fixed (char* p_value = value)
		{
			setter(new PWSTR(p_value)).ThrowOnError();
		}
	}
}

#endif // NET10_0_OR_GREATER
