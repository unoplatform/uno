#nullable enable

using System;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Hardware;
using Android.Runtime;
using Android.Views;
using Uno.UI;
using Android.Provider;
using static Android.Provider.Settings;
using Android.Database;
using Android.OS;
using System.Threading.Tasks;
using System.Threading;

namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor
	{
		private SimpleOrientationEventListener? _orientationListener;
		private SettingsContentObserver? _contentObserver;
		private SensorManager? _sensorManager;
		private ISensorEventListener? _sensorEventListener;

		// Threshold, in meters per second squared, closely equivalent to an angle of 25 degrees which correspond to the value when Android detect new screen orientation 
		private const double _threshold = 4.55;
		private const Android.Hardware.SensorType _gravitySensorType = Android.Hardware.SensorType.Gravity;

		#region Static

		private static Orientation _defaultDeviceOrientation;

		private static partial SimpleOrientationSensor? TryCreateInstance()
		{
			if (Application.Context.GetSystemService(Context.SensorService) is not SensorManager)
			{
				return null;
			}

			return new SimpleOrientationSensor();
		}

		private static Orientation DefaultDeviceOrientation
		{
			get
			{
				if (_defaultDeviceOrientation == Orientation.Undefined)
				{
					var context = ContextHelper.Current;

					if (context != null)
					{
						var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

						if (context.Resources?.Configuration is { } config
							&& windowManager != null
							&& windowManager.DefaultDisplay is { } defaultDisplay)
						{
							var rotation = defaultDisplay.Rotation;

							_defaultDeviceOrientation =
								((rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180) && config.Orientation == Orientation.Landscape) ||
								((rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270) && config.Orientation == Orientation.Portrait)
									? Orientation.Landscape
									: Orientation.Portrait;
						}
					}
				}

				return _defaultDeviceOrientation;
			}
		}

		#endregion

		partial void Initialize()
		{
			var mainLooper = Looper.MainLooper;
			var context = ContextHelper.Current;

			// Thread pool is used to avoid startup
			// cost of threads creation.
			ThreadPool.QueueUserWorkItem(
				_ =>
				{
					if (Application.Context.GetSystemService(Context.SensorService) is SensorManager sensorManager)
					{
						_sensorManager = sensorManager;

						var gravitySensor = _sensorManager.GetDefaultSensor(_gravitySensorType);

						// Gravity sensor support seems to have been added in Nougat (Android 7), but not entirely functional.
						// Therefore, only use it in Oreo+ 
						var useGravitySensor = Build.VERSION.SdkInt >= BuildVersionCodes.O;

						// If the device has a gyroscope we will use the SensorType.Gravity, if not we will use single angle orientation calculations instead
						if (gravitySensor != null && useGravitySensor)
						{
							_sensorManager.RegisterListener(new OrientationListener(this), _sensorManager.GetDefaultSensor(_gravitySensorType), SensorDelay.Normal);
						}
						else
						{
							_orientationListener = new SimpleOrientationEventListener(orientation => OnOrientationChanged(orientation));
							_contentObserver = new SettingsContentObserver(new Handler(mainLooper!), () => OnIsAccelerometerRotationEnabledChanged(IsAccelerometerRotationEnabled));

							if (Settings.System.GetUriFor(Settings.System.AccelerometerRotation) is { } accelerometerRotationUri)
							{
								context.ContentResolver!.RegisterContentObserver(accelerometerRotationUri, true, _contentObserver);
								if (_orientationListener.CanDetectOrientation() && IsAccelerometerRotationEnabled)
								{
									_orientationListener.Enable();
								}
							}
						}
					}
				}, null);
		}

		partial void StartListeningOrientationChanged()
		{
			var mainLooper = Looper.MainLooper;
			var context = ContextHelper.Current;

			// Thread pool is used to avoid startup
			// cost of threads creation.
			ThreadPool.QueueUserWorkItem(
				_ =>
				{
					if (Application.Context.GetSystemService(Context.SensorService) is SensorManager sensorManager)
					{
						_sensorManager = sensorManager;

						var gravitySensor = _sensorManager.GetDefaultSensor(_gravitySensorType);

						// Gravity sensor support seems to have been added in Nougat (Android 7), but not entirely functional.
						// Therefore, only use it in Oreo+ 
						var useGravitySensor = Build.VERSION.SdkInt >= BuildVersionCodes.O;

						// If the device has a gyroscope we will use the SensorType.Gravity, if not we will use single angle orientation calculations instead
						if (gravitySensor != null && useGravitySensor)
						{
							_sensorEventListener = new OrientationListener(this);
							_sensorManager.RegisterListener(_sensorEventListener, _sensorManager.GetDefaultSensor(_gravitySensorType), SensorDelay.Normal);
						}
						else
						{
							_orientationListener = new SimpleOrientationEventListener(orientation => OnOrientationChanged(orientation));
							_contentObserver = new SettingsContentObserver(new Handler(mainLooper!), () => OnIsAccelerometerRotationEnabledChanged(IsAccelerometerRotationEnabled));

							if (Settings.System.GetUriFor(Settings.System.AccelerometerRotation) is { } accelerometerRotationUri)
							{
								context.ContentResolver!.RegisterContentObserver(accelerometerRotationUri, true, _contentObserver);
								if (_orientationListener.CanDetectOrientation() && IsAccelerometerRotationEnabled)
								{
									_orientationListener.Enable();
								}
							}
						}
					}
				}, null);
		}

		partial void StopListeningOrientationChanged()
		{
			var context = ContextHelper.Current;

			if (Application.Context.GetSystemService(Context.SensorService) is SensorManager sensorManager)
			{
				_sensorManager = sensorManager;
				var gravitySensor = _sensorManager.GetDefaultSensor(_gravitySensorType);

				// Gravity sensor support seems to have been added in Nougat (Android 7), but not entirely functional.
				// Therefore, only use it in Oreo+ 
				var useGravitySensor = Build.VERSION.SdkInt >= BuildVersionCodes.O;

				// If the device has a gyroscope we will use the SensorType.Gravity, if not we will use single angle orientation calculations instead
				if (gravitySensor != null && useGravitySensor)
				{
					_sensorManager.UnregisterListener(_sensorEventListener, _sensorManager.GetDefaultSensor(_gravitySensorType));
				}
				else if (Settings.System.GetUriFor(Settings.System.AccelerometerRotation) is { } accelerometerRotationUri &&
					_orientationListener is { } orientationListener
					&& _contentObserver is { } contentObserver)
				{
					context.ContentResolver!.RegisterContentObserver(accelerometerRotationUri, true, contentObserver);
					if (orientationListener.CanDetectOrientation() && IsAccelerometerRotationEnabled)
					{
						_orientationListener.Disable();
					}
				}
			}
		}

		#region OrientationSensorType Methods and Classes

		private void OnOrientationChanged(int angle)
		{
			var simpleOrientation = ToSimpleOrientationRelativeToPortrait(angle, _currentOrientation);
			SetCurrentOrientation(simpleOrientation);
		}

		private static SimpleOrientation ToSimpleOrientationRelativeToPortrait(int orientation, SimpleOrientation previousOrientation)
		{
			// https://developer.android.com/reference/android/view/OrientationEventListener.html
			// orientation parameter is in degrees, ranging from 0 to 359.
			// orientation is:
			// - 0 degrees when the device is oriented in its natural position
			// - 90 degrees when its left side is at the top
			// - 180 degrees when it is upside down
			// - 270 degrees when its right side is to the top
			// - ORIENTATION_UNKNOWN when the device is close to flat and the orientation cannot be determined.

			if (orientation == OrientationEventListener.OrientationUnknown)
			{
				// device is close to flat then we push a face-up by default.
				return SimpleOrientation.Faceup;
			}

			if (DefaultDeviceOrientation == Orientation.Landscape)
			{
				// we offset the rotation by 270 degrees because
				// we want an orientation relative to Portrait
				orientation = (orientation + 270) % 360;
			}

			// Ensures orientation only changes when within close range to new orientation.
			// Empirical testing on an Android 6.0 device indicates that orientation changes 
			// when within about 22.5° (90° / 4) of a new orientation (0°, 90°, 180°, 270°).
			var threshold = 22.5;

			if (Math.Abs(orientation - 0) < threshold || Math.Abs(orientation - 360) < threshold)
			{
				// natural position
				return SimpleOrientation.NotRotated;
			}
			else if (Math.Abs(orientation - 90) < threshold)
			{
				// left side is at the top
				return SimpleOrientation.Rotated270DegreesCounterclockwise;
			}
			else if (Math.Abs(orientation - 180) < threshold)
			{
				// upside down
				return SimpleOrientation.Rotated180DegreesCounterclockwise;
			}
			else if (Math.Abs(orientation - 270) < threshold)
			{
				// right side is to the top
				return SimpleOrientation.Rotated90DegreesCounterclockwise;
			}
			else
			{
				return previousOrientation;
			}
		}

		private void OnIsAccelerometerRotationEnabledChanged(bool isAccelerometerRotationEnabled)
		{
			if (_orientationListener != null)
			{
				if (isAccelerometerRotationEnabled)
				{
					_orientationListener.Enable();
				}
				else
				{
					_orientationListener.Disable();
					SetCurrentOrientation(SimpleOrientation.NotRotated);
				}
			}
		}

		private static bool IsAccelerometerRotationEnabled
		{
			get
			{
				try
				{
					return Settings.System.GetInt(ContextHelper.Current.ContentResolver, Settings.System.AccelerometerRotation, 0) == 1;
				}
				catch (SettingNotFoundException)
				{
					return true; // If it can't be disabled in the settings, we assume it's enabled.
				}
			}
		}

		private class SettingsContentObserver : ContentObserver
		{
			Action _onChanged;

			public SettingsContentObserver(Handler handler, Action onChange) : base(handler)
			{
				_onChanged = onChange;
			}

			public override bool DeliverSelfNotifications() => true;

			public override void OnChange(bool selfChange)
			{
				base.OnChange(selfChange);
				_onChanged();
			}
		}

		private class SimpleOrientationEventListener : OrientationEventListener
		{
			private Action<int> _orientationChanged;

			public SimpleOrientationEventListener(Action<int> orientationChanged) : base(ContextHelper.Current, SensorDelay.Normal)
			{
				_orientationChanged = orientationChanged;
			}

			public override void OnOrientationChanged(int orientation) => _orientationChanged(orientation);
		}

		private sealed class OrientationListener : Java.Lang.Object, ISensorEventListener
		{
			private readonly SimpleOrientationSensor _simpleOrientationSensor;

			public OrientationListener(SimpleOrientationSensor simpleOrientationSensor)
			{
				_simpleOrientationSensor = simpleOrientationSensor;
			}

			public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}

			public void OnSensorChanged(SensorEvent? e)
			{
				if (e?.Sensor?.Type != _gravitySensorType || e?.Values == null)
				{
					return;
				}

				// All units are negatives compared to iOS : https://developer.android.com/reference/android/hardware/SensorEvent#values
				var gravityX = -(double)e.Values[0];
				var gravityY = -(double)e.Values[1];
				var gravityZ = -(double)e.Values[2];

				var simpleOrientation = ToSimpleOrientation(gravityX, gravityY, gravityZ, _threshold, _simpleOrientationSensor._currentOrientation);
				_simpleOrientationSensor.SetCurrentOrientation(simpleOrientation);
			}
		}

		#endregion
	}
}
