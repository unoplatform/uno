using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSDisplayInformationExtension : IDisplayInformationExtension
{
	internal MacOSDisplayInformationExtension(object _)
	{
		MacOSWindowNative.NativeWindowReady += NativeWindowReady;
	}

	private void NativeWindowReady(object? sender, MacOSWindowNative e)
	{
		MacOSWindowNative.NativeWindowReady -= NativeWindowReady;
		e.Host.DisplayInformationExtension = this;
		// post a notification so the values are initialized before we need them
		NativeUno.uno_window_notify_screen_change(e.Handle);
	}

	internal void Update(uint width, uint height, double scaleFactor)
	{
		ScreenWidthInRawPixels = width;
		ScreenHeightInRawPixels = height;
		RawPixelsPerViewPixel = scaleFactor;
		// update calculated fields from the native/shared ones
		CurrentOrientation = ScreenWidthInRawPixels > ScreenHeightInRawPixels ? DisplayOrientations.Landscape : DisplayOrientations.Portrait;
		LogicalDpi = (float)RawPixelsPerViewPixel * /* DisplayInformation.BaseDpi */ 96.0f;
		ResolutionScale = (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);
	}

	public DisplayOrientations CurrentOrientation { get; private set; }

	public uint ScreenHeightInRawPixels { get; private set; }

	public uint ScreenWidthInRawPixels { get; private set; }

	public float LogicalDpi { get; private set; }

	public double RawPixelsPerViewPixel { get; private set; }

	public ResolutionScale ResolutionScale { get; private set; }

	public double? DiagonalSizeInInches => null;
}
