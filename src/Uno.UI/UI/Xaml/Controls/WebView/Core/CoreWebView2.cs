namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public uint BrowserProcessId
	{
		get
		{
			throw new global::System.NotImplementedException("The member uint CoreWebView2.BrowserProcessId is not implemented in Uno.");
		}
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool ContainsFullScreenElement
	{
		get
		{
			throw new global::System.NotImplementedException("The member bool CoreWebView2.ContainsFullScreenElement is not implemented in Uno.");
		}
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Microsoft.Web.WebView2.Core.CoreWebView2Settings Settings
	{
		get
		{
			throw new global::System.NotImplementedException("The member CoreWebView2Settings CoreWebView2.Settings is not implemented in Uno.");
		}
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Microsoft.Web.WebView2.Core.CoreWebView2CookieManager CookieManager
	{
		get
		{
			throw new global::System.NotImplementedException("The member CoreWebView2CookieManager CoreWebView2.CookieManager is not implemented in Uno.");
		}
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Microsoft.Web.WebView2.Core.CoreWebView2Environment Environment
	{
		get
		{
			throw new global::System.NotImplementedException("The member CoreWebView2Environment CoreWebView2.Environment is not implemented in Uno.");
		}
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool IsSuspended
	{
		get
		{
			throw new global::System.NotImplementedException("The member bool CoreWebView2.IsSuspended is not implemented in Uno.");
		}
	}
#endif
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.CookieManager.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.Environment.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceResponseReceived.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceResponseReceived.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.DOMContentLoaded.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.DOMContentLoaded.remove
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void NavigateWithWebResourceRequest(global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequest Request)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.NavigateWithWebResourceRequest(CoreWebView2WebResourceRequest Request)");
	}
#endif
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.IsSuspended.get
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Windows.Foundation.IAsyncOperation<bool> TrySuspendAsync()
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CoreWebView2.TrySuspendAsync() is not implemented in Uno.");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Resume()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.Resume()");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, global::Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind accessKind)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void ClearVirtualHostNameToFolderMapping(string hostName)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.ClearVirtualHostNameToFolderMapping(string hostName)");
	}
#endif
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.Settings.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.Source.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.BrowserProcessId.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.CanGoBack.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.CanGoForward.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.DocumentTitle.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ContainsFullScreenElement.get
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.NavigationStarting.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.NavigationStarting.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ContentLoading.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ContentLoading.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.SourceChanged.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.SourceChanged.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.HistoryChanged.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.HistoryChanged.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.NavigationCompleted.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.NavigationCompleted.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.FrameNavigationStarting.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.FrameNavigationStarting.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.FrameNavigationCompleted.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.FrameNavigationCompleted.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ScriptDialogOpening.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ScriptDialogOpening.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.PermissionRequested.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.PermissionRequested.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ProcessFailed.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ProcessFailed.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WebMessageReceived.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WebMessageReceived.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.NewWindowRequested.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.NewWindowRequested.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.DocumentTitleChanged.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.DocumentTitleChanged.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ContainsFullScreenElementChanged.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.ContainsFullScreenElementChanged.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested.remove
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WindowCloseRequested.add
	// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2.WindowCloseRequested.remove
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Navigate(string uri)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.Navigate(string uri)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void NavigateToString(string htmlContent)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.NavigateToString(string htmlContent)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Windows.Foundation.IAsyncOperation<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<string> CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(string javaScript) is not implemented in Uno.");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void RemoveScriptToExecuteOnDocumentCreated(string id)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(string id)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Windows.Foundation.IAsyncOperation<string> ExecuteScriptAsync(string javaScript)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<string> CoreWebView2.ExecuteScriptAsync(string javaScript) is not implemented in Uno.");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Windows.Foundation.IAsyncAction CapturePreviewAsync(global::Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat imageFormat, global::Windows.Storage.Streams.IRandomAccessStream imageStream)
	{
		throw new global::System.NotImplementedException("The member IAsyncAction CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat imageFormat, IRandomAccessStream imageStream) is not implemented in Uno.");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Reload()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.Reload()");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void PostWebMessageAsJson(string webMessageAsJson)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.PostWebMessageAsJson(string webMessageAsJson)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void PostWebMessageAsString(string webMessageAsString)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.PostWebMessageAsString(string webMessageAsString)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Windows.Foundation.IAsyncOperation<string> CallDevToolsProtocolMethodAsync(string methodName, string parametersAsJson)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<string> CoreWebView2.CallDevToolsProtocolMethodAsync(string methodName, string parametersAsJson) is not implemented in Uno.");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void GoBack()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.GoBack()");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void GoForward()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.GoForward()");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::Microsoft.Web.WebView2.Core.CoreWebView2DevToolsProtocolEventReceiver GetDevToolsProtocolEventReceiver(string eventName)
	{
		throw new global::System.NotImplementedException("The member CoreWebView2DevToolsProtocolEventReceiver CoreWebView2.GetDevToolsProtocolEventReceiver(string eventName) is not implemented in Uno.");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Stop()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.Stop()");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void AddHostObjectToScript(string name, object rawObject)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.AddHostObjectToScript(string name, object rawObject)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void RemoveHostObjectFromScript(string name)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.RemoveHostObjectFromScript(string name)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void OpenDevToolsWindow()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.OpenDevToolsWindow()");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void AddWebResourceRequestedFilter(string uri, global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext ResourceContext)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext ResourceContext)");
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void RemoveWebResourceRequestedFilter(string uri, global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext ResourceContext)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2", "void CoreWebView2.RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext ResourceContext)");
	}
#endif

}
