#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimedMetadataEncodingProperties : global::Windows.Media.MediaProperties.IMediaEncodingProperties
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Subtype
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataEncodingProperties.Subtype is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.TimedMetadataEncodingProperties", "string TimedMetadataEncodingProperties.Subtype");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPropertySet TimedMetadataEncodingProperties.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataEncodingProperties.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public TimedMetadataEncodingProperties() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.TimedMetadataEncodingProperties", "TimedMetadataEncodingProperties.TimedMetadataEncodingProperties()");
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.TimedMetadataEncodingProperties()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetFormatUserData( byte[] value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.TimedMetadataEncodingProperties", "void TimedMetadataEncodingProperties.SetFormatUserData(byte[] value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GetFormatUserData(out byte[] value)
		{
			throw new global::System.NotImplementedException("The member void TimedMetadataEncodingProperties.GetFormatUserData(out byte[] value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.TimedMetadataEncodingProperties Copy()
		{
			throw new global::System.NotImplementedException("The member TimedMetadataEncodingProperties TimedMetadataEncodingProperties.Copy() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Properties.get
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Type.get
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Subtype.set
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Subtype.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.MediaProperties.TimedMetadataEncodingProperties CreatePgs()
		{
			throw new global::System.NotImplementedException("The member TimedMetadataEncodingProperties TimedMetadataEncodingProperties.CreatePgs() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.MediaProperties.TimedMetadataEncodingProperties CreateSrt()
		{
			throw new global::System.NotImplementedException("The member TimedMetadataEncodingProperties TimedMetadataEncodingProperties.CreateSrt() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.MediaProperties.TimedMetadataEncodingProperties CreateSsa( byte[] formatUserData)
		{
			throw new global::System.NotImplementedException("The member TimedMetadataEncodingProperties TimedMetadataEncodingProperties.CreateSsa(byte[] formatUserData) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.MediaProperties.TimedMetadataEncodingProperties CreateVobSub( byte[] formatUserData)
		{
			throw new global::System.NotImplementedException("The member TimedMetadataEncodingProperties TimedMetadataEncodingProperties.CreateVobSub(byte[] formatUserData) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.MediaProperties.IMediaEncodingProperties
	}
}
