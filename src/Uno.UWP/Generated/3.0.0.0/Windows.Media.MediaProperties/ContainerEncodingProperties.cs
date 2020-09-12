#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContainerEncodingProperties : global::Windows.Media.MediaProperties.IMediaEncodingProperties
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Subtype
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContainerEncodingProperties.Subtype is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.ContainerEncodingProperties", "string ContainerEncodingProperties.Subtype");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPropertySet ContainerEncodingProperties.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContainerEncodingProperties.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContainerEncodingProperties() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.ContainerEncodingProperties", "ContainerEncodingProperties.ContainerEncodingProperties()");
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.ContainerEncodingProperties.ContainerEncodingProperties()
		// Forced skipping of method Windows.Media.MediaProperties.ContainerEncodingProperties.Properties.get
		// Forced skipping of method Windows.Media.MediaProperties.ContainerEncodingProperties.Type.get
		// Forced skipping of method Windows.Media.MediaProperties.ContainerEncodingProperties.Subtype.set
		// Forced skipping of method Windows.Media.MediaProperties.ContainerEncodingProperties.Subtype.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.ContainerEncodingProperties Copy()
		{
			throw new global::System.NotImplementedException("The member ContainerEncodingProperties ContainerEncodingProperties.Copy() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.MediaProperties.IMediaEncodingProperties
	}
}
