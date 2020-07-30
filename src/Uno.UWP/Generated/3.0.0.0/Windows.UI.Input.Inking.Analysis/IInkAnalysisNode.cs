#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Analysis
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IInkAnalysisNode 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Rect BoundingRect
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode> Children
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint Id
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Input.Inking.Analysis.InkAnalysisNodeKind Kind
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode Parent
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Foundation.Point> RotatedBoundingRect
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.IInkAnalysisNode.Id.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.IInkAnalysisNode.Kind.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.IInkAnalysisNode.BoundingRect.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.IInkAnalysisNode.RotatedBoundingRect.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.IInkAnalysisNode.Children.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.IInkAnalysisNode.Parent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<uint> GetStrokeIds();
		#endif
	}
}
