#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkInputConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPrimaryBarrelButtonInputEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InkInputConfiguration.IsPrimaryBarrelButtonInputEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkInputConfiguration", "bool InkInputConfiguration.IsPrimaryBarrelButtonInputEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEraserInputEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InkInputConfiguration.IsEraserInputEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkInputConfiguration", "bool InkInputConfiguration.IsEraserInputEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkInputConfiguration.IsPrimaryBarrelButtonInputEnabled.get
		// Forced skipping of method Windows.UI.Input.Inking.InkInputConfiguration.IsPrimaryBarrelButtonInputEnabled.set
		// Forced skipping of method Windows.UI.Input.Inking.InkInputConfiguration.IsEraserInputEnabled.get
		// Forced skipping of method Windows.UI.Input.Inking.InkInputConfiguration.IsEraserInputEnabled.set
	}
}
