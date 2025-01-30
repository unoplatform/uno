using AppKit;
using Foundation;
using System;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private NSObject _didChangeScreenParametersObserver;

		public DisplayOrientations CurrentOrientation
		{
			get;
			private set;
		}

		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels
		{
			get;
			private set;
		}

		public uint ScreenWidthInRawPixels
		{
			get;
			private set;
		}

		public double RawPixelsPerViewPixel
		{
			get;
			private set;
		}

		/// <summary>
		/// Scale of 1 is considered @1x, which is the equivalent of 96.0 or 100% for UWP.
		/// https://developer.apple.com/documentation/uikit/uiscreen/1617836-scale
		/// </summary>
		public float LogicalDpi
		{
			get;
			private set;
		}

		public ResolutionScale ResolutionScale
		{
			get;
			private set;
		}

		private void Update()
		{
			var screen = NSScreen.MainScreen;
			var rect = screen.ConvertRectToBacking(screen.Frame);

			ScreenHeightInRawPixels = (uint)rect.Height;
			ScreenWidthInRawPixels = (uint)rect.Width;
			RawPixelsPerViewPixel = screen.BackingScaleFactor;

			CurrentOrientation = ScreenWidthInRawPixels > ScreenHeightInRawPixels
				? DisplayOrientations.Landscape : DisplayOrientations.Portrait;

			LogicalDpi = (float)(RawPixelsPerViewPixel * BaseDpi);

			ResolutionScale = (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);
		}

		partial void Initialize()
		{
			NSNotificationCenter
				.DefaultCenter
				.AddObserver(
					NSWindow.DidChangeScreenNotification,
					n =>
					{
						Update();
					}
				);
			// we are notified on changes but we're already on a screen so let's initialize
			Update();
		}

		partial void StartOrientationChanged() => ObserveDisplayMetricsChanges();

		partial void StopOrientationChanged() => UnobserveDisplayMetricsChanges();

		partial void StartDpiChanged() => ObserveDisplayMetricsChanges();

		partial void StopDpiChanged() => UnobserveDisplayMetricsChanges();

		private void ObserveDisplayMetricsChanges()
		{
			_lastKnownOrientation = CurrentOrientation;
			_lastKnownDpi = LogicalDpi;
			if (_didChangeScreenParametersObserver == null)
			{
				_didChangeScreenParametersObserver = NSNotificationCenter
					.DefaultCenter
					.AddObserver(
						NSApplication.DidChangeScreenParametersNotification,
						n =>
						{
							OnDisplayMetricsChanged();
						}
					);
			}
		}

		private void UnobserveDisplayMetricsChanges()
		{
			if (_dpiChanged == null && _orientationChanged == null)
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(_didChangeScreenParametersObserver);
				_didChangeScreenParametersObserver = null;
			}
		}
	}
}
