#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Metadata
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MarshalingBehaviorAttribute : global::System.Attribute
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MarshalingBehaviorAttribute( global::Windows.Foundation.Metadata.MarshalingType behavior) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Metadata.MarshalingBehaviorAttribute", "MarshalingBehaviorAttribute.MarshalingBehaviorAttribute(MarshalingType behavior)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Metadata.MarshalingBehaviorAttribute.MarshalingBehaviorAttribute(Windows.Foundation.Metadata.MarshalingType)
	}
}
