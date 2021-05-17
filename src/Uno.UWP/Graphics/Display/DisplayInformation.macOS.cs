using AppKit;
using Foundation;
using System;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private static readonly Lazy<DisplayInformation> _lazyInstance = new Lazy<DisplayInformation>(() => new DisplayInformation());

		private static DisplayInformation InternalGetForCurrentView() => _lazyInstance.Value;

		private NSObject _didChangeScreenParametersObserver = null;

		public DisplayOrientations CurrentOrientation
		{
			get
			{
				if (NSScreen.MainScreen.Frame.Width > NSScreen.MainScreen.Frame.Height)
				{
					return DisplayOrientations.Landscape;
				}
				else
				{
					return DisplayOrientations.Portrait;
				}
			}
		}

		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels
		{
			get
			{
				var screenSize = NSScreen.MainScreen.Frame.Size;
				var scale = NSScreen.MainScreen.BackingScaleFactor;
				return (uint)(screenSize.Height * scale);
			}
		}

		public uint ScreenWidthInRawPixels
		{
			get
			{
				var screenSize = NSScreen.MainScreen.Frame.Size;
				var scale = NSScreen.MainScreen.BackingScaleFactor;
				return (uint)(screenSize.Width * scale);
			}
		}

		public double RawPixelsPerViewPixel => NSScreen.MainScreen.BackingScaleFactor;

		public float LogicalDpi
		{
			get
			{
				// Scale of 1 is considered @1x, which is the equivalent of 96.0 or 100% for UWP.
				// https://developer.apple.com/documentation/uikit/uiscreen/1617836-scale
				return (float)(NSScreen.MainScreen.BackingScaleFactor * 96.0f);
			}
		}

		public ResolutionScale ResolutionScale => (ResolutionScale)(int)(NSScreen.MainScreen.BackingScaleFactor * 100.0);

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
