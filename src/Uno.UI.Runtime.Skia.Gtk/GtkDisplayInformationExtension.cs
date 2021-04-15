#nullable enable
using System.Runtime.InteropServices;
using Gtk;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia
{
	internal class GtkDisplayInformationExtension : IDisplayInformationExtension
	{
		private readonly DisplayInformation _displayInformation;
		private readonly Window _window;

		public GtkDisplayInformationExtension(object owner, Gtk.Window window)
		{
			_displayInformation = (DisplayInformation)owner;
			_window = window;
		}

		public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels => (uint)_window.Display.GetMonitorAtWindow(_window.Window).Workarea.Height;

		public uint ScreenWidthInRawPixels => (uint)_window.Display.GetMonitorAtWindow(_window.Window).Workarea.Width;

		public float LogicalDpi
		{
			get
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					return (float)_window.Screen.Resolution;
				}
				else
				{
					return (float)_window.Display.GetMonitorAtWindow(_window.Window).ScaleFactor * 96.0f;
				}
			}
		}

		public double RawPixelsPerViewPixel => LogicalDpi / 96.0f;

		public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

		public void StartDpiChanged()
		{
			_window.ScreenChanged += OnScreenChanged;
		}

		private void OnScreenChanged(object o, ScreenChangedArgs args)
		{
			_displayInformation.NotifyDpiChanged();
		}

		public void StopDpiChanged()
		{
			_window.ScreenChanged -= OnScreenChanged;
		}
	}
}
