using System;
using System.Globalization;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;
using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	internal class DisplayInformationExtension : IDisplayInformationExtension
	{
		private const string EnvironmentUnoDisplayScaleOverride = "UNO_DISPLAY_SCALE_OVERRIDE";
		private const double MillimetersToInches = 0.0393700787;

		private DisplayInformationDetails _details = new DisplayInformationDetails(0, 0, DisplayInformation.BaseDpi, 1, ResolutionScale.Scale100Percent, null);

		private record DisplayInformationDetails(
			uint ScreenWidthInRawPixels,
			uint ScreenHeightInRawPixels,
			float LogicalDpi,
			double RawPixelsPerViewPixel,
			ResolutionScale ResolutionScale,
			double? DiagonalSizeInInches);

		public DisplayInformationExtension(object owner, float? scaleOverride)
		{
			if (float.TryParse(
				Environment.GetEnvironmentVariable(EnvironmentUnoDisplayScaleOverride),
				NumberStyles.Any,
				CultureInfo.InvariantCulture,
				out var environmentScaleOverride))
			{
				scaleOverride = environmentScaleOverride;
			}

			var rawScale = scaleOverride ?? 1;

			var flooredScale = FloorScale(rawScale);

			var screenSize = FrameBufferWindowWrapper.Instance.Size;
			FrameBufferWindowWrapper.Instance.Window!.AppWindow.Changed += OnSizeChanged;
			_details = new(
				(uint)screenSize.Width,
				(uint)screenSize.Height,
				flooredScale * DisplayInformation.BaseDpi,
				flooredScale,
				(ResolutionScale)(int)(flooredScale * 100.0),
				0
			);

			this.LogDebug()?.Debug($"Display Information init: " +
			                       $"ResolutionScale: {ResolutionScale}, " +
			                       $"LogicalDpi: {LogicalDpi}, " +
			                       $"RawPixelsPerViewPixel: {RawPixelsPerViewPixel}, " +
			                       $"DiagonalSizeInInches: {DiagonalSizeInInches}, " +
			                       $"ScreenInRawPixels: {ScreenWidthInRawPixels}x{ScreenHeightInRawPixels}");
		}

		private void OnSizeChanged(AppWindow appWindow, AppWindowChangedEventArgs args)
		{
			_details = _details with
			{
				ScreenWidthInRawPixels = (uint)FrameBufferWindowWrapper.Instance.Size.Width,
				ScreenHeightInRawPixels = (uint)FrameBufferWindowWrapper.Instance.Size.Height,
			};
			this.LogDebug()?.Debug($"Display Information updated: " +
			                       $"ResolutionScale: {ResolutionScale}, " +
			                       $"LogicalDpi: {LogicalDpi}, " +
			                       $"RawPixelsPerViewPixel: {RawPixelsPerViewPixel}, " +
			                       $"DiagonalSizeInInches: {DiagonalSizeInInches}, " +
			                       $"ScreenInRawPixels: {ScreenWidthInRawPixels}x{ScreenHeightInRawPixels}");
		}

		public DisplayOrientations CurrentOrientation
			=> DisplayOrientations.Landscape;

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

		private static float FloorScale(float rawDpi)
			=> rawDpi switch
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
}
