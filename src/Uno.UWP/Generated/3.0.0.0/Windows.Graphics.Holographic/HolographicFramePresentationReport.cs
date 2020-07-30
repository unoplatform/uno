#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicFramePresentationReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan AppGpuDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFramePresentationReport.AppGpuDuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan AppGpuOverrun
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFramePresentationReport.AppGpuOverrun is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan CompositorGpuDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFramePresentationReport.CompositorGpuDuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MissedPresentationOpportunityCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HolographicFramePresentationReport.MissedPresentationOpportunityCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint PresentationCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HolographicFramePresentationReport.PresentationCount is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFramePresentationReport.CompositorGpuDuration.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFramePresentationReport.AppGpuDuration.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFramePresentationReport.AppGpuOverrun.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFramePresentationReport.MissedPresentationOpportunityCount.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFramePresentationReport.PresentationCount.get
	}
}
