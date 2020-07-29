#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IContactInformation2 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset DisplayPictureDate
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation2.DisplayPictureDate.get
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation2.DisplayPictureDate.set
	}
}
