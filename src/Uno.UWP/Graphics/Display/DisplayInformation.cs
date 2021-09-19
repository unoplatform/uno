#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private float _lastKnownDpi;
		private DisplayOrientations _lastKnownOrientation;

		private const float BaseDpi = 96.0f;

		private static readonly object _syncLock = new object();

		private static DisplayOrientations _autoRotationPreferences;

		private StartStopDelegateWrapper<TypedEventHandler<DisplayInformation, object>> _orientationChangedWrapper;
		private StartStopDelegateWrapper<TypedEventHandler<DisplayInformation, object>> _dpiChangedWrapper;

		private DisplayInformation()
		{
			Initialize();
		}

		public static DisplayOrientations AutoRotationPreferences
		{
			get => _autoRotationPreferences;
			set
			{
				_autoRotationPreferences = value;
				SetOrientationPartial(_autoRotationPreferences);
			}
		}		

		public bool StereoEnabled { get; private set; } = false;

		public static DisplayInformation GetForCurrentView() => InternalGetForCurrentView();

		static partial void SetOrientationPartial(DisplayOrientations orientations);

		partial void Initialize();

		partial void StartOrientationChanged();

		partial void StopOrientationChanged();

		partial void StartDpiChanged();

		partial void StopDpiChanged();

#pragma warning disable CS0067
		public event TypedEventHandler<DisplayInformation, object> OrientationChanged
		{
			add => _orientationChangedWrapper.AddHandler(value);
			remove => _orientationChangedWrapper.RemoveHandler(value);
		}

		public event TypedEventHandler<DisplayInformation, object> DpiChanged
		{
			add => _dpiChangedWrapper.AddHandler(value);
			remove => _dpiChangedWrapper.RemoveHandler(value);
		}
#pragma warning restore CS0067

		private void OnOrientationChanged() =>
			_orientationChangedWrapper.Event?.Invoke(this, null);

		private void OnDpiChanged() =>
			_dpiChangedWrapper.Event?.Invoke(this, null);

		private void OnDisplayMetricsChanged()
		{
			var newOrientation = CurrentOrientation;
			var newDpi = LogicalDpi;
			if (_lastKnownOrientation != newOrientation)
			{
				OnOrientationChanged();
				_lastKnownOrientation = newOrientation;
			}
			if (Math.Abs(_lastKnownDpi - newDpi) > 0.01)
			{
				OnDpiChanged();
				_lastKnownDpi = newDpi;
			}
		}
	}
}
