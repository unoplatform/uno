#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicFrameScanoutReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MissedScanoutCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HolographicFrameScanoutReport.MissedScanoutCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFrameRenderingReport RenderingReport
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicFrameRenderingReport HolographicFrameScanoutReport.RenderingReport is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan SystemRelativeLatchTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFrameScanoutReport.SystemRelativeLatchTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan SystemRelativePhotonTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFrameScanoutReport.SystemRelativePhotonTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan SystemRelativeScanoutStartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFrameScanoutReport.SystemRelativeScanoutStartTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrameScanoutReport.RenderingReport.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrameScanoutReport.MissedScanoutCount.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrameScanoutReport.SystemRelativeLatchTime.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrameScanoutReport.SystemRelativeScanoutStartTime.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrameScanoutReport.SystemRelativePhotonTime.get
	}
}
