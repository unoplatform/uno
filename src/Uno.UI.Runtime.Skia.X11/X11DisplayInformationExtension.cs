using System;
using System.Globalization;
using Windows.Graphics.Display;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11DisplayInformationExtension : IDisplayInformationExtension
	{
		private const string EnvironmentUnoDisplayScaleOverride = "UNO_DISPLAY_SCALE_OVERRIDE";
		private const double MillimetersToInches = 0.0393700787;

		private readonly DisplayInformation _displayInformation;
		private readonly float? _scaleOverride;
		private DisplayInformationDetails _details = new DisplayInformationDetails(0, 0, DisplayInformation.BaseDpi, 1, ResolutionScale.Scale100Percent, null);
		private X11Renderer? _renderer;

		private record DisplayInformationDetails(
			uint ScreenWidthInRawPixels,
			uint ScreenHeightInRawPixels,
			float LogicalDpi,
			double RawPixelsPerViewPixel,
			ResolutionScale ResolutionScale,
			double? DiagonalSizeInInches);

		public X11Renderer? Renderer
		{
			get => _renderer;
			set
			{
				_renderer = value;
				UpdateDetails();
			}
		}

		public X11DisplayInformationExtension(object owner, float? scaleOverride)
		{
			_displayInformation = (DisplayInformation)owner;
			_scaleOverride = scaleOverride;

			if (float.TryParse(
				Environment.GetEnvironmentVariable(EnvironmentUnoDisplayScaleOverride),
				NumberStyles.Any,
				CultureInfo.InvariantCulture,
				out var environmentScaleOverride))
			{
				_scaleOverride = environmentScaleOverride;
			}

			UpdateDetails();
		}

		private void UpdateDetails()
		{
			if (_details.ScreenWidthInRawPixels == 0)
			{
				var screenWidthInInches = 11;
				var screenHeightInInches = 11;

				var dpi = DisplayInformation.BaseDpi;

				var diagonalSizeInInches = Math.Sqrt(screenWidthInInches * screenWidthInInches +
					screenHeightInInches * screenHeightInInches);

				var rawScale = _scaleOverride ?? dpi / DisplayInformation.BaseDpi;

				var flooredScale = FloorScale(rawScale);

				_details = new(
					1920,
					1080,
					flooredScale * DisplayInformation.BaseDpi,
					flooredScale,
					(ResolutionScale)(int)(flooredScale * 100.0),
					diagonalSizeInInches
				);
			}
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
				> 5.00f => 5.00f,
				> 4.50f => 4.50f,
				> 4.00f => 4.00f,
				> 3.50f => 3.50f,
				> 3.00f => 3.00f,
				> 2.50f => 2.50f,
				> 2.25f => 2.25f,
				> 2.00f => 2.00f,
				> 1.80f => 1.80f,
				> 1.75f => 1.75f,
				> 1.60f => 1.60f,
				> 1.50f => 1.50f,
				> 1.40f => 1.40f,
				> 1.25f => 1.25f,
				> 1.20f => 1.20f,
				> 1.00f => 1.00f,
				_ => 1.00f,
			};
	}
}
