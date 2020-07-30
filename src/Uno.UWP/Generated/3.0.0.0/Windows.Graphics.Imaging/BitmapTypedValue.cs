#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BitmapTypedValue 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.PropertyType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member PropertyType BitmapTypedValue.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member object BitmapTypedValue.Value is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BitmapTypedValue( object value,  global::Windows.Foundation.PropertyType type) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Imaging.BitmapTypedValue", "BitmapTypedValue.BitmapTypedValue(object value, PropertyType type)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Imaging.BitmapTypedValue.BitmapTypedValue(object, Windows.Foundation.PropertyType)
		// Forced skipping of method Windows.Graphics.Imaging.BitmapTypedValue.Value.get
		// Forced skipping of method Windows.Graphics.Imaging.BitmapTypedValue.Type.get
	}
}
