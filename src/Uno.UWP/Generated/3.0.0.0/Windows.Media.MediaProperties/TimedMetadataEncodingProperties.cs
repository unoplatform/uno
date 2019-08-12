#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimedMetadataEncodingProperties : global::Windows.Media.MediaProperties.IMediaEncodingProperties
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.MediaProperties.MediaPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPropertySet TimedMetadataEncodingProperties.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataEncodingProperties.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public TimedMetadataEncodingProperties() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.TimedMetadataEncodingProperties", "TimedMetadataEncodingProperties.TimedMetadataEncodingProperties()");
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.TimedMetadataEncodingProperties()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SetFormatUserData( byte[] value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.TimedMetadataEncodingProperties", "void TimedMetadataEncodingProperties.SetFormatUserData(byte[] value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void GetFormatUserData(out byte[] value)
		{
			throw new global::System.NotImplementedException("The member void TimedMetadataEncodingProperties.GetFormatUserData(out byte[] value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.MediaProperties.TimedMetadataEncodingProperties Copy()
		{
			throw new global::System.NotImplementedException("The member TimedMetadataEncodingProperties TimedMetadataEncodingProperties.Copy() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Properties.get
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Type.get
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Subtype.set
		// Forced skipping of method Windows.Media.MediaProperties.TimedMetadataEncodingProperties.Subtype.get
		// Processing: Windows.Media.MediaProperties.IMediaEncodingProperties
	}
}
