#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkStrokeBuilder 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public InkStrokeBuilder() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStrokeBuilder", "InkStrokeBuilder.InkStrokeBuilder()");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkStrokeBuilder.InkStrokeBuilder()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void BeginStroke( global::Windows.UI.Input.PointerPoint pointerPoint)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStrokeBuilder", "void InkStrokeBuilder.BeginStroke(PointerPoint pointerPoint)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.PointerPoint AppendToStroke( global::Windows.UI.Input.PointerPoint pointerPoint)
		{
			throw new global::System.NotImplementedException("The member PointerPoint InkStrokeBuilder.AppendToStroke(PointerPoint pointerPoint) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkStroke EndStroke( global::Windows.UI.Input.PointerPoint pointerPoint)
		{
			throw new global::System.NotImplementedException("The member InkStroke InkStrokeBuilder.EndStroke(PointerPoint pointerPoint) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkStroke CreateStroke( global::System.Collections.Generic.IEnumerable<global::Windows.Foundation.Point> points)
		{
			throw new global::System.NotImplementedException("The member InkStroke InkStrokeBuilder.CreateStroke(IEnumerable<Point> points) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDefaultDrawingAttributes( global::Windows.UI.Input.Inking.InkDrawingAttributes drawingAttributes)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStrokeBuilder", "void InkStrokeBuilder.SetDefaultDrawingAttributes(InkDrawingAttributes drawingAttributes)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkStroke CreateStrokeFromInkPoints( global::System.Collections.Generic.IEnumerable<global::Windows.UI.Input.Inking.InkPoint> inkPoints,  global::System.Numerics.Matrix3x2 transform)
		{
			throw new global::System.NotImplementedException("The member InkStroke InkStrokeBuilder.CreateStrokeFromInkPoints(IEnumerable<InkPoint> inkPoints, Matrix3x2 transform) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkStroke CreateStrokeFromInkPoints( global::System.Collections.Generic.IEnumerable<global::Windows.UI.Input.Inking.InkPoint> inkPoints,  global::System.Numerics.Matrix3x2 transform,  global::System.DateTimeOffset? strokeStartedTime,  global::System.TimeSpan? strokeDuration)
		{
			throw new global::System.NotImplementedException("The member InkStroke InkStrokeBuilder.CreateStrokeFromInkPoints(IEnumerable<InkPoint> inkPoints, Matrix3x2 transform, DateTimeOffset? strokeStartedTime, TimeSpan? strokeDuration) is not implemented in Uno.");
		}
		#endif
	}
}
