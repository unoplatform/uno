#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PasswordCredential 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string UserName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PasswordCredential.UserName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordCredential", "string PasswordCredential.UserName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Resource
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PasswordCredential.Resource is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordCredential", "string PasswordCredential.Resource");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Password
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PasswordCredential.Password is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordCredential", "string PasswordCredential.Password");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet PasswordCredential.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public PasswordCredential( string resource,  string userName,  string password) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordCredential", "PasswordCredential.PasswordCredential(string resource, string userName, string password)");
		}
		#endif
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.PasswordCredential(string, string, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public PasswordCredential() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordCredential", "PasswordCredential.PasswordCredential()");
		}
		#endif
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.PasswordCredential()
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.Resource.get
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.Resource.set
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.UserName.get
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.UserName.set
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.Password.get
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.Password.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void RetrievePassword()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Credentials.PasswordCredential", "void PasswordCredential.RetrievePassword()");
		}
		#endif
		// Forced skipping of method Windows.Security.Credentials.PasswordCredential.Properties.get
	}
}
