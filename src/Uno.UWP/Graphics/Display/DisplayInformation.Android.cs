#if __ANDROID__
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
		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			var currentActivity = ContextHelper.Current as Activity;
			if (currentActivity != null)
			{
				currentActivity.RequestedOrientation = orientations.ToScreenOrientation();
			}
		}

		public DisplayOrientations CurrentOrientation => GetCurrentOrientation();

		
		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation => GetNativeOrientation();


		public uint ScreenHeightInRawPixels
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					return (uint)realDisplayMetrics.HeightPixels;
				}
			}
		}

		public uint ScreenWidthInRawPixels
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					return (uint)realDisplayMetrics.WidthPixels;
				}
			}
		}
		
		public double RawPixelsPerViewPixel
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					return 1.0f * (int)realDisplayMetrics.DensityDpi / (int)DisplayMetricsDensity.Default;
				}
			}
		}

		public float LogicalDpi
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					// DisplayMetrics of 1.0 matches 100%, or UWP's default 96.0 DPI.
					// https://stuff.mit.edu/afs/sipb/project/android/docs/reference/android/util/DisplayMetrics.html#density
					return realDisplayMetrics.Density * BaseDpi;
				}
			}
		}

		public ResolutionScale ResolutionScale
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					return (ResolutionScale)(int)(realDisplayMetrics.Density * 100);
				}
			}
		}

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the x axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpix#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		public float RawDpiX
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					return realDisplayMetrics.Xdpi;
				}
			}
		}

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the y axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpiy#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		public float RawDpiY
		{
			get
			{
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					return realDisplayMetrics.Ydpi;
				}
			}
		}

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
				using (var realDisplayMetrics = CreateRealDisplayMetrics())
				{
					var x = Math.Pow((uint)realDisplayMetrics.WidthPixels / realDisplayMetrics.Xdpi, 2);
					var y = Math.Pow((uint)realDisplayMetrics.HeightPixels / realDisplayMetrics.Ydpi, 2);
					var screenInches = Math.Sqrt(x + y);
					return screenInches;
				}
			}
		}

		/// <summary>
		/// Sets the NativeOrientation property
		/// </summary>
		/// <param name="windowManager">Window manager</param>
		/// <remarks>
		/// Based on responses in
		/// <see cref="https://stackoverflow.com/questions/4553650/how-to-check-device-natural-default-orientation-on-android-i-e-get-landscape">this SO question</see>
		/// </remarks>
		private DisplayOrientations GetNativeOrientation()
		{
			using (var windowManager = CreateWindowManager())
			{
				var orientation = ContextHelper.Current.Resources.Configuration.Orientation;
				if (orientation == Android.Content.Res.Orientation.Undefined)
				{
					return DisplayOrientations.None;
				}

				var rotation = windowManager.DefaultDisplay.Rotation;
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
		/// <param name="windowManager">Window manager</param>
		/// <remarks>
		/// Again quite complicated to do on Android, based on accepted solution at
		/// <see cref="https://stackoverflow.com/questions/10380989/how-do-i-get-the-current-orientation-activityinfo-screen-orientation-of-an-a">this SO question</see>
		/// </remarks>
		private DisplayOrientations GetCurrentOrientation()
		{
			using (var windowManager = CreateWindowManager())
			{
				var rotation = windowManager.DefaultDisplay.Rotation;
				using (var displayMetrics = new DisplayMetrics())
				{
#pragma warning disable 618
					windowManager.DefaultDisplay.GetMetrics(displayMetrics);
#pragma warning restore 618

					int width = displayMetrics.WidthPixels;
					int height = displayMetrics.HeightPixels;

					if (width == height)
					{
						//square device, can't tell orientation
						return DisplayOrientations.None;
					}

					if (NativeOrientation == DisplayOrientations.Portrait)
					{
						switch (rotation)
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
						switch (rotation)
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
		}

		private IWindowManager CreateWindowManager()
		{
			return ContextHelper.Current.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
		}

		private DisplayMetrics CreateRealDisplayMetrics()
		{
			var displayMetrics = new DisplayMetrics();
			using (var windowManager = CreateWindowManager())
			{
				windowManager.DefaultDisplay.GetRealMetrics(displayMetrics);
			}
			return displayMetrics;
		}

		partial void StartOrientationChanged()
		{
			_lastKnownOrientation = CurrentOrientation;
		}

		partial void StartDpiChanged()
		{
			_lastKnownDpi = LogicalDpi;
		}

		internal void HandleConfigurationChange()
		{
			OnDisplayMetricsChanged();
		}
	}
}
#endif
