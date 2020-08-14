#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Analysis
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkAnalysisRoot : global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect BoundingRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect InkAnalysisRoot.BoundingRect is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode> Children
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<IInkAnalysisNode> InkAnalysisRoot.Children is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint InkAnalysisRoot.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.Analysis.InkAnalysisNodeKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkAnalysisNodeKind InkAnalysisRoot.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode Parent
		{
			get
			{
				throw new global::System.NotImplementedException("The member IInkAnalysisNode InkAnalysisRoot.Parent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Foundation.Point> RotatedBoundingRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Point> InkAnalysisRoot.RotatedBoundingRect is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RecognizedText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string InkAnalysisRoot.RecognizedText is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.RecognizedText.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode> FindNodes( global::Windows.UI.Input.Inking.Analysis.InkAnalysisNodeKind nodeKind)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<IInkAnalysisNode> InkAnalysisRoot.FindNodes(InkAnalysisNodeKind nodeKind) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.Id.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.Kind.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.BoundingRect.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.RotatedBoundingRect.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.Children.get
		// Forced skipping of method Windows.UI.Input.Inking.Analysis.InkAnalysisRoot.Parent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<uint> GetStrokeIds()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<uint> InkAnalysisRoot.GetStrokeIds() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.UI.Input.Inking.Analysis.IInkAnalysisNode
	}
}
