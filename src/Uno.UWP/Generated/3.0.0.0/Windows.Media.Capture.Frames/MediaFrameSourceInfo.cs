#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaFrameSourceInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Perception.Spatial.SpatialCoordinateSystem CoordinateSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialCoordinateSystem MediaFrameSourceInfo.CoordinateSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Enumeration.DeviceInformation DeviceInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation MediaFrameSourceInfo.DeviceInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaFrameSourceInfo.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Capture.MediaStreamType MediaStreamType
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaStreamType MediaFrameSourceInfo.MediaStreamType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> MediaFrameSourceInfo.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Capture.Frames.MediaFrameSourceGroup SourceGroup
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameSourceGroup MediaFrameSourceInfo.SourceGroup is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Capture.Frames.MediaFrameSourceKind SourceKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameSourceKind MediaFrameSourceInfo.SourceKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string ProfileId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaFrameSourceInfo.ProfileId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Capture.MediaCaptureVideoProfileMediaDescription> VideoProfileMediaDescription
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MediaCaptureVideoProfileMediaDescription> MediaFrameSourceInfo.VideoProfileMediaDescription is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.Id.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.MediaStreamType.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.SourceKind.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.SourceGroup.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.DeviceInformation.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.Properties.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.CoordinateSystem.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.ProfileId.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceInfo.VideoProfileMediaDescription.get
	}
}
