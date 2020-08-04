#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayStateOperationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception DisplayStateOperationResult.ExtendedErrorCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayStateOperationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayStateOperationStatus DisplayStateOperationResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayStateOperationResult.Status.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayStateOperationResult.ExtendedErrorCode.get
	}
}
