#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebViewControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool CanGoBack
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool CanGoForward
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool ContainsFullScreenElement
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Color DefaultBackgroundColor
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Web.UI.WebViewControlDeferredPermissionRequest> DeferredPermissionRequests
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DocumentTitle
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Web.UI.WebViewControlSettings Settings
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Uri Source
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Web.UI.IWebViewControl.Source.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.Source.set
		// Forced skipping of method Windows.Web.UI.IWebViewControl.DocumentTitle.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.CanGoBack.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.CanGoForward.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.DefaultBackgroundColor.set
		// Forced skipping of method Windows.Web.UI.IWebViewControl.DefaultBackgroundColor.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ContainsFullScreenElement.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.Settings.get
		// Forced skipping of method Windows.Web.UI.IWebViewControl.DeferredPermissionRequests.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void GoForward();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void GoBack();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Refresh();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Stop();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Navigate( global::System.Uri source);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void NavigateToString( string text);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void NavigateToLocalStreamUri( global::System.Uri source,  global::Windows.Web.IUriToStreamResolver streamResolver);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void NavigateWithHttpRequestMessage( global::Windows.Web.Http.HttpRequestMessage requestMessage);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<string> InvokeScriptAsync( string scriptName,  global::System.Collections.Generic.IEnumerable<string> arguments);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncAction CapturePreviewToStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream stream);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackage> CaptureSelectedContentToDataPackageAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Uri BuildLocalStreamUri( string contentIdentifier,  string relativePath);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void GetDeferredPermissionRequestById( uint id, out global::Windows.Web.UI.WebViewControlDeferredPermissionRequest result);
		#endif
		// Forced skipping of method Windows.Web.UI.IWebViewControl.NavigationStarting.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.NavigationStarting.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ContentLoading.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ContentLoading.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.DOMContentLoaded.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.DOMContentLoaded.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.NavigationCompleted.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.NavigationCompleted.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameNavigationStarting.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameNavigationStarting.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameContentLoading.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameContentLoading.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameDOMContentLoaded.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameDOMContentLoaded.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameNavigationCompleted.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.FrameNavigationCompleted.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ScriptNotify.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ScriptNotify.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.LongRunningScriptDetected.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.LongRunningScriptDetected.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.UnsafeContentWarningDisplaying.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.UnsafeContentWarningDisplaying.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.UnviewableContentIdentified.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.UnviewableContentIdentified.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.PermissionRequested.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.PermissionRequested.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.UnsupportedUriSchemeIdentified.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.UnsupportedUriSchemeIdentified.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.NewWindowRequested.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.NewWindowRequested.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ContainsFullScreenElementChanged.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.ContainsFullScreenElementChanged.remove
		// Forced skipping of method Windows.Web.UI.IWebViewControl.WebResourceRequested.add
		// Forced skipping of method Windows.Web.UI.IWebViewControl.WebResourceRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, object> ContainsFullScreenElementChanged;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlContentLoadingEventArgs> ContentLoading;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlDOMContentLoadedEventArgs> DOMContentLoaded;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlContentLoadingEventArgs> FrameContentLoading;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlDOMContentLoadedEventArgs> FrameDOMContentLoaded;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationCompletedEventArgs> FrameNavigationCompleted;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationStartingEventArgs> FrameNavigationStarting;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlLongRunningScriptDetectedEventArgs> LongRunningScriptDetected;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationCompletedEventArgs> NavigationCompleted;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNavigationStartingEventArgs> NavigationStarting;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlNewWindowRequestedEventArgs> NewWindowRequested;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlPermissionRequestedEventArgs> PermissionRequested;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlScriptNotifyEventArgs> ScriptNotify;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, object> UnsafeContentWarningDisplaying;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlUnviewableContentIdentifiedEventArgs> UnviewableContentIdentified;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Web.UI.IWebViewControl, global::Windows.Web.UI.WebViewControlWebResourceRequestedEventArgs> WebResourceRequested;
		#endif
	}
}
