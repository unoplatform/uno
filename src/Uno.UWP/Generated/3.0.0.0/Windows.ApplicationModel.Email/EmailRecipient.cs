#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailRecipient 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailRecipient.Name is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipient", "string EmailRecipient.Name");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Address
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailRecipient.Address is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipient", "string EmailRecipient.Address");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public EmailRecipient( string address) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipient", "EmailRecipient.EmailRecipient(string address)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.EmailRecipient(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public EmailRecipient( string address,  string name) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipient", "EmailRecipient.EmailRecipient(string address, string name)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.EmailRecipient(string, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public EmailRecipient() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipient", "EmailRecipient.EmailRecipient()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.EmailRecipient()
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.Name.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.Name.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.Address.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipient.Address.set
	}
}
