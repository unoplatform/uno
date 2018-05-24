#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Analysis
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkAnalysisDrawingKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Drawing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Circle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ellipse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Triangle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoscelesTriangle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EquilateralTriangle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightTriangle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Quadrilateral,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rectangle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Diamond,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Trapezoid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Parallelogram,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pentagon,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hexagon,
		#endif
	}
	#endif
}
