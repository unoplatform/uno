#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreApplicationView 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreWindow CoreWindow
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWindow CoreApplicationView.CoreWindow is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsHosted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreApplicationView.IsHosted is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsMain
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreApplicationView.IsMain is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Dispatcher
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComponent
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreApplicationView.IsComponent is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property TitleBar
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet CoreApplicationView.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue CoreApplicationView.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.CoreWindow.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.Activated.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.Activated.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.IsMain.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.IsHosted.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.Dispatcher.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.IsComponent.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.TitleBar.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.HostedViewClosing.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.HostedViewClosing.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.Properties.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationView.DispatcherQueue.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Core.CoreApplicationView, global::Windows.ApplicationModel.Activation.IActivatedEventArgs> Activated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationView", "event TypedEventHandler<CoreApplicationView, IActivatedEventArgs> CoreApplicationView.Activated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationView", "event TypedEventHandler<CoreApplicationView, IActivatedEventArgs> CoreApplicationView.Activated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Core.CoreApplicationView, global::Windows.ApplicationModel.Core.HostedViewClosingEventArgs> HostedViewClosing
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationView", "event TypedEventHandler<CoreApplicationView, HostedViewClosingEventArgs> CoreApplicationView.HostedViewClosing");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationView", "event TypedEventHandler<CoreApplicationView, HostedViewClosingEventArgs> CoreApplicationView.HostedViewClosing");
			}
		}
		#endif
	}
}
