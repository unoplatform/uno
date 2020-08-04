#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Casting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CastingDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string FriendlyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CastingDevice.FriendlyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStreamWithContentType Icon
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStreamWithContentType CastingDevice.Icon is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CastingDevice.Id is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Casting.CastingDevice.Id.get
		// Forced skipping of method Windows.Media.Casting.CastingDevice.FriendlyName.get
		// Forced skipping of method Windows.Media.Casting.CastingDevice.Icon.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Casting.CastingPlaybackTypes> GetSupportedCastingPlaybackTypesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CastingPlaybackTypes> CastingDevice.GetSupportedCastingPlaybackTypesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Casting.CastingConnection CreateCastingConnection()
		{
			throw new global::System.NotImplementedException("The member CastingConnection CastingDevice.CreateCastingConnection() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( global::Windows.Media.Casting.CastingPlaybackTypes type)
		{
			throw new global::System.NotImplementedException("The member string CastingDevice.GetDeviceSelector(CastingPlaybackTypes type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetDeviceSelectorFromCastingSourceAsync( global::Windows.Media.Casting.CastingSource castingSource)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CastingDevice.GetDeviceSelectorFromCastingSourceAsync(CastingSource castingSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Casting.CastingDevice> FromIdAsync( string value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CastingDevice> CastingDevice.FromIdAsync(string value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> DeviceInfoSupportsCastingAsync( global::Windows.Devices.Enumeration.DeviceInformation device)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CastingDevice.DeviceInfoSupportsCastingAsync(DeviceInformation device) is not implemented in Uno.");
		}
		#endif
	}
}
