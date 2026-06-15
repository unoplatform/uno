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

		private static object _windowIdMapLock = new object();
		private static readonly Dictionary<WindowId, DisplayInformation> _windowIdMap = new();

#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__
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
		private DisplayInformation(WindowId windowId, bool skipExtensionInitialization)
		{
			WindowId = windowId;
			if (!skipExtensionInitialization)
			{
				Initialize();
			}
		}
#endif

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

#pragma warning disable RS0030 // Do not use banned APIs
		internal static DisplayInformation GetForCurrentViewSafe() => GetForCurrentView();
#pragma warning restore RS0030 // Do not use banned APIs

		internal static DisplayInformation GetOrCreateForWindowId(WindowId windowId)
		{
			lock (_windowIdMapLock)
			{
				if (!_windowIdMap.TryGetValue(windowId, out var displayInformation))
				{
					displayInformation = new(
#if !ANDROID
						windowId
#endif
					);
					_windowIdMap[windowId] = displayInformation;
				}

				return displayInformation;
			}
		}

#if !ANDROID
		/// <summary>
		/// Test-only seam that registers a <see cref="DisplayInformation"/> entry for an arbitrary
		/// <see cref="WindowId"/> without resolving the platform windowing extension. On Skia the
		/// regular <see cref="GetOrCreateForWindowId"/> path runs <see cref="Initialize"/>, which
		/// resolves an <c>IDisplayInformationExtension</c> for a real <c>AppWindow</c> and throws for
		/// an id with no live window. This lets the registry lifetime invariant (entry added, then
		/// removed by <see cref="DestroyForWindowId"/> and collectible) be verified without a window.
		/// </summary>
		internal static DisplayInformation CreateForWindowIdForTests(WindowId windowId)
		{
			lock (_windowIdMapLock)
			{
				if (!_windowIdMap.TryGetValue(windowId, out var displayInformation))
				{
					displayInformation = new(windowId, skipExtensionInitialization: true);
					_windowIdMap[windowId] = displayInformation;
				}

				return displayInformation;
			}
		}
#endif

		/// <summary>
		/// Removes the <see cref="DisplayInformation"/> registered for a window once that window is
		/// closed. Without this, the static map retains the closed window's native wrapper graph
		/// (and every event subscriber reachable from it) for the process lifetime — for a
		/// secondary-app window in a collectible AssemblyLoadContext, that pins the entire ALC.
		/// </summary>
		internal static void DestroyForWindowId(WindowId windowId)
		{
			lock (_windowIdMapLock)
			{
				_windowIdMap.Remove(windowId);
			}
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

#if __ANDROID__ || __IOS__ || __WASM__ || __SKIA__
		private void OnDpiChanged() => _dpiChanged?.Invoke(this, null);
#endif

#if __ANDROID__ || __IOS__ || __WASM__
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
