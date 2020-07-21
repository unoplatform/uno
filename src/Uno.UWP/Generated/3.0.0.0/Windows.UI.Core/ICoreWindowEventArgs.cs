#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICoreWindowEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Handled
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.UI.Core.ICoreWindowEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.ICoreWindowEventArgs.Handled.set
	}
}
