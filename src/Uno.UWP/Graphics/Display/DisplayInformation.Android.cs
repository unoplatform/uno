#nullable enable
using System;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;
using Java.Interop;
using Uno.UI;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private DisplayMetricsCache _cachedDisplayMetrics;
		private SurfaceOrientation _cachedRotation;

		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			var currentActivity = ContextHelper.Current as Activity;
			if (currentActivity != null)
			{
				currentActivity.RequestedOrientation = orientations.ToScreenOrientation();
			}
		}

		partial void Initialize()
		{
			RefreshDisplayMetricsCache();
		}

		public DisplayOrientations CurrentOrientation => GetCurrentOrientation();


		/// <summary>
		/// Gets the native orientation of the display monitor,
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation => GetNativeOrientation();


		public uint ScreenHeightInRawPixels
			=> (uint)_cachedDisplayMetrics.HeightPixels;

		public uint ScreenWidthInRawPixels
			=> (uint)_cachedDisplayMetrics.WidthPixels;

		public double RawPixelsPerViewPixel => _cachedDisplayMetrics.Density;

		public float LogicalDpi
			// DisplayMetrics of 1.0 matches 100%, or UWP's default 96.0 DPI.
			// https://stuff.mit.edu/afs/sipb/project/android/docs/reference/android/util/DisplayMetrics.html#density
			=> _cachedDisplayMetrics.Density * BaseDpi;

		public ResolutionScale ResolutionScale
			=> (ResolutionScale)(int)(_cachedDisplayMetrics.Density * 100);

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the x axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpix#remarks">Docs</see>
		/// defaults to 0 if not set
		/// </remarks>
		public float RawDpiX
			=> _cachedDisplayMetrics.Xdpi;

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the y axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpiy#remarks">Docs</see>
		/// defaults to 0 if not set
		/// </remarks>
		public float RawDpiY
			=> _cachedDisplayMetrics.Ydpi;

		/// <summary>
		/// Diagonal size of the display in inches.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.diagonalsizeininches#property-value">Docs</see>
		/// defaults to null if not set
		/// </remarks>
		public double? DiagonalSizeInInches
		{
			get
			{
				var x = Math.Pow((uint)_cachedDisplayMetrics.WidthPixels / _cachedDisplayMetrics.Xdpi, 2);
				var y = Math.Pow((uint)_cachedDisplayMetrics.HeightPixels / _cachedDisplayMetrics.Ydpi, 2);
				var screenInches = Math.Sqrt(x + y);
				return screenInches;
			}
		}

		/// <summary>
		/// Sets the NativeOrientation property
		/// </summary>
		/// <remarks>
		/// Based on responses in
		/// https://stackoverflow.com/questions/4553650/how-to-check-device-natural-default-orientation-on-android-i-e-get-landscape this SO question
		/// </remarks>
		private DisplayOrientations GetNativeOrientation()
		{
			using (var windowManager = CreateWindowManager())
			{
				var orientation = ContextHelper.Current.Resources!.Configuration!.Orientation;
				if (orientation == Android.Content.Res.Orientation.Undefined)
				{
					return DisplayOrientations.None;
				}

				var rotation = _cachedRotation;
				bool isLandscape;
				switch (rotation)
				{
					case SurfaceOrientation.Rotation0:
					case SurfaceOrientation.Rotation180:
						isLandscape = orientation == Android.Content.Res.Orientation.Landscape;
						break;
					default:
						isLandscape = orientation == Android.Content.Res.Orientation.Portrait;
						break;
				}
				return isLandscape ? DisplayOrientations.Landscape : DisplayOrientations.Portrait;
			}
		}

		/// <summary>
		/// Sets the CurrentOrientation property
		/// </summary>
		/// <remarks>
		/// Again quite complicated to do on Android, based on accepted solution at
		/// https://stackoverflow.com/questions/10380989/how-do-i-get-the-current-orientation-activityinfo-screen-orientation-of-an-a this SO question
		/// </remarks>
		private DisplayOrientations GetCurrentOrientation()
		{
			using (var windowManager = CreateWindowManager())
			{
				int width = _cachedDisplayMetrics.WidthPixels;
				int height = _cachedDisplayMetrics.HeightPixels;

				if (width == height)
				{
					//square device, can't tell orientation
					return DisplayOrientations.None;
				}

				if (NativeOrientation == DisplayOrientations.Portrait)
				{
					switch (_cachedRotation)
					{
						case SurfaceOrientation.Rotation0:
							return DisplayOrientations.Portrait;
						case SurfaceOrientation.Rotation90:
							return DisplayOrientations.Landscape;
						case SurfaceOrientation.Rotation180:
							return DisplayOrientations.PortraitFlipped;
						case SurfaceOrientation.Rotation270:
							return DisplayOrientations.LandscapeFlipped;
						default:
							//invalid rotation
							return DisplayOrientations.None;
					}
				}
				else if (NativeOrientation == DisplayOrientations.Landscape)
				{
					//device is landscape or square
					switch (_cachedRotation)
					{
						case SurfaceOrientation.Rotation0:
							return DisplayOrientations.Landscape;
						case SurfaceOrientation.Rotation90:
							return DisplayOrientations.Portrait;
						case SurfaceOrientation.Rotation180:
							return DisplayOrientations.LandscapeFlipped;
						case SurfaceOrientation.Rotation270:
							return DisplayOrientations.PortraitFlipped;
						default:
							//invalid rotation
							return DisplayOrientations.None;
					}
				}
				else
				{
					//fallback
					return DisplayOrientations.None;
				}
			}
		}

		private IWindowManager CreateWindowManager()
		{
			if (ContextHelper.Current.GetSystemService(Context.WindowService) is { } windowService)
			{
				return windowService.JavaCast<IWindowManager>();
			}

			throw new InvalidOperationException("Failed to get the system Window Service");
		}

		partial void StartOrientationChanged()
			=> _lastKnownOrientation = CurrentOrientation;

		partial void StartDpiChanged()
			=> _lastKnownDpi = LogicalDpi;

		internal void HandleConfigurationChange()
		{
			RefreshDisplayMetricsCache();
			OnDisplayMetricsChanged();
		}

		private void RefreshDisplayMetricsCache()
		{
			using var displayMetrics = new DisplayMetrics();
			using var windowManager = CreateWindowManager();
			if (windowManager.DefaultDisplay is { } defaultDisplay)
			{

				if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.R)
				{
#pragma warning disable CS0618 // GetRealMetrics is obsolete in API 31
#pragma warning disable CA1422 // Validate platform compatibility
					defaultDisplay.GetRealMetrics(displayMetrics);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // GetRealMetrics is obsolete in API 31
					_cachedDisplayMetrics = new DisplayMetricsCache(displayMetrics);
				}
				else
				{
					_cachedDisplayMetrics = new DisplayMetricsCache(windowManager.CurrentWindowMetrics, Android.Content.Res.Resources.System?.Configuration);
				}
				_cachedRotation = windowManager.DefaultDisplay.Rotation;
			}
			else
			{
				throw new InvalidOperationException("Failed to get the default display information");
			}
		}

		private class DisplayMetricsCache
		{
			public DisplayMetricsCache(DisplayMetrics displayMetric)
			{
				Density = displayMetric.Density;
				DensityDpi = displayMetric.DensityDpi;
				HeightPixels = displayMetric.HeightPixels;
				ScaledDensity = displayMetric.ScaledDensity;
				WidthPixels = displayMetric.WidthPixels;
				Xdpi = displayMetric.Xdpi;
				Ydpi = displayMetric.Ydpi;
			}

			public DisplayMetricsCache(WindowMetrics windowMetric, Android.Content.Res.Configuration? configuration)
			{
				HeightPixels = windowMetric.Bounds.Height();
				WidthPixels = windowMetric.Bounds.Width();
				if (configuration != null)
				{
					Xdpi = configuration.DensityDpi;
					Ydpi = configuration.DensityDpi;
					Density = configuration.DensityDpi / (float)DisplayMetricsDensity.Default;
					ScaledDensity = Density;
					DensityDpi = ConvertIntToDensityEnum(configuration.DensityDpi);
				}
			}

			public float Density { get; }

			public DisplayMetricsDensity DensityDpi { get; }

			public int HeightPixels { get; }

			public float ScaledDensity { get; }

			public int WidthPixels { get; }

			public float Xdpi { get; }

			public float Ydpi { get; }

			private DisplayMetricsDensity ConvertIntToDensityEnum(int DPI)
			{
				return DPI switch
				{
					120 => DisplayMetricsDensity.Low,
					160 => DisplayMetricsDensity.Medium,
					240 => DisplayMetricsDensity.High,
					320 => DisplayMetricsDensity.Xhigh,
					480 => DisplayMetricsDensity.Xxhigh,
					640 => DisplayMetricsDensity.Xxxhigh,

					213 => DisplayMetricsDensity.Tv,

					< 120 => DisplayMetricsDensity.Low,
					< 160 => DisplayMetricsDensity.D140,
					< 180 => DisplayMetricsDensity.D180,
					< 200 => DisplayMetricsDensity.D200,
					< 220 => DisplayMetricsDensity.D220,
					< 260 => DisplayMetricsDensity.D220,
					< 280 => DisplayMetricsDensity.D280,

					< 300 => DisplayMetricsDensity.D300,
					< 340 => DisplayMetricsDensity.D340,
					< 360 => DisplayMetricsDensity.D360,
					< 400 => DisplayMetricsDensity.D400,
					< 420 => DisplayMetricsDensity.D420,
					< 440 => DisplayMetricsDensity.D440,
					< 450 => DisplayMetricsDensity.D450,
					< 560 => DisplayMetricsDensity.D560,

					_ => DisplayMetricsDensity.D600
				};

			}

		}
	}
}
