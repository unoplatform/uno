#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreInputView 
	{
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputView.OcclusionsChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputView.OcclusionsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.ViewManagement.Core.CoreInputViewOcclusion> GetCoreInputViewOcclusions()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<CoreInputViewOcclusion> CoreInputView.GetCoreInputViewOcclusions() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryShowPrimaryView()
		{
			throw new global::System.NotImplementedException("The member bool CoreInputView.TryShowPrimaryView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryHidePrimaryView()
		{
			throw new global::System.NotImplementedException("The member bool CoreInputView.TryHidePrimaryView() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputView.XYFocusTransferringFromPrimaryView.add
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputView.XYFocusTransferringFromPrimaryView.remove
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputView.XYFocusTransferredToPrimaryView.add
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputView.XYFocusTransferredToPrimaryView.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryTransferXYFocusToPrimaryView( global::Windows.Foundation.Rect origin,  global::Windows.UI.ViewManagement.Core.CoreInputViewXYFocusTransferDirection direction)
		{
			throw new global::System.NotImplementedException("The member bool CoreInputView.TryTransferXYFocusToPrimaryView(Rect origin, CoreInputViewXYFocusTransferDirection direction) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryShow()
		{
			throw new global::System.NotImplementedException("The member bool CoreInputView.TryShow() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryShow( global::Windows.UI.ViewManagement.Core.CoreInputViewKind type)
		{
			throw new global::System.NotImplementedException("The member bool CoreInputView.TryShow(CoreInputViewKind type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryHide()
		{
			throw new global::System.NotImplementedException("The member bool CoreInputView.TryHide() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.ViewManagement.Core.CoreInputView GetForUIContext( global::Windows.UI.UIContext context)
		{
			throw new global::System.NotImplementedException("The member CoreInputView CoreInputView.GetForUIContext(UIContext context) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.ViewManagement.Core.CoreInputView GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member CoreInputView CoreInputView.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.Core.CoreInputView, global::Windows.UI.ViewManagement.Core.CoreInputViewOcclusionsChangedEventArgs> OcclusionsChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputView", "event TypedEventHandler<CoreInputView, CoreInputViewOcclusionsChangedEventArgs> CoreInputView.OcclusionsChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputView", "event TypedEventHandler<CoreInputView, CoreInputViewOcclusionsChangedEventArgs> CoreInputView.OcclusionsChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.Core.CoreInputView, object> XYFocusTransferredToPrimaryView
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputView", "event TypedEventHandler<CoreInputView, object> CoreInputView.XYFocusTransferredToPrimaryView");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputView", "event TypedEventHandler<CoreInputView, object> CoreInputView.XYFocusTransferredToPrimaryView");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.Core.CoreInputView, global::Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs> XYFocusTransferringFromPrimaryView
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputView", "event TypedEventHandler<CoreInputView, CoreInputViewTransferringXYFocusEventArgs> CoreInputView.XYFocusTransferringFromPrimaryView");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputView", "event TypedEventHandler<CoreInputView, CoreInputViewTransferringXYFocusEventArgs> CoreInputView.XYFocusTransferringFromPrimaryView");
			}
		}
		#endif
	}
}
