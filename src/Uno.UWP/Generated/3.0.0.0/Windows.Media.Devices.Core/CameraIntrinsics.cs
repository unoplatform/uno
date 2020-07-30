#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CameraIntrinsics 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 FocalLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CameraIntrinsics.FocalLength is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ImageHeight
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CameraIntrinsics.ImageHeight is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ImageWidth
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CameraIntrinsics.ImageWidth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 PrincipalPoint
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CameraIntrinsics.PrincipalPoint is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3 RadialDistortion
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector3 CameraIntrinsics.RadialDistortion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 TangentialDistortion
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CameraIntrinsics.TangentialDistortion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Matrix4x4 UndistortedProjectionTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member Matrix4x4 CameraIntrinsics.UndistortedProjectionTransform is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CameraIntrinsics( global::System.Numerics.Vector2 focalLength,  global::System.Numerics.Vector2 principalPoint,  global::System.Numerics.Vector3 radialDistortion,  global::System.Numerics.Vector2 tangentialDistortion,  uint imageWidth,  uint imageHeight) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.CameraIntrinsics", "CameraIntrinsics.CameraIntrinsics(Vector2 focalLength, Vector2 principalPoint, Vector3 radialDistortion, Vector2 tangentialDistortion, uint imageWidth, uint imageHeight)");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.CameraIntrinsics(System.Numerics.Vector2, System.Numerics.Vector2, System.Numerics.Vector3, System.Numerics.Vector2, uint, uint)
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.FocalLength.get
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.PrincipalPoint.get
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.RadialDistortion.get
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.TangentialDistortion.get
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.ImageWidth.get
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.ImageHeight.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point ProjectOntoFrame( global::System.Numerics.Vector3 coordinate)
		{
			throw new global::System.NotImplementedException("The member Point CameraIntrinsics.ProjectOntoFrame(Vector3 coordinate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2 UnprojectAtUnitDepth( global::Windows.Foundation.Point pixelCoordinate)
		{
			throw new global::System.NotImplementedException("The member Vector2 CameraIntrinsics.UnprojectAtUnitDepth(Point pixelCoordinate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ProjectManyOntoFrame( global::System.Numerics.Vector3[] coordinates,  global::Windows.Foundation.Point[] results)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.CameraIntrinsics", "void CameraIntrinsics.ProjectManyOntoFrame(Vector3[] coordinates, Point[] results)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UnprojectPixelsAtUnitDepth( global::Windows.Foundation.Point[] pixelCoordinates,  global::System.Numerics.Vector2[] results)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.CameraIntrinsics", "void CameraIntrinsics.UnprojectPixelsAtUnitDepth(Point[] pixelCoordinates, Vector2[] results)");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.Core.CameraIntrinsics.UndistortedProjectionTransform.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point DistortPoint( global::Windows.Foundation.Point input)
		{
			throw new global::System.NotImplementedException("The member Point CameraIntrinsics.DistortPoint(Point input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DistortPoints( global::Windows.Foundation.Point[] inputs,  global::Windows.Foundation.Point[] results)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.CameraIntrinsics", "void CameraIntrinsics.DistortPoints(Point[] inputs, Point[] results)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point UndistortPoint( global::Windows.Foundation.Point input)
		{
			throw new global::System.NotImplementedException("The member Point CameraIntrinsics.UndistortPoint(Point input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UndistortPoints( global::Windows.Foundation.Point[] inputs,  global::Windows.Foundation.Point[] results)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.CameraIntrinsics", "void CameraIntrinsics.UndistortPoints(Point[] inputs, Point[] results)");
		}
		#endif
	}
}
