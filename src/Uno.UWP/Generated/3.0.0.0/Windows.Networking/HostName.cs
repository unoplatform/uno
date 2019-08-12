#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HostName : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string CanonicalName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HostName.CanonicalName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HostName.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Networking.Connectivity.IPInformation IPInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPInformation HostName.IPInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string RawName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HostName.RawName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Networking.HostNameType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostNameType HostName.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsEqual( global::Windows.Networking.HostName hostName)
		{
			throw new global::System.NotImplementedException("The member bool HostName.IsEqual(HostName hostName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HostName.ToString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static int Compare( string value1,  string value2)
		{
			throw new global::System.NotImplementedException("The member int HostName.Compare(string value1, string value2) is not implemented in Uno.");
		}
		#endif
	}
}
