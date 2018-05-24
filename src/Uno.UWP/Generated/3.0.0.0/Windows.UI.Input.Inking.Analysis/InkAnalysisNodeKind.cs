#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Analysis
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkAnalysisNodeKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnclassifiedInk,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Root,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WritingRegion,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paragraph,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Line,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InkWord,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InkBullet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InkDrawing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ListItem,
		#endif
	}
	#endif
}
