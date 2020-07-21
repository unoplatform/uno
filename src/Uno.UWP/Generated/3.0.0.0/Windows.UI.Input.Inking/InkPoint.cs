#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkPoint 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point InkPoint.Position is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Pressure
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InkPoint.Pressure is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float TiltX
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InkPoint.TiltX is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float TiltY
		{
			get
			{
				throw new global::System.NotImplementedException("The member float InkPoint.TiltY is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong InkPoint.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public InkPoint( global::Windows.Foundation.Point position,  float pressure,  float tiltX,  float tiltY,  ulong timestamp) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkPoint", "InkPoint.InkPoint(Point position, float pressure, float tiltX, float tiltY, ulong timestamp)");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.InkPoint(Windows.Foundation.Point, float, float, float, ulong)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public InkPoint( global::Windows.Foundation.Point position,  float pressure) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkPoint", "InkPoint.InkPoint(Point position, float pressure)");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.InkPoint(Windows.Foundation.Point, float)
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.Position.get
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.Pressure.get
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.TiltX.get
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.TiltY.get
		// Forced skipping of method Windows.UI.Input.Inking.InkPoint.Timestamp.get
	}
}
