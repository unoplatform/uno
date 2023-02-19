#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.DialProtocol
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DialDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DialDevice.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20DialDevice.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string FriendlyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DialDevice.FriendlyName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20DialDevice.FriendlyName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStreamReference Thumbnail
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStreamReference DialDevice.Thumbnail is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IRandomAccessStreamReference%20DialDevice.Thumbnail");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.DialProtocol.DialDevice.Id.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.DialProtocol.DialApp GetDialApp( string appName)
		{
			throw new global::System.NotImplementedException("The member DialApp DialDevice.GetDialApp(string appName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DialApp%20DialDevice.GetDialApp%28string%20appName%29");
		}
		#endif
		// Forced skipping of method Windows.Media.DialProtocol.DialDevice.FriendlyName.get
		// Forced skipping of method Windows.Media.DialProtocol.DialDevice.Thumbnail.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( string appName)
		{
			throw new global::System.NotImplementedException("The member string DialDevice.GetDeviceSelector(string appName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20DialDevice.GetDeviceSelector%28string%20appName%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Media.DialProtocol.DialDevice> FromIdAsync( string value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DialDevice> DialDevice.FromIdAsync(string value) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDialDevice%3E%20DialDevice.FromIdAsync%28string%20value%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> DeviceInfoSupportsDialAsync( global::Windows.Devices.Enumeration.DeviceInformation device)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> DialDevice.DeviceInfoSupportsDialAsync(DeviceInformation device) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20DialDevice.DeviceInfoSupportsDialAsync%28DeviceInformation%20device%29");
		}
		#endif
	}
}
