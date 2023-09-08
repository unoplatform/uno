#nullable enable
using System;
using System.Windows;
using System.Windows.Media;
using Windows.Graphics.Display;
using WpfApplication = global::System.Windows.Application;

namespace Uno.UI.Runtime.Skia.Wpf
{
	internal class WpfDisplayInformationExtension : IDisplayInformationExtension
	{
		private readonly DisplayInformation _displayInformation;

		private float? _dpi;

		public WpfDisplayInformationExtension(object owner)
		{
			_displayInformation = (DisplayInformation)owner;
			WpfApplication.Current.Activated += Current_Activated;
		}

		private void Current_Activated(object? sender, EventArgs e)
		{
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
			//TODO:MZ: Get DPI for each window separately
			var dpi = VisualTreeHelper.GetDpi(WpfApplication.Current.Windows[0]);
			return (float)Math.Max(dpi.DpiScaleX, dpi.DpiScaleY) * DisplayInformation.BaseDpi;
		}
	}
}
