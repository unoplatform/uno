#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CredentialPicker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.UI.CredentialPickerResults> PickAsync( global::Windows.Security.Credentials.UI.CredentialPickerOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CredentialPickerResults> CredentialPicker.PickAsync(CredentialPickerOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.UI.CredentialPickerResults> PickAsync( string targetName,  string message)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CredentialPickerResults> CredentialPicker.PickAsync(string targetName, string message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.UI.CredentialPickerResults> PickAsync( string targetName,  string message,  string caption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CredentialPickerResults> CredentialPicker.PickAsync(string targetName, string message, string caption) is not implemented in Uno.");
		}
		#endif
	}
}
