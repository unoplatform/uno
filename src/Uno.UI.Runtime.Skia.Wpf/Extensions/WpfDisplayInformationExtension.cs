#nullable enable
using System;
using System.Windows;
using System.Windows.Media;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;
using Uno.UI.Runtime.Skia.Wpf.UI.Controls;
using WpfApplication = global::System.Windows.Application;
using WpfWindow = global::System.Windows.Window;

namespace Uno.UI.Runtime.Skia.Wpf
{
	internal class WpfDisplayInformationExtension : IDisplayInformationExtension
	{
		private readonly DisplayInformation _displayInformation;

		private WpfWindow? _window;
		private float? _dpi;

		public WpfDisplayInformationExtension(object owner)
		{
			_displayInformation = (DisplayInformation)owner;
			GetWindow().Activated += Current_Activated;
		}

		private void Current_Activated(object? sender, EventArgs e)
		{
			GetWindow().DpiChanged += OnDpiChanged;
		}

		private WpfWindow GetWindow()
		{
			if (_window is { })
			{
				return _window;
			}

			// TODO: this is a ridiculous amount of indirection, find something better
			if (AppWindow.GetFromWindowId(_displayInformation.WindowId) is not { } appWindow ||
				Microsoft.UI.Xaml.Window.GetFromAppWindow(appWindow) is not { } window ||
				UnoWpfWindow.GetGtkWindowFromWindow(window) is not { } gtkWindow)
			{
				throw new InvalidOperationException($"{nameof(WpfDisplayInformationExtension)} couldn't find a GTK window.");
			}

			_window = gtkWindow;
			return _window;
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
