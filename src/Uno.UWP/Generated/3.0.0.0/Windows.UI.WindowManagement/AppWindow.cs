#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppWindow 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppWindow.Title is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "string AppWindow.Title");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PersistedStateId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppWindow.PersistedStateId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "string AppWindow.PersistedStateId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.UIContentRoot Content
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIContentRoot AppWindow.Content is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue AppWindow.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.AppWindowFrame Frame
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppWindowFrame AppWindow.Frame is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVisible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppWindow.IsVisible is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.AppWindowPresenter Presenter
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppWindowPresenter AppWindow.Presenter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.AppWindowTitleBar TitleBar
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppWindowTitleBar AppWindow.TitleBar is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.UIContext UIContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIContext AppWindow.UIContext is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.WindowingEnvironment WindowingEnvironment
		{
			get
			{
				throw new global::System.NotImplementedException("The member WindowingEnvironment AppWindow.WindowingEnvironment is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Content.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.DispatcherQueue.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Frame.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.IsVisible.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.PersistedStateId.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.PersistedStateId.set
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Presenter.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Title.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Title.set
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.TitleBar.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.UIContext.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.WindowingEnvironment.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CloseAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppWindow.CloseAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.AppWindowPlacement GetPlacement()
		{
			throw new global::System.NotImplementedException("The member AppWindowPlacement AppWindow.GetPlacement() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.WindowManagement.DisplayRegion> GetDisplayRegions()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<DisplayRegion> AppWindow.GetDisplayRegions() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestMoveToDisplayRegion( global::Windows.UI.WindowManagement.DisplayRegion displayRegion)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestMoveToDisplayRegion(DisplayRegion displayRegion)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestMoveAdjacentToCurrentView()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestMoveAdjacentToCurrentView()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestMoveAdjacentToWindow( global::Windows.UI.WindowManagement.AppWindow anchorWindow)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestMoveAdjacentToWindow(AppWindow anchorWindow)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestMoveRelativeToWindowContent( global::Windows.UI.WindowManagement.AppWindow anchorWindow,  global::Windows.Foundation.Point contentOffset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestMoveRelativeToWindowContent(AppWindow anchorWindow, Point contentOffset)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestMoveRelativeToCurrentViewContent( global::Windows.Foundation.Point contentOffset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestMoveRelativeToCurrentViewContent(Point contentOffset)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestMoveRelativeToDisplayRegion( global::Windows.UI.WindowManagement.DisplayRegion displayRegion,  global::Windows.Foundation.Point displayRegionOffset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestMoveRelativeToDisplayRegion(DisplayRegion displayRegion, Point displayRegionOffset)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestSize( global::Windows.Foundation.Size frameSize)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.RequestSize(Size frameSize)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryShowAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppWindow.TryShowAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Changed.add
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Changed.remove
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Closed.add
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.Closed.remove
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.CloseRequested.add
		// Forced skipping of method Windows.UI.WindowManagement.AppWindow.CloseRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.UI.WindowManagement.AppWindow> TryCreateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppWindow> AppWindow.TryCreateAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ClearAllPersistedState()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.ClearAllPersistedState()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ClearPersistedState( string key)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "void AppWindow.ClearPersistedState(string key)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WindowManagement.AppWindow, global::Windows.UI.WindowManagement.AppWindowChangedEventArgs> Changed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "event TypedEventHandler<AppWindow, AppWindowChangedEventArgs> AppWindow.Changed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "event TypedEventHandler<AppWindow, AppWindowChangedEventArgs> AppWindow.Changed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WindowManagement.AppWindow, global::Windows.UI.WindowManagement.AppWindowCloseRequestedEventArgs> CloseRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "event TypedEventHandler<AppWindow, AppWindowCloseRequestedEventArgs> AppWindow.CloseRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "event TypedEventHandler<AppWindow, AppWindowCloseRequestedEventArgs> AppWindow.CloseRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WindowManagement.AppWindow, global::Windows.UI.WindowManagement.AppWindowClosedEventArgs> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "event TypedEventHandler<AppWindow, AppWindowClosedEventArgs> AppWindow.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindow", "event TypedEventHandler<AppWindow, AppWindowClosedEventArgs> AppWindow.Closed");
			}
		}
		#endif
	}
}
