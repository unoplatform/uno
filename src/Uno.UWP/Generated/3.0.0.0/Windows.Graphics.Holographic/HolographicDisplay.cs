#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicDisplay 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicAdapterId AdapterId
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicAdapterId HolographicDisplay.AdapterId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HolographicDisplay.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsOpaque
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicDisplay.IsOpaque is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsStereo
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicDisplay.IsStereo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Size MaxViewportSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size HolographicDisplay.MaxViewportSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialLocator SpatialLocator
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialLocator HolographicDisplay.SpatialLocator is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RefreshRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member double HolographicDisplay.RefreshRate is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.DisplayName.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.MaxViewportSize.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.IsStereo.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.IsOpaque.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.AdapterId.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.SpatialLocator.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicDisplay.RefreshRate.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicViewConfiguration TryGetViewConfiguration( global::Windows.Graphics.Holographic.HolographicViewConfigurationKind kind)
		{
			throw new global::System.NotImplementedException("The member HolographicViewConfiguration HolographicDisplay.TryGetViewConfiguration(HolographicViewConfigurationKind kind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Holographic.HolographicDisplay GetDefault()
		{
			throw new global::System.NotImplementedException("The member HolographicDisplay HolographicDisplay.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
