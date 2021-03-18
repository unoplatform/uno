#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointerPoint 
	{
		// Skipping already declared property FrameId
		// Skipping already declared property IsInContact
		// Skipping already declared property PointerDevice
		// Skipping already declared property PointerId
		// Skipping already declared property Position
		// Skipping already declared property Properties
		// Skipping already declared property RawPosition
		// Skipping already declared property Timestamp
		// Forced skipping of method Windows.UI.Input.PointerPoint.PointerDevice.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.Position.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.RawPosition.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.PointerId.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.FrameId.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.Timestamp.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.IsInContact.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.PointerPoint GetCurrentPoint( uint pointerId)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerPoint.GetCurrentPoint(uint pointerId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints( uint pointerId)
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerPoint.GetIntermediatePoints(uint pointerId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.PointerPoint GetCurrentPoint( uint pointerId,  global::Windows.UI.Input.IPointerPointTransform transform)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerPoint.GetCurrentPoint(uint pointerId, IPointerPointTransform transform) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints( uint pointerId,  global::Windows.UI.Input.IPointerPointTransform transform)
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerPoint.GetIntermediatePoints(uint pointerId, IPointerPointTransform transform) is not implemented in Uno.");
		}
		#endif
	}
}
