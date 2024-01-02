using System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Windows.Graphics.Display;

public sealed partial class DisplayInformation
{
	private static readonly Lazy<DisplayInformation> _lazyInstance = new Lazy<DisplayInformation>(() => new DisplayInformation());

	// TODO: make this return a specific instance based on the window corresponding to
	// the ApplicationView whose thread is the same as the thread this method is called from
	private static DisplayInformation InternalGetForCurrentView() => _lazyInstance.Value;

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

	/// <summary>
	/// This is used to adjust the sizing of managed vs. native elements on GTK, as it does not have built-in support for fractional scaling
	/// which is available on Windows. We can still emulate this by up-scaling native GTK controls by the ratio between the actual scale 
	/// and the emulated scale.
	/// </summary>
	internal double FractionalScaleAdjustment => _displayInformationExtension is { } ? _displayInformationExtension.RawPixelsPerViewPixel / Math.Truncate(_displayInformationExtension.RawPixelsPerViewPixel) : 1;

	public ResolutionScale ResolutionScale => _displayInformationExtension?.ResolutionScale ?? ResolutionScale.Scale100Percent;

	public double? DiagonalSizeInInches => _displayInformationExtension?.DiagonalSizeInInches;
}
