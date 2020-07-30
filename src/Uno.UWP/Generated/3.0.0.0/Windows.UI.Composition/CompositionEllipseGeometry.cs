#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionEllipseGeometry : global::Windows.UI.Composition.CompositionGeometry
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 Radius
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CompositionEllipseGeometry.Radius is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionEllipseGeometry", "Vector2 CompositionEllipseGeometry.Radius");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 Center
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CompositionEllipseGeometry.Center is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionEllipseGeometry", "Vector2 CompositionEllipseGeometry.Center");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionEllipseGeometry.Center.get
		// Forced skipping of method Windows.UI.Composition.CompositionEllipseGeometry.Center.set
		// Forced skipping of method Windows.UI.Composition.CompositionEllipseGeometry.Radius.get
		// Forced skipping of method Windows.UI.Composition.CompositionEllipseGeometry.Radius.set
	}
}
