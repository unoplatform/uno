#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicFrame 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Holographic.HolographicCamera> AddedCameras
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<HolographicCamera> HolographicFrame.AddedCameras is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFramePrediction CurrentPrediction
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicFramePrediction HolographicFrame.CurrentPrediction is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan HolographicFrame.Duration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Holographic.HolographicCamera> RemovedCameras
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<HolographicCamera> HolographicFrame.RemovedCameras is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFrameId Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicFrameId HolographicFrame.Id is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrame.AddedCameras.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrame.RemovedCameras.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicCameraRenderingParameters GetRenderingParameters( global::Windows.Graphics.Holographic.HolographicCameraPose cameraPose)
		{
			throw new global::System.NotImplementedException("The member HolographicCameraRenderingParameters HolographicFrame.GetRenderingParameters(HolographicCameraPose cameraPose) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrame.Duration.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrame.CurrentPrediction.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateCurrentPrediction()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicFrame", "void HolographicFrame.UpdateCurrentPrediction()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFramePresentResult PresentUsingCurrentPrediction()
		{
			throw new global::System.NotImplementedException("The member HolographicFramePresentResult HolographicFrame.PresentUsingCurrentPrediction() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFramePresentResult PresentUsingCurrentPrediction( global::Windows.Graphics.Holographic.HolographicFramePresentWaitBehavior waitBehavior)
		{
			throw new global::System.NotImplementedException("The member HolographicFramePresentResult HolographicFrame.PresentUsingCurrentPrediction(HolographicFramePresentWaitBehavior waitBehavior) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void WaitForFrameToFinish()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicFrame", "void HolographicFrame.WaitForFrameToFinish()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters GetQuadLayerUpdateParameters( global::Windows.Graphics.Holographic.HolographicQuadLayer layer)
		{
			throw new global::System.NotImplementedException("The member HolographicQuadLayerUpdateParameters HolographicFrame.GetQuadLayerUpdateParameters(HolographicQuadLayer layer) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicFrame.Id.get
	}
}
