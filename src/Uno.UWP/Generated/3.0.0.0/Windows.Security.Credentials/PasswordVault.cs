#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PasswordVault 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public PasswordVault() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordVault", "PasswordVault.PasswordVault()");
		}
		#endif
		// Forced skipping of method Windows.Security.Credentials.PasswordVault.PasswordVault()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Add( global::Windows.Security.Credentials.PasswordCredential credential)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordVault", "void PasswordVault.Add(PasswordCredential credential)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Remove( global::Windows.Security.Credentials.PasswordCredential credential)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordVault", "void PasswordVault.Remove(PasswordCredential credential)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Security.Credentials.PasswordCredential Retrieve( string resource,  string userName)
		{
			throw new global::System.NotImplementedException("The member PasswordCredential PasswordVault.Retrieve(string resource, string userName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Credentials.PasswordCredential> FindAllByResource( string resource)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<PasswordCredential> PasswordVault.FindAllByResource(string resource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Credentials.PasswordCredential> FindAllByUserName( string userName)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<PasswordCredential> PasswordVault.FindAllByUserName(string userName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Credentials.PasswordCredential> RetrieveAll()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<PasswordCredential> PasswordVault.RetrieveAll() is not implemented in Uno.");
		}
		#endif
	}
}
