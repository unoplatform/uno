#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICoreInputSourceBase 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Core.CoreDispatcher Dispatcher
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsInputEnabled
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.UI.Core.ICoreInputSourceBase.Dispatcher.get
		// Forced skipping of method Windows.UI.Core.ICoreInputSourceBase.IsInputEnabled.get
		// Forced skipping of method Windows.UI.Core.ICoreInputSourceBase.IsInputEnabled.set
		// Forced skipping of method Windows.UI.Core.ICoreInputSourceBase.InputEnabled.add
		// Forced skipping of method Windows.UI.Core.ICoreInputSourceBase.InputEnabled.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.InputEnabledEventArgs> InputEnabled;
		#endif
	}
}
