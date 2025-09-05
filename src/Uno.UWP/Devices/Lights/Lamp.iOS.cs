using System;
using Uno.Foundation.Logging;
using AVFoundation;

namespace Windows.Devices.Lights
{
    public partial class Lamp
    {
        private AVCaptureDevice _captureDevice;
        private AVCaptureSession _captureSession;
        private AVCaptureDeviceInput _deviceInput;
        private AVCaptureOutput _dummyOutput;
        private float _brightness;
        private bool _isEnabled;

        private Lamp(AVCaptureDevice captureDevice)
        {
            _captureDevice = captureDevice;
            _brightness = GetCurrentBrightness();
            _isEnabled = _brightness > 0;

            // Create capture session for torch control
            _captureSession = new AVCaptureSession();
            _deviceInput = new AVCaptureDeviceInput(captureDevice, out var inputError);
            if (inputError != null)
            {
                if (this.Log().IsEnabled(LogLevel.Error))
                {
                    this.Log().LogError($"Failed to create AVCaptureDeviceInput: {inputError}");
                }
            }
            if (inputError != null)
            {
                throw new InvalidOperationException("Could not create device input. Error: " + inputError);
            }

            if (_captureSession.CanAddInput(_deviceInput))
            {
                _captureSession.AddInput(_deviceInput);
            }
            else
            {
                throw new InvalidOperationException("Could not add device input to session");
            }

            // Add a dummy output to satisfy session requirements
            _dummyOutput = new AVCaptureVideoDataOutput();
            if (_captureSession.CanAddOutput(_dummyOutput))
            {
                _captureSession.AddOutput(_dummyOutput);
            }
            else
            {
                throw new InvalidOperationException("Could not add dummy output to session");
            }

            _captureSession.StartRunning();
            if (!_captureSession.Running)
            {
                throw new InvalidOperationException("Could not start capture session");
            }
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
                        var success = _captureDevice.SetTorchModeLevel(_brightness, out var torchError);
                        if (!success || torchError != null)
                        {
                            if (this.Log().IsEnabled(LogLevel.Warning))
                            {
                                this.Log().LogWarning($"Failed to set torch level to {_brightness}: {torchError?.Description ?? "Unknown error"}. Falling back to full brightness.");
                            }
                            // If setting level fails, try to turn torch on at full brightness
                            _captureDevice.TorchMode = AVCaptureTorchMode.On;
                        }
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

        private static Lamp TryCreateInstance()
        {
            var captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video.GetConstant());

            var lampAvailable =
                captureDevice != null &&
                (captureDevice.HasFlash || captureDevice.HasTorch);

            if (lampAvailable)
            {
                return new Lamp(captureDevice);
            }

            return null;
        }

        public void Dispose()
        {
            if (_captureSession?.Running == true)
            {
                _captureSession.StopRunning();
            }
            _captureSession?.Dispose();
            _captureSession = null;
            _deviceInput?.Dispose();
            _deviceInput = null;
            _dummyOutput?.Dispose();
            _dummyOutput = null;
            _captureDevice?.Dispose();
            _captureDevice = null;
        }
    }
}
