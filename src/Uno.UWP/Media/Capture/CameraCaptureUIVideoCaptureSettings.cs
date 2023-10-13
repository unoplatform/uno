#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	public partial class CameraCaptureUIVideoCaptureSettings
	{
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__
		[global::Uno.NotImplemented]
		public global::Windows.Media.Capture.CameraCaptureUIMaxVideoResolution MaxResolution
		{
			get
			{
				throw new global::System.NotImplementedException("The member CameraCaptureUIMaxVideoResolution CameraCaptureUIVideoCaptureSettings.MaxResolution is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings", "CameraCaptureUIMaxVideoResolution CameraCaptureUIVideoCaptureSettings.MaxResolution");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__
		[global::Uno.NotImplemented]
		public float MaxDurationInSeconds
		{
			get
			{
				throw new global::System.NotImplementedException("The member float CameraCaptureUIVideoCaptureSettings.MaxDurationInSeconds is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings", "float CameraCaptureUIVideoCaptureSettings.MaxDurationInSeconds");
			}
		}
#endif

		public CameraCaptureUIVideoFormat Format
		{
			get; set;
		}

#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__
		[global::Uno.NotImplemented]
		public bool AllowTrimming
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CameraCaptureUIVideoCaptureSettings.AllowTrimming is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings", "bool CameraCaptureUIVideoCaptureSettings.AllowTrimming");
			}
		}
#endif
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.Format.get
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.Format.set
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.MaxResolution.get
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.MaxResolution.set
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.MaxDurationInSeconds.get
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.MaxDurationInSeconds.set
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.AllowTrimming.get
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings.AllowTrimming.set
	}
}
