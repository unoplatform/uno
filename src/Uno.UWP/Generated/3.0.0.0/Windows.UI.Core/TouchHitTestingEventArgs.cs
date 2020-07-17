#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TouchHitTestingEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TouchHitTestingEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.TouchHitTestingEventArgs", "bool TouchHitTestingEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreProximityEvaluation ProximityEvaluation
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreProximityEvaluation TouchHitTestingEventArgs.ProximityEvaluation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.TouchHitTestingEventArgs", "CoreProximityEvaluation TouchHitTestingEventArgs.ProximityEvaluation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect BoundingBox
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect TouchHitTestingEventArgs.BoundingBox is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point Point
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point TouchHitTestingEventArgs.Point is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.TouchHitTestingEventArgs.ProximityEvaluation.get
		// Forced skipping of method Windows.UI.Core.TouchHitTestingEventArgs.ProximityEvaluation.set
		// Forced skipping of method Windows.UI.Core.TouchHitTestingEventArgs.Point.get
		// Forced skipping of method Windows.UI.Core.TouchHitTestingEventArgs.BoundingBox.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreProximityEvaluation EvaluateProximity( global::Windows.Foundation.Rect controlBoundingBox)
		{
			throw new global::System.NotImplementedException("The member CoreProximityEvaluation TouchHitTestingEventArgs.EvaluateProximity(Rect controlBoundingBox) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreProximityEvaluation EvaluateProximity( global::Windows.Foundation.Point[] controlVertices)
		{
			throw new global::System.NotImplementedException("The member CoreProximityEvaluation TouchHitTestingEventArgs.EvaluateProximity(Point[] controlVertices) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.TouchHitTestingEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.TouchHitTestingEventArgs.Handled.set
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
