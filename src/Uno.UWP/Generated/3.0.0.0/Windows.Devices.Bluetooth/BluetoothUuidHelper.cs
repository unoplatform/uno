#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class BluetoothUuidHelper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Guid FromShortId( uint shortId)
		{
			throw new global::System.NotImplementedException("The member Guid BluetoothUuidHelper.FromShortId(uint shortId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20BluetoothUuidHelper.FromShortId%28uint%20shortId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static uint? TryGetShortId( global::System.Guid uuid)
		{
			throw new global::System.NotImplementedException("The member uint? BluetoothUuidHelper.TryGetShortId(Guid uuid) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%3F%20BluetoothUuidHelper.TryGetShortId%28Guid%20uuid%29");
		}
		#endif
	}
}
