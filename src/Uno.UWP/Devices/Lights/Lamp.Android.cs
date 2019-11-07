#if __ANDROID__
using System;
using System.Linq;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Windows.Devices.Lights
{
	public partial class Lamp
	{
		private readonly object _lock = new object();

		private readonly string _defaultCameraId;
		private CameraManager _cameraManager;
#pragma warning disable CS0618
		// using deprecated API for older Android versions
		private Android.Hardware.Camera _camera = null;
#pragma warning restore CS0618
		private SurfaceTexture _surfaceTexture = null;

		private float _brightness;
		private bool _isEnabled;

		private Lamp(CameraManager cameraManager, string defaultCameraId)
		{
			_cameraManager = cameraManager;
			_defaultCameraId = defaultCameraId;
		}

#pragma warning disable CS0618
		private Lamp(Android.Hardware.Camera camera, SurfaceTexture surfaceTexture)
		{
			_camera = camera;
			_surfaceTexture = surfaceTexture;
		}
#pragma warning restore CS0618

		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				_isEnabled = value;
				UpdateLampState();
			}
		}

		public float BrightnessLevel
		{
			get => _brightness;
			set
			{
				_brightness = value > 0 ? 1 : 0;
				UpdateLampState();
			}
		}

		private static Lamp TryCreateInstance()
		{
			if (!SupportsFlashlight())
			{
				return null;
			}

			if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.M)
			{
				var cameraManager = Application.Context.GetSystemService(Context.CameraService) as CameraManager;
				if (cameraManager != null)
				{
					foreach (var cameraId in cameraManager.GetCameraIdList())
					{
						var hasFlash = cameraManager.GetCameraCharacteristics(cameraId)
							.Get(CameraCharacteristics.FlashInfoAvailable);
						if (Java.Lang.Boolean.True.Equals(hasFlash))
						{
							return new Lamp(cameraManager, cameraId);
						}
					}
				}
			}
			else
			{
#pragma warning disable CS0618
				// using deprecated API for older Android versions				
				var surfaceTexture = new SurfaceTexture(0);
				var camera = Android.Hardware.Camera.Open();
				camera.SetPreviewTexture(surfaceTexture);
				return new Lamp(camera, surfaceTexture);
#pragma warning restore CS0618
			}
			return null;
		}

		private static bool SupportsFlashlight()
		{
			var packageManager = Application.Context.PackageManager;
			return packageManager
				.GetSystemAvailableFeatures()
				.Any(feature =>
					feature.Name?.Equals(
						PackageManager.FeatureCameraFlash,
						StringComparison.OrdinalIgnoreCase) == true);
		}

		private void UpdateLampState()
		{
			var isOn = _isEnabled && _brightness > 0;
			lock (_lock)
			{
				if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.M)
				{
					_cameraManager.SetTorchMode(_defaultCameraId, isOn);
				}
				else
				{
#pragma warning disable CS0618
					// using deprecated API for older Android versions
					var param = _camera.GetParameters();
					param.FlashMode = _isEnabled ?
						Android.Hardware.Camera.Parameters.FlashModeTorch :
						Android.Hardware.Camera.Parameters.FlashModeOff;
					_camera.SetParameters(param);

					if (isOn)
					{
						_camera.StartPreview();
					}
					else
					{
						_camera.StopPreview();
					}
#pragma warning restore CS0618
				}
			}
		}


		public void Dispose()
		{
			lock (_lock)
			{
				_camera?.Release();
				_camera?.Dispose();
				_camera = null;
				_surfaceTexture?.Dispose();
				_surfaceTexture = null;
				_cameraManager?.Dispose();
				_cameraManager = null;
			}
		}
	}
}
#endif
