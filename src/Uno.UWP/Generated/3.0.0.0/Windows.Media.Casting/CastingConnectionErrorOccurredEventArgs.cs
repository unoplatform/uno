#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Casting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CastingConnectionErrorOccurredEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Casting.CastingConnectionErrorStatus ErrorStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CastingConnectionErrorStatus CastingConnectionErrorOccurredEventArgs.ErrorStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CastingConnectionErrorOccurredEventArgs.Message is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Casting.CastingConnectionErrorOccurredEventArgs.ErrorStatus.get
		// Forced skipping of method Windows.Media.Casting.CastingConnectionErrorOccurredEventArgs.Message.get
	}
}
