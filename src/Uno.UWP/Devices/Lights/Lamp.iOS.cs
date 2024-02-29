using System;
using AVFoundation;

namespace Windows.Devices.Lights
{
	public partial class Lamp
	{
		private AVCaptureDevice _captureDevice;
		private float _brightness;
		private bool _isEnabled;

		private Lamp(AVCaptureDevice captureDevice)
		{
			_captureDevice = captureDevice;
			_brightness = GetCurrentBrightness();
			_isEnabled = _brightness > 0;
		}

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

		private float GetCurrentBrightness()
		{
			if (_captureDevice.HasTorch)
			{
				return _captureDevice.TorchLevel;
			}

			return _captureDevice.FlashMode == AVCaptureFlashMode.On ? 1 : 0;
		}

		private void UpdateLampState()
		{
			_captureDevice.LockForConfiguration(out var error);

			if (error == null)
			{
				if (_isEnabled && _brightness > 0)
				{
					if (_captureDevice.HasTorch)
					{
						_captureDevice.SetTorchModeLevel(
							_brightness,
							out var torchErr);
					}
					else
					{
						_captureDevice.FlashMode = AVCaptureFlashMode.On;
					}
				}
				else
				{
					if (_captureDevice.HasTorch)
					{
						_captureDevice.TorchMode = AVCaptureTorchMode.Off;
					}
					else
					{
						_captureDevice.FlashMode = AVCaptureFlashMode.Off;
					}
				}
			}
			else
			{
				throw new InvalidOperationException("Could not change lamp state. Error: " + error);
			}

			_captureDevice.UnlockForConfiguration();
		}

		private static Lamp? TryCreateInstance()
		{
			var captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video.GetConstant()!);

			var lampAvailable =
				captureDevice != null &&
				(captureDevice.HasFlash || captureDevice.HasTorch);

			if (lampAvailable)
			{
				return new Lamp(captureDevice!);
			}

			return null;
		}

		public void Dispose()
		{
			_captureDevice?.Dispose();
			_captureDevice = null!;
		}
	}
}
