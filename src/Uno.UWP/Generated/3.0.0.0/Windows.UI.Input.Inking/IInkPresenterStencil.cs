#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IInkPresenterStencil 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Color BackgroundColor
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Color ForegroundColor
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsVisible
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Input.Inking.InkPresenterStencilKind Kind
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Numerics.Matrix3x2 Transform
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.Kind.get
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.IsVisible.get
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.IsVisible.set
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.BackgroundColor.get
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.BackgroundColor.set
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.ForegroundColor.get
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.ForegroundColor.set
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.Transform.get
		// Forced skipping of method Windows.UI.Input.Inking.IInkPresenterStencil.Transform.set
	}
}
