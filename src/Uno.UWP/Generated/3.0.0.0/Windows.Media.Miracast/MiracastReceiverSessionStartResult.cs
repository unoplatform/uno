#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverSessionStartResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception MiracastReceiverSessionStartResult.ExtendedError is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20MiracastReceiverSessionStartResult.ExtendedError");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverSessionStartStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverSessionStartStatus MiracastReceiverSessionStartResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MiracastReceiverSessionStartStatus%20MiracastReceiverSessionStartResult.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSessionStartResult.Status.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSessionStartResult.ExtendedError.get
	}
}
