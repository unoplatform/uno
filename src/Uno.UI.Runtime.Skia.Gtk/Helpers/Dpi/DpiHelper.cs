using System;
using System.Runtime.InteropServices;
using Gtk;
using Uno.Helpers;
using Uno.UI.Runtime.Skia.Helpers.Windows;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.Helpers.Dpi
{
	internal class DpiHelper
	{
		private readonly Window _window;
		private readonly StartStopEventWrapper<EventHandler> _dpiChangedWrapper;

		private float? _dpi;

		public DpiHelper(Window window)
		{
			_window = window;
			_dpiChangedWrapper = new(StartDpiChanged, StopDpiChanged);
		}

		public event EventHandler DpiChanged
		{
			add => _dpiChangedWrapper.AddHandler(value);
			remove => _dpiChangedWrapper.RemoveHandler(value);
		}

		public float GetLogicalDpi() => _dpi ??= GetNativeDpi();

		private void StartDpiChanged()
		{
			_window.AddEvents((int)Gdk.EventType.Configure);
			_window.Screen.MonitorsChanged += OnMonitorsChanged;
			_window.ScreenChanged += OnScreenChanged;
			_window.SizeAllocated += OnWindowSizeAllocated;
			_window.ConfigureEvent += OnWindowConfigure;
			_window.Screen.SizeChanged += OnScreenSizeChanged;
		}

		private void StopDpiChanged()
		{
			_window.Screen.MonitorsChanged -= OnMonitorsChanged;
			_window.ScreenChanged -= OnScreenChanged;
			_window.SizeAllocated -= OnWindowSizeAllocated;
			_window.ConfigureEvent -= OnWindowConfigure;
			_window.Screen.SizeChanged -= OnScreenSizeChanged;
		}

		private void OnWindowConfigure(object o, ConfigureEventArgs args) => CheckDpiUpdate();

		private void OnWindowSizeAllocated(object o, SizeAllocatedArgs args) => CheckDpiUpdate();

		private void OnMonitorsChanged(object sender, EventArgs e) => CheckDpiUpdate();

		private void OnScreenChanged(object o, ScreenChangedArgs args) => CheckDpiUpdate();

		private void OnScreenSizeChanged(object sender, EventArgs e) => CheckDpiUpdate();

		private void CheckDpiUpdate()
		{
			var dpi = GetNativeDpi();
			if (dpi != _dpi)
			{
				_dpi = dpi;
				_dpiChangedWrapper.Event?.Invoke(this, EventArgs.Empty);
			}
		}

		private float GetNativeDpi()
		{
			if (_window.Window == null)
			{
				return DisplayInformation.BaseDpi; // GDK Window not initialized yet
			}

			float dpi;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				dpi = DpiUtilities.GetDpiForWindow(DpiUtilities.GetWin32Hwnd(_window.Window));
			}
			else
			{
				dpi = _window.Display.GetMonitorAtWindow(_window.Window).ScaleFactor * DisplayInformation.BaseDpi;
			}
			return dpi;
		}
	}
}
