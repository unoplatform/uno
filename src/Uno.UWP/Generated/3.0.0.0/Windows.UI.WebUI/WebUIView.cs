#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebUIView : global::Windows.Web.UI.IWebViewControl,global::Windows.Web.UI.IWebViewControl2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IgnoreApplicationContentUriRulesNavigationRestrictions
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebUIView.IgnoreApplicationContentUriRulesNavigationRestrictions is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "bool WebUIView.IgnoreApplicationContentUriRulesNavigationRestrictions");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ApplicationViewId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int WebUIView.ApplicationViewId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebUIView.Source is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "Uri WebUIView.Source");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Color DefaultBackgroundColor
		{
			get
			{
				throw new global::System.NotImplementedException("The member Color WebUIView.DefaultBackgroundColor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "Color WebUIView.DefaultBackgroundColor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanGoBack
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebUIView.CanGoBack is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanGoForward
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebUIView.CanGoForward is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ContainsFullScreenElement
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebUIView.ContainsFullScreenElement is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Web.UI.WebViewControlDeferredPermissionRequest> DeferredPermissionRequests
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<WebViewControlDeferredPermissionRequest> WebUIView.DeferredPermissionRequests is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DocumentTitle
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebUIView.DocumentTitle is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.UI.WebViewControlSettings Settings
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewControlSettings WebUIView.Settings is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ApplicationViewId.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Closed.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Closed.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Activated.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Activated.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.IgnoreApplicationContentUriRulesNavigationRestrictions.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.IgnoreApplicationContentUriRulesNavigationRestrictions.set
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Source.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Source.set
		// Forced skipping of method Windows.UI.WebUI.WebUIView.DocumentTitle.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.CanGoBack.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.CanGoForward.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.DefaultBackgroundColor.set
		// Forced skipping of method Windows.UI.WebUI.WebUIView.DefaultBackgroundColor.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ContainsFullScreenElement.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.Settings.get
		// Forced skipping of method Windows.UI.WebUI.WebUIView.DeferredPermissionRequests.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GoForward()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.GoForward()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GoBack()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.GoBack()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Refresh()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.Refresh()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Navigate( global::System.Uri source)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.Navigate(Uri source)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void NavigateToString( string text)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.NavigateToString(string text)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void NavigateToLocalStreamUri( global::System.Uri source,  global::Windows.Web.IUriToStreamResolver streamResolver)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.NavigateToLocalStreamUri(Uri source, IUriToStreamResolver streamResolver)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void NavigateWithHttpRequestMessage( global::Windows.Web.Http.HttpRequestMessage requestMessage)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.NavigateWithHttpRequestMessage(HttpRequestMessage requestMessage)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> InvokeScriptAsync( string scriptName,  global::System.Collections.Generic.IEnumerable<string> arguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> WebUIView.InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CapturePreviewToStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebUIView.CapturePreviewToStreamAsync(IRandomAccessStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackage> CaptureSelectedContentToDataPackageAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DataPackage> WebUIView.CaptureSelectedContentToDataPackageAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri BuildLocalStreamUri( string contentIdentifier,  string relativePath)
		{
			throw new global::System.NotImplementedException("The member Uri WebUIView.BuildLocalStreamUri(string contentIdentifier, string relativePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GetDeferredPermissionRequestById( uint id, out global::Windows.Web.UI.WebViewControlDeferredPermissionRequest result)
		{
			throw new global::System.NotImplementedException("The member void WebUIView.GetDeferredPermissionRequestById(uint id, out WebViewControlDeferredPermissionRequest result) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIView.NavigationStarting.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.NavigationStarting.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ContentLoading.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ContentLoading.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.DOMContentLoaded.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.DOMContentLoaded.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.NavigationCompleted.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.NavigationCompleted.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameNavigationStarting.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameNavigationStarting.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameContentLoading.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameContentLoading.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameDOMContentLoaded.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameDOMContentLoaded.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameNavigationCompleted.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.FrameNavigationCompleted.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ScriptNotify.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ScriptNotify.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.LongRunningScriptDetected.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.LongRunningScriptDetected.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.UnsafeContentWarningDisplaying.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.UnsafeContentWarningDisplaying.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.UnviewableContentIdentified.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.UnviewableContentIdentified.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.PermissionRequested.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.PermissionRequested.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.UnsupportedUriSchemeIdentified.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.UnsupportedUriSchemeIdentified.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.NewWindowRequested.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.NewWindowRequested.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ContainsFullScreenElementChanged.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.ContainsFullScreenElementChanged.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIView.WebResourceRequested.add
		// Forced skipping of method Windows.UI.WebUI.WebUIView.WebResourceRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddInitializeScript( string script)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "void WebUIView.AddInitializeScript(string script)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.UI.WebUI.WebUIView> CreateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebUIView> WebUIView.CreateAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.UI.WebUI.WebUIView> CreateAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebUIView> WebUIView.CreateAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WebUI.WebUIView, global::Windows.ApplicationModel.Activation.IActivatedEventArgs> Activated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<WebUIView, IActivatedEventArgs> WebUIView.Activated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<WebUIView, IActivatedEventArgs> WebUIView.Activated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WebUI.WebUIView, object> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<WebUIView, object> WebUIView.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<WebUIView, object> WebUIView.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, object> ContainsFullScreenElementChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, object> WebUIView.ContainsFullScreenElementChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, object> WebUIView.ContainsFullScreenElementChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlContentLoadingEventArgs> ContentLoading
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlContentLoadingEventArgs> WebUIView.ContentLoading");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlContentLoadingEventArgs> WebUIView.ContentLoading");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlDOMContentLoadedEventArgs> DOMContentLoaded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlDOMContentLoadedEventArgs> WebUIView.DOMContentLoaded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlDOMContentLoadedEventArgs> WebUIView.DOMContentLoaded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlContentLoadingEventArgs> FrameContentLoading
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlContentLoadingEventArgs> WebUIView.FrameContentLoading");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlContentLoadingEventArgs> WebUIView.FrameContentLoading");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlDOMContentLoadedEventArgs> FrameDOMContentLoaded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlDOMContentLoadedEventArgs> WebUIView.FrameDOMContentLoaded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlDOMContentLoadedEventArgs> WebUIView.FrameDOMContentLoaded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationCompletedEventArgs> FrameNavigationCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationCompletedEventArgs> WebUIView.FrameNavigationCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationCompletedEventArgs> WebUIView.FrameNavigationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationStartingEventArgs> FrameNavigationStarting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationStartingEventArgs> WebUIView.FrameNavigationStarting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationStartingEventArgs> WebUIView.FrameNavigationStarting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlLongRunningScriptDetectedEventArgs> LongRunningScriptDetected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlLongRunningScriptDetectedEventArgs> WebUIView.LongRunningScriptDetected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlLongRunningScriptDetectedEventArgs> WebUIView.LongRunningScriptDetected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationCompletedEventArgs> NavigationCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationCompletedEventArgs> WebUIView.NavigationCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationCompletedEventArgs> WebUIView.NavigationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationStartingEventArgs> NavigationStarting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationStartingEventArgs> WebUIView.NavigationStarting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNavigationStartingEventArgs> WebUIView.NavigationStarting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs> NewWindowRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNewWindowRequestedEventArgs> WebUIView.NewWindowRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlNewWindowRequestedEventArgs> WebUIView.NewWindowRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlPermissionRequestedEventArgs> PermissionRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlPermissionRequestedEventArgs> WebUIView.PermissionRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlPermissionRequestedEventArgs> WebUIView.PermissionRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlScriptNotifyEventArgs> ScriptNotify
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlScriptNotifyEventArgs> WebUIView.ScriptNotify");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlScriptNotifyEventArgs> WebUIView.ScriptNotify");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, object> UnsafeContentWarningDisplaying
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, object> WebUIView.UnsafeContentWarningDisplaying");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, object> WebUIView.UnsafeContentWarningDisplaying");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlUnsupportedUriSchemeIdentifiedEventArgs> WebUIView.UnsupportedUriSchemeIdentified");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlUnsupportedUriSchemeIdentifiedEventArgs> WebUIView.UnsupportedUriSchemeIdentified");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlUnviewableContentIdentifiedEventArgs> UnviewableContentIdentified
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlUnviewableContentIdentifiedEventArgs> WebUIView.UnviewableContentIdentified");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlUnviewableContentIdentifiedEventArgs> WebUIView.UnviewableContentIdentified");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlWebResourceRequestedEventArgs> WebResourceRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlWebResourceRequestedEventArgs> WebUIView.WebResourceRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIView", "event TypedEventHandler<IWebViewControl, WebViewControlWebResourceRequestedEventArgs> WebUIView.WebResourceRequested");
			}
		}
		#endif
		// Processing: Windows.Web.UI.IWebViewControl
		// Processing: Windows.Web.UI.IWebViewControl2
	}
}
