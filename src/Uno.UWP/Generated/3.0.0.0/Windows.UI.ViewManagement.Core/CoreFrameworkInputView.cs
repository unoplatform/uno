#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreFrameworkInputView 
	{
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputView.PrimaryViewAnimationStarting.add
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputView.PrimaryViewAnimationStarting.remove
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputView.OcclusionsChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputView.OcclusionsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.ViewManagement.Core.CoreFrameworkInputView GetForUIContext( global::Windows.UI.UIContext context)
		{
			throw new global::System.NotImplementedException("The member CoreFrameworkInputView CoreFrameworkInputView.GetForUIContext(UIContext context) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreFrameworkInputView%20CoreFrameworkInputView.GetForUIContext%28UIContext%20context%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.ViewManagement.Core.CoreFrameworkInputView GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member CoreFrameworkInputView CoreFrameworkInputView.GetForCurrentView() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreFrameworkInputView%20CoreFrameworkInputView.GetForCurrentView%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.Core.CoreFrameworkInputView, global::Windows.UI.ViewManagement.Core.CoreFrameworkInputViewOcclusionsChangedEventArgs> OcclusionsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreFrameworkInputView", "event TypedEventHandler<CoreFrameworkInputView, CoreFrameworkInputViewOcclusionsChangedEventArgs> CoreFrameworkInputView.OcclusionsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreFrameworkInputView", "event TypedEventHandler<CoreFrameworkInputView, CoreFrameworkInputViewOcclusionsChangedEventArgs> CoreFrameworkInputView.OcclusionsChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.Core.CoreFrameworkInputView, global::Windows.UI.ViewManagement.Core.CoreFrameworkInputViewAnimationStartingEventArgs> PrimaryViewAnimationStarting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreFrameworkInputView", "event TypedEventHandler<CoreFrameworkInputView, CoreFrameworkInputViewAnimationStartingEventArgs> CoreFrameworkInputView.PrimaryViewAnimationStarting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreFrameworkInputView", "event TypedEventHandler<CoreFrameworkInputView, CoreFrameworkInputViewAnimationStartingEventArgs> CoreFrameworkInputView.PrimaryViewAnimationStarting");
			}
		}
		#endif
	}
}
