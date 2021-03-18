#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppDiagnosticInfoWatcherEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.AppDiagnosticInfo AppDiagnosticInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppDiagnosticInfo AppDiagnosticInfoWatcherEventArgs.AppDiagnosticInfo is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.AppDiagnosticInfoWatcherEventArgs.AppDiagnosticInfo.get
	}
}
