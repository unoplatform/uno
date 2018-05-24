#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Content.Res;
using Android.Hardware;
using Android.Runtime;
using Android.Views;
using Uno.UI;
using Windows.Graphics.Display;
using Android.Provider;
using static Android.Provider.Settings;
using Android.Database;
using Android.OS;

namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor
	{
		#region Static

		private static Orientation _defaultDeviceOrientation;

		private static Orientation DefaultDeviceOrientation
		{
			get
			{
				if (_defaultDeviceOrientation == Orientation.Undefined)
				{
					var context = ContextHelper.Current;
					var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
					var config = context.Resources.Configuration;
					var rotation = windowManager.DefaultDisplay.Rotation;

					_defaultDeviceOrientation =
						((rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180) && config.Orientation == Orientation.Landscape) ||
						((rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270) && config.Orientation == Orientation.Portrait)
							? Orientation.Landscape
							: Orientation.Portrait;
				}

				return _defaultDeviceOrientation;
			}
		}

		#endregion

		private SimpleOrientationEventListener _orientationListener;
		private SettingsContentObserver _contentObserver;

		partial void Initialize()
		{
			_orientationListener = new SimpleOrientationEventListener(orientation => OnOrientationChanged(orientation));
			_contentObserver = new SettingsContentObserver(new Handler(Looper.MainLooper), () => OnIsAccelerometerRotationEnabledChanged(IsAccelerometerRotationEnabled));

			ContextHelper.Current.ContentResolver.RegisterContentObserver(Settings.System.GetUriFor(Settings.System.AccelerometerRotation), true, _contentObserver);
			if (_orientationListener.CanDetectOrientation() && IsAccelerometerRotationEnabled)
			{
				_orientationListener.Enable();
			}
		}

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
	}
}
#endif