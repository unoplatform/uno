#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IContactInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DisplayName
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IRandomAccessStreamReference DisplayPicture
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string FamilyName
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string GivenName
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string HonorificPrefix
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string HonorificSuffix
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.DisplayName.get
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.DisplayName.set
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.FamilyName.get
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.FamilyName.set
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.GivenName.get
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.GivenName.set
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.HonorificPrefix.get
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.HonorificPrefix.set
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.HonorificSuffix.get
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.HonorificSuffix.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> GetDisplayPictureAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncAction SetDisplayPictureAsync( global::Windows.Storage.Streams.IInputStream stream);
		#endif
		// Forced skipping of method Windows.Phone.PersonalInformation.IContactInformation.DisplayPicture.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IDictionary<string, object>> GetPropertiesAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> ToVcardAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> ToVcardAsync( global::Windows.Phone.PersonalInformation.VCardFormat format);
		#endif
	}
}
