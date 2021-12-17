#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CubicBezierEasingFunction : global::Windows.UI.Composition.CompositionEasingFunction
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 ControlPoint1
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CubicBezierEasingFunction.ControlPoint1 is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 ControlPoint2
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CubicBezierEasingFunction.ControlPoint2 is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CubicBezierEasingFunction.ControlPoint1.get
		// Forced skipping of method Windows.UI.Composition.CubicBezierEasingFunction.ControlPoint2.get
	}
}
