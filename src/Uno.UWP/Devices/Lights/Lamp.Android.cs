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

		private readonly string? _defaultCameraId;
		private CameraManager? _cameraManager;
#pragma warning disable CS0618
		// using deprecated API for older Android versions
		private Android.Hardware.Camera? _camera;
#pragma warning restore CS0618
		private SurfaceTexture? _surfaceTexture;

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
				_brightness = value;
				UpdateLampState();
			}
		}

		private static Lamp? TryCreateInstance()
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
						if (Java.Lang.Boolean.True!.Equals(hasFlash))
						{
							return new Lamp(cameraManager, cameraId);
						}
					}
				}
			}
			else
			{
#pragma warning disable CS0618
#pragma warning disable CA1422 // Validate platform compatibility
				// using deprecated API for older Android versions
				var surfaceTexture = new SurfaceTexture(0);
				var camera = Android.Hardware.Camera.Open()!;
				camera.SetPreviewTexture(surfaceTexture);
				return new Lamp(camera, surfaceTexture);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618
			}
			return null;
		}

		private static bool SupportsFlashlight()
		{
			var packageManager = Application.Context.PackageManager!;
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
#if ANDROID33_0_OR_GREATER
				if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.Tiramisu)
				{
					_cameraManager!.SetTorchMode(_defaultCameraId!, isOn);
					if (!isOn)
					{
						return;
					}
					var characteristics = _cameraManager.GetCameraCharacteristics(_defaultCameraId!);
					const int minLevel = 1;
					var maxLevel = (Java.Lang.Integer)characteristics.Get(CameraCharacteristics.FlashInfoStrengthMaximumLevel)!;
					if (maxLevel is null)
					{
						// https://developer.android.com/reference/android/hardware/camera2/CameraCharacteristics#FLASH_INFO_STRENGTH_MAXIMUM_LEVEL
						// The value for this key will be null for devices with no flash unit.
						return;
					}

					// Android ranges from 1 (minLevel) to maxLevel
					// _brightness ranges from 0 to 1
					var nativeLevel = minLevel + _brightness * ((int)maxLevel - minLevel);
					_cameraManager.TurnOnTorchWithStrengthLevel(_defaultCameraId!, (int)Math.Round(nativeLevel));
				}
				else
#endif
				if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.M)
				{
					_cameraManager!.SetTorchMode(_defaultCameraId!, isOn);
				}
				else
				{
					// using deprecated API for older Android versions
#pragma warning disable CS0618 // Type or member is obsolete
					var param = _camera!.GetParameters()!;
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
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}


		public void Dispose()
		{
			lock (_lock)
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				_camera?.Release();
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
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
