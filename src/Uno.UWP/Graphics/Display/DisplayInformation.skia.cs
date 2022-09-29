#nullable disable

using System;
using Uno.Foundation.Extensibility;

namespace Windows.Graphics.Display;

public sealed partial class DisplayInformation
{
	private static readonly Lazy<DisplayInformation> _lazyInstance = new Lazy<DisplayInformation>(() => new DisplayInformation());

	private static DisplayInformation InternalGetForCurrentView() => _lazyInstance.Value;

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

	public ResolutionScale ResolutionScale => _displayInformationExtension.ResolutionScale;

	public double? DiagonalSizeInInches => _displayInformationExtension.DiagonalSizeInInches;
}
