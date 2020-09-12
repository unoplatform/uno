#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NDCustomData : global::Windows.Media.Protection.PlayReady.INDCustomData
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte[] CustomData
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte[] NDCustomData.CustomData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte[] CustomDataTypeID
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte[] NDCustomData.CustomDataTypeID is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NDCustomData( byte[] customDataTypeIDBytes,  byte[] customDataBytes) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDCustomData", "NDCustomData.NDCustomData(byte[] customDataTypeIDBytes, byte[] customDataBytes)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDCustomData.NDCustomData(byte[], byte[])
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDCustomData.CustomDataTypeID.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDCustomData.CustomData.get
		// Processing: Windows.Media.Protection.PlayReady.INDCustomData
	}
}
