using System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Windows.Graphics.Display;

public sealed partial class DisplayInformation
{
	private IDisplayInformationExtension _displayInformationExtension;

	partial void Initialize()
	{
		if (!ApiExtensibility.CreateInstance(this, out _displayInformationExtension))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Unable to find IDisplayInformationExtension extension");
			}
		}
	}

	internal void NotifyDpiChanged() => OnDpiChanged();

	public DisplayOrientations CurrentOrientation => _displayInformationExtension?.CurrentOrientation ?? DisplayOrientations.Landscape;

	public uint ScreenHeightInRawPixels => _displayInformationExtension?.ScreenHeightInRawPixels ?? 1080;

	public uint ScreenWidthInRawPixels => _displayInformationExtension?.ScreenWidthInRawPixels ?? 1920;

	public float LogicalDpi => _displayInformationExtension?.LogicalDpi ?? 96f;

	public double RawPixelsPerViewPixel => _displayInformationExtension?.RawPixelsPerViewPixel ?? 1;

	public ResolutionScale ResolutionScale => _displayInformationExtension?.ResolutionScale ?? ResolutionScale.Scale100Percent;

	public double? DiagonalSizeInInches => _displayInformationExtension?.DiagonalSizeInInches;
}
