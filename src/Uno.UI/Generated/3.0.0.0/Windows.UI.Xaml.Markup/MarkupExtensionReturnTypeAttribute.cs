#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Markup
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MarkupExtensionReturnTypeAttribute : global::System.Attribute
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MarkupExtensionReturnTypeAttribute() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.MarkupExtensionReturnTypeAttribute", "MarkupExtensionReturnTypeAttribute.MarkupExtensionReturnTypeAttribute()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Markup.MarkupExtensionReturnTypeAttribute.MarkupExtensionReturnTypeAttribute()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		public  global::System.Type ReturnType;
		#endif
	}
}
