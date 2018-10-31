#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreApplicationViewTitleBar 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ExtendViewIntoTitleBar
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreApplicationViewTitleBar.ExtendViewIntoTitleBar is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.ExtendViewIntoTitleBar");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double Height
		{
			get
			{
				throw new global::System.NotImplementedException("The member double CoreApplicationViewTitleBar.Height is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsVisible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreApplicationViewTitleBar.IsVisible is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double SystemOverlayLeftInset
		{
			get
			{
				throw new global::System.NotImplementedException("The member double CoreApplicationViewTitleBar.SystemOverlayLeftInset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double SystemOverlayRightInset
		{
			get
			{
				throw new global::System.NotImplementedException("The member double CoreApplicationViewTitleBar.SystemOverlayRightInset is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.ExtendViewIntoTitleBar.set
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.ExtendViewIntoTitleBar.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.SystemOverlayLeftInset.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.SystemOverlayRightInset.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.Height.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.LayoutMetricsChanged.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.LayoutMetricsChanged.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.IsVisible.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.IsVisibleChanged.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplicationViewTitleBar.IsVisibleChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Core.CoreApplicationViewTitleBar, object> IsVisibleChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.IsVisibleChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.IsVisibleChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Core.CoreApplicationViewTitleBar, object> LayoutMetricsChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.LayoutMetricsChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.LayoutMetricsChanged");
			}
		}
		#endif
	}
}
