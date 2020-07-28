#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkDrawingAttributesPencilProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Opacity
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InkDrawingAttributesPencilProperties.Opacity is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkDrawingAttributesPencilProperties", "double InkDrawingAttributesPencilProperties.Opacity");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkDrawingAttributesPencilProperties.Opacity.get
		// Forced skipping of method Windows.UI.Input.Inking.InkDrawingAttributesPencilProperties.Opacity.set
	}
}
