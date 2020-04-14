using AppKit;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		partial void Initialize()
		{			
			UpdateProperties();
		}

		private void UpdateProperties()
		{
			UpdateLogicalProperties();
			UpdateRawProperties();
			UpdateNativeOrientation();
			UpdateCurrentOrientation();
		}

		private void UpdateLogicalProperties()
		{
			// Scale of 1 is considered @1x, which is the equivalent of 96.0 or 100% for UWP.
			LogicalDpi = (float)(NSScreen.MainScreen.BackingScaleFactor * 96.0f);
			ResolutionScale = (ResolutionScale)(int)(NSScreen.MainScreen.BackingScaleFactor * 100.0);
		}

		/// <summary>
		/// Sets ScreenHeightInRawPixels, ScreenWidthInRawPixels and RawPixelsPerViewPixel
		/// </summary>
		private void UpdateRawProperties()
		{
			var screenSize = NSScreen.MainScreen.Frame.Size;
			var scale = NSScreen.MainScreen.BackingScaleFactor;
			ScreenHeightInRawPixels = (uint)(screenSize.Height * scale);
			ScreenWidthInRawPixels = (uint)(screenSize.Width * scale);
			RawPixelsPerViewPixel = NSScreen.MainScreen.BackingScaleFactor;
		}

		/// <summary>
		/// Sets the NativeOrientation property 		
		/// </summary>
		private void UpdateNativeOrientation()
		{
			NativeOrientation = DisplayOrientations.Landscape;
		}

		private void UpdateCurrentOrientation()
		{
			if (NSScreen.MainScreen.Frame.Width > NSScreen.MainScreen.Frame.Height)
			{
				CurrentOrientation = DisplayOrientations.Landscape;
			}
			else
			{
				CurrentOrientation = DisplayOrientations.Portrait;
			}
		}
	}
}