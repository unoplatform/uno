#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEAppearance 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort Category
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEAppearance.Category is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort RawValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEAppearance.RawValue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort SubCategory
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEAppearance.SubCategory is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEAppearance.RawValue.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEAppearance.Category.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEAppearance.SubCategory.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Bluetooth.BluetoothLEAppearance FromRawValue( ushort rawValue)
		{
			throw new global::System.NotImplementedException("The member BluetoothLEAppearance BluetoothLEAppearance.FromRawValue(ushort rawValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Bluetooth.BluetoothLEAppearance FromParts( ushort appearanceCategory,  ushort appearanceSubCategory)
		{
			throw new global::System.NotImplementedException("The member BluetoothLEAppearance BluetoothLEAppearance.FromParts(ushort appearanceCategory, ushort appearanceSubCategory) is not implemented in Uno.");
		}
		#endif
	}
}
