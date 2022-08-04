#nullable enable
using System;
using System.Windows;
using System.Windows.Media;
using Windows.Graphics.Display;
using WpfApplication = global::System.Windows.Application;

namespace Uno.UI.Skia.Platform
{
	internal class WpfDisplayInformationExtension : IDisplayInformationExtension
	{
		private readonly DisplayInformation _displayInformation;

		private float? _dpi = null;

		public WpfDisplayInformationExtension(object owner)
		{
			_displayInformation = (DisplayInformation)owner;
			WpfApplication.Current.MainWindow.DpiChanged += OnDpiChanged;
		}

		public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels => 0;

		public uint ScreenWidthInRawPixels => 0;

		public float LogicalDpi => _dpi ??= GetDpi();

		public double RawPixelsPerViewPixel => LogicalDpi / DisplayInformation.BaseDpi;

		public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

		public double? DiagonalSizeInInches => null;

		private void OnDpiChanged(object sender, DpiChangedEventArgs e)
		{
			_dpi = GetDpi();
			_displayInformation.NotifyDpiChanged();
		}

		private float GetDpi()
		{
			var dpi = VisualTreeHelper.GetDpi(WpfApplication.Current.MainWindow);
			return (float)Math.Max(dpi.DpiScaleX, dpi.DpiScaleY) * DisplayInformation.BaseDpi;
		}
	}
}
