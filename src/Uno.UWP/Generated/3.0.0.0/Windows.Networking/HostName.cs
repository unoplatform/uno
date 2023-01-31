#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class HostName : global::Windows.Foundation.IStringable
	{
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CanonicalName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HostName.CanonicalName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20HostName.CanonicalName");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HostName.DisplayName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20HostName.DisplayName");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.IPInformation IPInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPInformation HostName.IPInformation is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IPInformation%20HostName.IPInformation");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RawName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HostName.RawName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20HostName.RawName");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostNameType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostNameType HostName.Type is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HostNameType%20HostName.Type");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HostName( string hostName) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.HostName", "HostName.HostName(string hostName)");
		}
		#endif
		// Forced skipping of method Windows.Networking.HostName.HostName(string)
		// Forced skipping of method Windows.Networking.HostName.IPInformation.get
		// Forced skipping of method Windows.Networking.HostName.RawName.get
		// Forced skipping of method Windows.Networking.HostName.DisplayName.get
		// Forced skipping of method Windows.Networking.HostName.CanonicalName.get
		// Forced skipping of method Windows.Networking.HostName.Type.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEqual( global::Windows.Networking.HostName hostName)
		{
			throw new global::System.NotImplementedException("The member bool HostName.IsEqual(HostName hostName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20HostName.IsEqual%28HostName%20hostName%29");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HostName.ToString() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20HostName.ToString%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static int Compare( string value1,  string value2)
		{
			throw new global::System.NotImplementedException("The member int HostName.Compare(string value1, string value2) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20HostName.Compare%28string%20value1%2C%20string%20value2%29");
		}
		#endif
	}
}
