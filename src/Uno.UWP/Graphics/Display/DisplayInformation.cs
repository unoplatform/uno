#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		internal const float BaseDpi = 96.0f;

		private static readonly Dictionary<WindowId, DisplayInformation> _windowIdMap = new();

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
		private float _lastKnownDpi;
		private DisplayOrientations _lastKnownOrientation;
#endif

		private static readonly object _syncLock = new object();

		private static DisplayOrientations _autoRotationPreferences;

		private TypedEventHandler<DisplayInformation, object> _orientationChanged;
		private TypedEventHandler<DisplayInformation, object> _dpiChanged;

		private DisplayInformation(
#if !ANDROID
			WindowId windowId
#endif
			)
		{
#if !ANDROID
			WindowId = windowId;
#endif
			Initialize();
		}

#if !ANDROID
		internal WindowId WindowId { get; }
#endif

		public static DisplayInformation GetForCurrentView()
		{
#if ANDROID
			return GetForCurrentViewAndroid();
#else
			// This is needed to ensure for "current view" there is always a corresponding DisplayView instance.
			// This means that Uno Islands and WinUI apps can keep using this API for now until we make the breaking change
			// on Uno.WinUI codebase.
			return GetOrCreateForWindowId(AppWindow.MainWindowId);
#endif
		}

		internal static DisplayInformation GetForCurrentViewSafe() => GetForCurrentView();

		internal static DisplayInformation GetForWindowId(WindowId windowId)
		{
			if (!_windowIdMap.TryGetValue(windowId, out var appView))
			{
				throw new InvalidOperationException(
					$"ApplicationView corresponding with this window does not exist yet, which usually means " +
					$"the API was called too early in the windowing lifecycle. Try to use ApplicationView later.");
			}

			return appView;
		}

		internal static DisplayInformation GetOrCreateForWindowId(WindowId windowId)
		{
			if (!_windowIdMap.TryGetValue(windowId, out var appView))
			{
				appView = new(windowId);
				_windowIdMap[windowId] = appView;
			}

			return appView;
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

		public bool StereoEnabled { get; private set; }

		static partial void SetOrientationPartial(DisplayOrientations orientations);

		partial void Initialize();

		partial void StartOrientationChanged();

		partial void StopOrientationChanged();

		partial void StartDpiChanged();

		partial void StopDpiChanged();

#pragma warning disable CS0067
		public event TypedEventHandler<DisplayInformation, object> OrientationChanged
		{
			add
			{
				lock (_syncLock)
				{
					bool isFirstSubscriber = _orientationChanged == null;
					_orientationChanged += value;
					if (isFirstSubscriber)
					{
						StartOrientationChanged();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_orientationChanged -= value;
					if (_orientationChanged == null)
					{
						StopOrientationChanged();
					}
				}
			}
		}

		public event TypedEventHandler<DisplayInformation, object> DpiChanged
		{
			add
			{
				lock (_syncLock)
				{
					bool isFirstSubscriber = _dpiChanged == null;
					_dpiChanged += value;
					if (isFirstSubscriber)
					{
						StartDpiChanged();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_dpiChanged -= value;
					if (_dpiChanged == null)
					{
						StopDpiChanged();
					}
				}
			}
		}
#pragma warning restore CS0067

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || __SKIA__
		private void OnDpiChanged() => _dpiChanged?.Invoke(this, null);
#endif

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
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

		private void OnOrientationChanged() => _orientationChanged?.Invoke(this, null);
#endif
	}
}
