#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.DevicePortal
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DevicePortalConnectionClosedReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceLimitsExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtocolError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAuthorized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserNotPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServiceTerminated,
		#endif
	}
	#endif
}
