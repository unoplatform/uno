#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkStroke 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Selected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InkStroke.Selected is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStroke", "bool InkStroke.Selected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkDrawingAttributes DrawingAttributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkDrawingAttributes InkStroke.DrawingAttributes is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStroke", "InkDrawingAttributes InkStroke.DrawingAttributes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect BoundingRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect InkStroke.BoundingRect is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Recognized
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InkStroke.Recognized is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Matrix3x2 PointTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member Matrix3x2 InkStroke.PointTransform is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStroke", "Matrix3x2 InkStroke.PointTransform");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset? StrokeStartedTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? InkStroke.StrokeStartedTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStroke", "DateTimeOffset? InkStroke.StrokeStartedTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? StrokeDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? InkStroke.StrokeDuration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkStroke", "TimeSpan? InkStroke.StrokeDuration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint InkStroke.Id is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.DrawingAttributes.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.DrawingAttributes.set
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.BoundingRect.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.Selected.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.Selected.set
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.Recognized.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Input.Inking.InkStrokeRenderingSegment> GetRenderingSegments()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<InkStrokeRenderingSegment> InkStroke.GetRenderingSegments() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkStroke Clone()
		{
			throw new global::System.NotImplementedException("The member InkStroke InkStroke.Clone() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.PointTransform.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.PointTransform.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Input.Inking.InkPoint> GetInkPoints()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<InkPoint> InkStroke.GetInkPoints() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.Id.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.StrokeStartedTime.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.StrokeStartedTime.set
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.StrokeDuration.get
		// Forced skipping of method Windows.UI.Input.Inking.InkStroke.StrokeDuration.set
	}
}
