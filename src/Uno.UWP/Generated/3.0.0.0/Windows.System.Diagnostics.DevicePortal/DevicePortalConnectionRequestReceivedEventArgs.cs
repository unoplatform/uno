#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.DevicePortal
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DevicePortalConnectionRequestReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpRequestMessage RequestMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpRequestMessage DevicePortalConnectionRequestReceivedEventArgs.RequestMessage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpResponseMessage ResponseMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpResponseMessage DevicePortalConnectionRequestReceivedEventArgs.ResponseMessage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsWebSocketUpgradeRequest
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DevicePortalConnectionRequestReceivedEventArgs.IsWebSocketUpgradeRequest is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> WebSocketProtocolsRequested
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> DevicePortalConnectionRequestReceivedEventArgs.WebSocketProtocolsRequested is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnectionRequestReceivedEventArgs.RequestMessage.get
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnectionRequestReceivedEventArgs.ResponseMessage.get
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnectionRequestReceivedEventArgs.IsWebSocketUpgradeRequest.get
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnectionRequestReceivedEventArgs.WebSocketProtocolsRequested.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral DevicePortalConnectionRequestReceivedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
