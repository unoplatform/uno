#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionLineGeometry : global::Windows.UI.Composition.CompositionGeometry
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 Start
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CompositionLineGeometry.Start is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionLineGeometry", "Vector2 CompositionLineGeometry.Start");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 End
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CompositionLineGeometry.End is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionLineGeometry", "Vector2 CompositionLineGeometry.End");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionLineGeometry.Start.get
		// Forced skipping of method Windows.UI.Composition.CompositionLineGeometry.Start.set
		// Forced skipping of method Windows.UI.Composition.CompositionLineGeometry.End.get
		// Forced skipping of method Windows.UI.Composition.CompositionLineGeometry.End.set
	}
}
