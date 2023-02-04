#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IppSetAttributesResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Devices.Printers.IppAttributeError> AttributeErrors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, IppAttributeError> IppSetAttributesResult.AttributeErrors is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20IppAttributeError%3E%20IppSetAttributesResult.AttributeErrors");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Succeeded
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool IppSetAttributesResult.Succeeded is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20IppSetAttributesResult.Succeeded");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.IppSetAttributesResult.Succeeded.get
		// Forced skipping of method Windows.Devices.Printers.IppSetAttributesResult.AttributeErrors.get
	}
}
