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
				throw new global::System.NotImplementedException("The member CastingConnectionErrorStatus CastingConnectionErrorOccurredEventArgs.ErrorStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CastingConnectionErrorStatus%20CastingConnectionErrorOccurredEventArgs.ErrorStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CastingConnectionErrorOccurredEventArgs.Message is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CastingConnectionErrorOccurredEventArgs.Message");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Casting.CastingConnectionErrorOccurredEventArgs.ErrorStatus.get
		// Forced skipping of method Windows.Media.Casting.CastingConnectionErrorOccurredEventArgs.Message.get
	}
}
