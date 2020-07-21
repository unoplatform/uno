#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWindowResizeManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ShouldWaitForLayoutCompletion
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWindowResizeManager.ShouldWaitForLayoutCompletion is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindowResizeManager", "bool CoreWindowResizeManager.ShouldWaitForLayoutCompletion");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void NotifyLayoutCompleted()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreWindowResizeManager", "void CoreWindowResizeManager.NotifyLayoutCompleted()");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreWindowResizeManager.ShouldWaitForLayoutCompletion.set
		// Forced skipping of method Windows.UI.Core.CoreWindowResizeManager.ShouldWaitForLayoutCompletion.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Core.CoreWindowResizeManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member CoreWindowResizeManager CoreWindowResizeManager.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
	}
}
