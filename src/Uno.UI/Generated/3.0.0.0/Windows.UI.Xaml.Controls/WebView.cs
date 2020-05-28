#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET461 || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebView 
	{
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  global::System.Uri Source
		{
			get
			{
				return (global::System.Uri)this.GetValue(SourceProperty);
			}
			set
			{
				this.SetValue(SourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::System.Uri> AllowedScriptNotifyUris
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::System.Uri>)this.GetValue(AllowedScriptNotifyUrisProperty);
			}
			set
			{
				this.SetValue(AllowedScriptNotifyUrisProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackage DataTransferPackage
		{
			get
			{
				return (global::Windows.ApplicationModel.DataTransfer.DataPackage)this.GetValue(DataTransferPackageProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color DefaultBackgroundColor
		{
			get
			{
				return (global::Windows.UI.Color)this.GetValue(DefaultBackgroundColorProperty);
			}
			set
			{
				this.SetValue(DefaultBackgroundColorProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  bool CanGoBack
		{
			get
			{
				return (bool)this.GetValue(CanGoBackProperty);
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  bool CanGoForward
		{
			get
			{
				return (bool)this.GetValue(CanGoForwardProperty);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  string DocumentTitle
		{
			get
			{
				return (string)this.GetValue(DocumentTitleProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ContainsFullScreenElement
		{
			get
			{
				return (bool)this.GetValue(ContainsFullScreenElementProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.WebViewDeferredPermissionRequest> DeferredPermissionRequests
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<WebViewDeferredPermissionRequest> WebView.DeferredPermissionRequests is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.WebViewExecutionMode ExecutionMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewExecutionMode WebView.ExecutionMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.WebViewSettings Settings
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewSettings WebView.Settings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AllowedScriptNotifyUrisProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(AllowedScriptNotifyUris), typeof(global::System.Collections.Generic.IList<global::System.Uri>), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::System.Uri>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::System.Collections.Generic.IList<global::System.Uri> AnyScriptNotifyUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<Uri> WebView.AnyScriptNotifyUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DataTransferPackageProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DataTransferPackage), typeof(global::Windows.ApplicationModel.DataTransfer.DataPackage), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(global::Windows.ApplicationModel.DataTransfer.DataPackage)));
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Source), typeof(global::System.Uri), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(global::System.Uri)));
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CanGoBackProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanGoBack), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CanGoForwardProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CanGoForward), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DefaultBackgroundColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DefaultBackgroundColor), typeof(global::Windows.UI.Color), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DocumentTitleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DocumentTitle), typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContainsFullScreenElementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ContainsFullScreenElement), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.WebView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.WebViewExecutionMode DefaultExecutionMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewExecutionMode WebView.DefaultExecutionMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public WebView( global::Windows.UI.Xaml.Controls.WebViewExecutionMode executionMode) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "WebView.WebView(WebViewExecutionMode executionMode)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.WebView(Windows.UI.Xaml.Controls.WebViewExecutionMode)
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public WebView() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "WebView.WebView()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.WebView()
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.Source.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.Source.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.AllowedScriptNotifyUris.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.AllowedScriptNotifyUris.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DataTransferPackage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.LoadCompleted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.LoadCompleted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ScriptNotify.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ScriptNotify.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NavigationFailed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NavigationFailed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string InvokeScript( string scriptName,  string[] arguments)
		{
			throw new global::System.NotImplementedException("The member string WebView.InvokeScript(string scriptName, string[] arguments) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  void Navigate( global::System.Uri source)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.Navigate(Uri source)");
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  void NavigateToString( string text)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.NavigateToString(string text)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.CanGoBack.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.CanGoForward.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DocumentTitle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NavigationStarting.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NavigationStarting.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ContentLoading.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ContentLoading.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DOMContentLoaded.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DOMContentLoaded.remove
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  void GoForward()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.GoForward()");
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  void GoBack()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.GoBack()");
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  void Refresh()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.Refresh()");
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction CapturePreviewToStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebView.CapturePreviewToStreamAsync(IRandomAccessStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<string> InvokeScriptAsync( string scriptName,  global::System.Collections.Generic.IEnumerable<string> arguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> WebView.InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackage> CaptureSelectedContentToDataPackageAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DataPackage> WebView.CaptureSelectedContentToDataPackageAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void NavigateToLocalStreamUri( global::System.Uri source,  global::Windows.Web.IUriToStreamResolver streamResolver)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.NavigateToLocalStreamUri(Uri source, IUriToStreamResolver streamResolver)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Uri BuildLocalStreamUri( string contentIdentifier,  string relativePath)
		{
			throw new global::System.NotImplementedException("The member Uri WebView.BuildLocalStreamUri(string contentIdentifier, string relativePath) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DefaultBackgroundColor.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DefaultBackgroundColor.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NavigationCompleted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NavigationCompleted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameNavigationStarting.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameNavigationStarting.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameContentLoading.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameContentLoading.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameDOMContentLoaded.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameDOMContentLoaded.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameNavigationCompleted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.FrameNavigationCompleted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.LongRunningScriptDetected.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.LongRunningScriptDetected.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.UnsafeContentWarningDisplaying.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.UnsafeContentWarningDisplaying.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.UnviewableContentIdentified.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.UnviewableContentIdentified.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void NavigateWithHttpRequestMessage( global::Windows.Web.Http.HttpRequestMessage requestMessage)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.NavigateWithHttpRequestMessage(HttpRequestMessage requestMessage)");
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  bool Focus( global::Windows.UI.Xaml.FocusState value)
		{
			throw new global::System.NotImplementedException("The member bool WebView.Focus(FocusState value) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ContainsFullScreenElement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ContainsFullScreenElementChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ContainsFullScreenElementChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ExecutionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DeferredPermissionRequests.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.Settings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.UnsupportedUriSchemeIdentified.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.UnsupportedUriSchemeIdentified.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NewWindowRequested.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.NewWindowRequested.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.PermissionRequested.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.PermissionRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void AddWebAllowedObject( string name,  object pObject)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "void WebView.AddWebAllowedObject(string name, object pObject)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.WebViewDeferredPermissionRequest DeferredPermissionRequestById( uint id)
		{
			throw new global::System.NotImplementedException("The member WebViewDeferredPermissionRequest WebView.DeferredPermissionRequestById(uint id) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusLeft.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusLeft.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusRight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusRight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusUp.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusUp.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusDown.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusDown.set
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.SeparateProcessLost.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.SeparateProcessLost.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.WebResourceRequested.add
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.WebResourceRequested.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusLeftProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusRightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusUpProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.XYFocusDownProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DefaultExecutionMode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncAction ClearTemporaryWebDataAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebView.ClearTemporaryWebDataAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.ContainsFullScreenElementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.CanGoBackProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.CanGoForwardProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DocumentTitleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DefaultBackgroundColorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.AnyScriptNotifyUri.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.SourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.AllowedScriptNotifyUrisProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebView.DataTransferPackageProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Navigation.LoadCompletedEventHandler LoadCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event LoadCompletedEventHandler WebView.LoadCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event LoadCompletedEventHandler WebView.LoadCompleted");
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.WebViewNavigationFailedEventHandler NavigationFailed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event WebViewNavigationFailedEventHandler WebView.NavigationFailed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event WebViewNavigationFailedEventHandler WebView.NavigationFailed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.NotifyEventHandler ScriptNotify
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event NotifyEventHandler WebView.ScriptNotify");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event NotifyEventHandler WebView.ScriptNotify");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewContentLoadingEventArgs> ContentLoading
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewContentLoadingEventArgs> WebView.ContentLoading");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewContentLoadingEventArgs> WebView.ContentLoading");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewDOMContentLoadedEventArgs> DOMContentLoaded
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewDOMContentLoadedEventArgs> WebView.DOMContentLoaded");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewDOMContentLoadedEventArgs> WebView.DOMContentLoaded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewContentLoadingEventArgs> FrameContentLoading
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewContentLoadingEventArgs> WebView.FrameContentLoading");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewContentLoadingEventArgs> WebView.FrameContentLoading");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewDOMContentLoadedEventArgs> FrameDOMContentLoaded
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewDOMContentLoadedEventArgs> WebView.FrameDOMContentLoaded");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewDOMContentLoadedEventArgs> WebView.FrameDOMContentLoaded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewNavigationCompletedEventArgs> FrameNavigationCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> WebView.FrameNavigationCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> WebView.FrameNavigationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs> FrameNavigationStarting
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> WebView.FrameNavigationStarting");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> WebView.FrameNavigationStarting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewLongRunningScriptDetectedEventArgs> LongRunningScriptDetected
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewLongRunningScriptDetectedEventArgs> WebView.LongRunningScriptDetected");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewLongRunningScriptDetectedEventArgs> WebView.LongRunningScriptDetected");
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewNavigationCompletedEventArgs> NavigationCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> WebView.NavigationCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> WebView.NavigationCompleted");
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs> NavigationStarting
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> WebView.NavigationStarting");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> WebView.NavigationStarting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, object> UnsafeContentWarningDisplaying
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, object> WebView.UnsafeContentWarningDisplaying");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, object> WebView.UnsafeContentWarningDisplaying");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewUnviewableContentIdentifiedEventArgs> UnviewableContentIdentified
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewUnviewableContentIdentifiedEventArgs> WebView.UnviewableContentIdentified");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewUnviewableContentIdentifiedEventArgs> WebView.UnviewableContentIdentified");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, object> ContainsFullScreenElementChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, object> WebView.ContainsFullScreenElementChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, object> WebView.ContainsFullScreenElementChanged");
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewNewWindowRequestedEventArgs> NewWindowRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNewWindowRequestedEventArgs> WebView.NewWindowRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewNewWindowRequestedEventArgs> WebView.NewWindowRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewPermissionRequestedEventArgs> PermissionRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewPermissionRequestedEventArgs> WebView.PermissionRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewPermissionRequestedEventArgs> WebView.PermissionRequested");
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewUnsupportedUriSchemeIdentifiedEventArgs> WebView.UnsupportedUriSchemeIdentified");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewUnsupportedUriSchemeIdentifiedEventArgs> WebView.UnsupportedUriSchemeIdentified");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewSeparateProcessLostEventArgs> SeparateProcessLost
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewSeparateProcessLostEventArgs> WebView.SeparateProcessLost");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewSeparateProcessLostEventArgs> WebView.SeparateProcessLost");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.WebView, global::Windows.UI.Xaml.Controls.WebViewWebResourceRequestedEventArgs> WebResourceRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewWebResourceRequestedEventArgs> WebView.WebResourceRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebView", "event TypedEventHandler<WebView, WebViewWebResourceRequestedEventArgs> WebView.WebResourceRequested");
			}
		}
		#endif
	}
}
