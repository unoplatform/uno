#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UnifiedPosErrorData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ExtendedReason
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UnifiedPosErrorData.ExtendedReason is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UnifiedPosErrorData.Message is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.UnifiedPosErrorReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member UnifiedPosErrorReason UnifiedPosErrorData.Reason is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.UnifiedPosErrorSeverity Severity
		{
			get
			{
				throw new global::System.NotImplementedException("The member UnifiedPosErrorSeverity UnifiedPosErrorData.Severity is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UnifiedPosErrorData( string message,  global::Windows.Devices.PointOfService.UnifiedPosErrorSeverity severity,  global::Windows.Devices.PointOfService.UnifiedPosErrorReason reason,  uint extendedReason) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.UnifiedPosErrorData", "UnifiedPosErrorData.UnifiedPosErrorData(string message, UnifiedPosErrorSeverity severity, UnifiedPosErrorReason reason, uint extendedReason)");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.UnifiedPosErrorData.UnifiedPosErrorData(string, Windows.Devices.PointOfService.UnifiedPosErrorSeverity, Windows.Devices.PointOfService.UnifiedPosErrorReason, uint)
		// Forced skipping of method Windows.Devices.PointOfService.UnifiedPosErrorData.Message.get
		// Forced skipping of method Windows.Devices.PointOfService.UnifiedPosErrorData.Severity.get
		// Forced skipping of method Windows.Devices.PointOfService.UnifiedPosErrorData.Reason.get
		// Forced skipping of method Windows.Devices.PointOfService.UnifiedPosErrorData.ExtendedReason.get
	}
}
