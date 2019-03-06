#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaCaptureDeviceExclusiveControlStatusChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaCaptureDeviceExclusiveControlStatusChangedEventArgs.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Capture.MediaCaptureDeviceExclusiveControlStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaCaptureDeviceExclusiveControlStatus MediaCaptureDeviceExclusiveControlStatusChangedEventArgs.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCaptureDeviceExclusiveControlStatusChangedEventArgs.DeviceId.get
		// Forced skipping of method Windows.Media.Capture.MediaCaptureDeviceExclusiveControlStatusChangedEventArgs.Status.get
	}
}
