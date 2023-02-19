#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactBatch 
	{
		// Skipping already declared property Contacts
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactBatchStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactBatchStatus ContactBatch.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ContactBatchStatus%20ContactBatch.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactBatch.Contacts.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactBatch.Status.get
	}
}
