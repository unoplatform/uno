#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Metadata
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContractVersionAttribute : global::System.Attribute
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContractVersionAttribute( uint version) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.ContractVersionAttribute", "ContractVersionAttribute.ContractVersionAttribute(uint version)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.ContractVersionAttribute.ContractVersionAttribute(uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContractVersionAttribute( global::System.Type contract,  uint version) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.ContractVersionAttribute", "ContractVersionAttribute.ContractVersionAttribute(Type contract, uint version)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.ContractVersionAttribute.ContractVersionAttribute(System.Type, uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContractVersionAttribute( string contract,  uint version) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.ContractVersionAttribute", "ContractVersionAttribute.ContractVersionAttribute(string contract, uint version)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.ContractVersionAttribute.ContractVersionAttribute(string, uint)
	}
}
