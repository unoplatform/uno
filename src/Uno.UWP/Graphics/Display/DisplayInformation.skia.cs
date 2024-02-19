using System;
using Uno.Foundation.Extensibility;

namespace Windows.Graphics.Display;

public sealed partial class DisplayInformation
{
	private IDisplayInformationExtension _displayInformationExtension;

	partial void Initialize()
	{
		if (!ApiExtensibility.CreateInstance(this, out _displayInformationExtension))
		{
			throw new InvalidOperationException($"Unable to find IDisplayInformationExtension extension");
		}
	}

	internal void NotifyDpiChanged() => OnDpiChanged();

	public DisplayOrientations CurrentOrientation => _displayInformationExtension.CurrentOrientation;

	public uint ScreenHeightInRawPixels => _displayInformationExtension.ScreenHeightInRawPixels;

	public uint ScreenWidthInRawPixels => _displayInformationExtension.ScreenWidthInRawPixels;

	public float LogicalDpi => _displayInformationExtension.LogicalDpi;

	public double RawPixelsPerViewPixel => _displayInformationExtension.RawPixelsPerViewPixel;

	/// <summary>
	/// This is used to adjust the sizing of managed vs. native elements on GTK, as it does not have built-in support for fractional scaling
	/// which is available on Windows. We can still emulate this by up-scaling native GTK controls by the ratio between the actual scale 
	/// and the emulated scale.
	/// </summary>
	internal double FractionalScaleAdjustment => _displayInformationExtension.RawPixelsPerViewPixel / Math.Truncate(_displayInformationExtension.RawPixelsPerViewPixel);

	public ResolutionScale ResolutionScale => _displayInformationExtension.ResolutionScale;

	public double? DiagonalSizeInInches => _displayInformationExtension.DiagonalSizeInInches;
}
