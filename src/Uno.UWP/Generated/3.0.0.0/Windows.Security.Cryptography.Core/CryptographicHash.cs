#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CryptographicHash 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Append( global::Windows.Storage.Streams.IBuffer data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Core.CryptographicHash", "void CryptographicHash.Append(IBuffer data)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.Streams.IBuffer GetValueAndReset()
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicHash.GetValueAndReset() is not implemented in Uno.");
		}
		#endif
	}
}
