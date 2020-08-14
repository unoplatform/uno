#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Metadata
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StaticAttribute : global::System.Attribute
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StaticAttribute( global::System.Type type,  uint version) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.StaticAttribute", "StaticAttribute.StaticAttribute(Type type, uint version)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.StaticAttribute.StaticAttribute(System.Type, uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StaticAttribute( global::System.Type type,  uint version,  global::Windows.Foundation.Metadata.Platform platform) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.StaticAttribute", "StaticAttribute.StaticAttribute(Type type, uint version, Platform platform)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.StaticAttribute.StaticAttribute(System.Type, uint, Windows.Foundation.Metadata.Platform)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StaticAttribute( global::System.Type type,  uint version,  string contractName) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.StaticAttribute", "StaticAttribute.StaticAttribute(Type type, uint version, string contractName)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.StaticAttribute.StaticAttribute(System.Type, uint, string)
	}
}
