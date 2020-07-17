#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMediaEncodingProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.MediaProperties.MediaPropertySet Properties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Subtype
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Type
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.IMediaEncodingProperties.Properties.get
		// Forced skipping of method Windows.Media.MediaProperties.IMediaEncodingProperties.Type.get
		// Forced skipping of method Windows.Media.MediaProperties.IMediaEncodingProperties.Subtype.set
		// Forced skipping of method Windows.Media.MediaProperties.IMediaEncodingProperties.Subtype.get
	}
}
