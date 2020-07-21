#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Metadata
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ComposableAttribute : global::System.Attribute
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ComposableAttribute( global::System.Type type,  global::Windows.Foundation.Metadata.CompositionType compositionType,  uint version) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.ComposableAttribute", "ComposableAttribute.ComposableAttribute(Type type, CompositionType compositionType, uint version)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.ComposableAttribute.ComposableAttribute(System.Type, Windows.Foundation.Metadata.CompositionType, uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ComposableAttribute( global::System.Type type,  global::Windows.Foundation.Metadata.CompositionType compositionType,  uint version,  global::Windows.Foundation.Metadata.Platform platform) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.ComposableAttribute", "ComposableAttribute.ComposableAttribute(Type type, CompositionType compositionType, uint version, Platform platform)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.ComposableAttribute.ComposableAttribute(System.Type, Windows.Foundation.Metadata.CompositionType, uint, Windows.Foundation.Metadata.Platform)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ComposableAttribute( global::System.Type type,  global::Windows.Foundation.Metadata.CompositionType compositionType,  uint version,  string contract) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.ComposableAttribute", "ComposableAttribute.ComposableAttribute(Type type, CompositionType compositionType, uint version, string contract)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.ComposableAttribute.ComposableAttribute(System.Type, Windows.Foundation.Metadata.CompositionType, uint, string)
	}
}
