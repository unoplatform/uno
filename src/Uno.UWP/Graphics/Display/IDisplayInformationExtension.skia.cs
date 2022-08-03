using System;
namespace Windows.Graphics.Display;

public interface IDisplayInformationExtension
{
	DisplayOrientations CurrentOrientation { get; }

	uint ScreenHeightInRawPixels { get; }

	uint ScreenWidthInRawPixels { get; }

	float LogicalDpi { get; }

	double RawPixelsPerViewPixel { get; }

	ResolutionScale ResolutionScale { get; }

	double? DiagonalSizeInInches { get; }
}
