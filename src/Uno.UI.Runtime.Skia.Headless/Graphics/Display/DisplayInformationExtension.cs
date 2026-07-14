#nullable enable

using System;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;
using Uno.WinUI.Runtime.Skia.Headless.UI;

namespace Uno.UI.Runtime.Skia.Headless;

internal class DisplayInformationExtension : IDisplayInformationExtension
{
	private readonly DisplayOrientations _orientation;
	private DisplayInformationDetails _details;

	private record DisplayInformationDetails(
		uint ScreenWidthInRawPixels,
		uint ScreenHeightInRawPixels,
		float LogicalDpi,
		double RawPixelsPerViewPixel,
		ResolutionScale ResolutionScale,
		double? DiagonalSizeInInches);

	public DisplayInformationExtension(object owner, DisplayOrientations orientation)
	{
		_orientation = orientation;

		// Honor the exact configured scale for DPI/RawPixelsPerViewPixel; only the ResolutionScale
		// enum is snapped to a valid step, since it can only take discrete WinUI values.
		var scale = HeadlessWindowWrapper.Instance.Scale;
		var flooredScale = FloorScale(scale);

		var screenSize = HeadlessWindowWrapper.Instance.Size;
		if (HeadlessWindowWrapper.Instance.Window is { } window)
		{
			window.AppWindow.Changed += OnSizeChanged;
		}

		_details = new(
			(uint)screenSize.Width,
			(uint)screenSize.Height,
			scale * DisplayInformation.BaseDpi,
			scale,
			(ResolutionScale)(int)(flooredScale * 100.0),
			null
		);
	}

	private void OnSizeChanged(AppWindow appWindow, AppWindowChangedEventArgs args)
	{
		_details = _details with
		{
			ScreenWidthInRawPixels = (uint)HeadlessWindowWrapper.Instance.Size.Width,
			ScreenHeightInRawPixels = (uint)HeadlessWindowWrapper.Instance.Size.Height,
		};
	}

	public DisplayOrientations CurrentOrientation => _orientation;

	public uint ScreenHeightInRawPixels
		=> _details.ScreenHeightInRawPixels;

	public uint ScreenWidthInRawPixels
		=> _details.ScreenWidthInRawPixels;

	public float LogicalDpi => _details.LogicalDpi;

	public double RawPixelsPerViewPixel => _details.RawPixelsPerViewPixel;

	public ResolutionScale ResolutionScale => _details.ResolutionScale;

	public double? DiagonalSizeInInches => _details.DiagonalSizeInInches;

	public void StartDpiChanged()
	{
	}

	public void StopDpiChanged()
	{
	}

	private static float FloorScale(float rawScale)
		=> rawScale switch
		{
			>= 5.00f => 5.00f,
			>= 4.50f => 4.50f,
			>= 4.00f => 4.00f,
			>= 3.50f => 3.50f,
			>= 3.00f => 3.00f,
			>= 2.50f => 2.50f,
			>= 2.25f => 2.25f,
			>= 2.00f => 2.00f,
			>= 1.80f => 1.80f,
			>= 1.75f => 1.75f,
			>= 1.60f => 1.60f,
			>= 1.50f => 1.50f,
			>= 1.40f => 1.40f,
			>= 1.25f => 1.25f,
			>= 1.20f => 1.20f,
			_ => 1.00f,
		};
}
