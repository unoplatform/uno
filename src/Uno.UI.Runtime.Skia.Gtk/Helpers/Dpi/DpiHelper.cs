using System;
using System.Runtime.InteropServices;
using Gtk;
using Uno.Helpers;
using Uno.UI.Runtime.Skia.Gtk.Helpers.Windows;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.Gtk.Helpers.Dpi;

internal class DpiHelper
{
	private readonly StartStopEventWrapper<EventHandler> _dpiChangedWrapper;

	private float? _dpi;

	public DpiHelper()
	{
		_dpiChangedWrapper = new(StartDpiChanged, StopDpiChanged);
	}

	public event EventHandler DpiChanged
	{
		add => _dpiChangedWrapper.AddHandler(value);
		remove => _dpiChangedWrapper.RemoveHandler(value);
	}

	private void StartDpiChanged()
	{
		GetWindow().AddEvents((int)Gdk.EventType.Configure);
		GetWindow().Screen.MonitorsChanged += OnMonitorsChanged;
		GetWindow().ScreenChanged += OnScreenChanged;
		GetWindow().SizeAllocated += OnWindowSizeAllocated;
		GetWindow().ConfigureEvent += OnWindowConfigure;
		GetWindow().Screen.SizeChanged += OnScreenSizeChanged;
	}

	private void StopDpiChanged()
	{
		GetWindow().Screen.MonitorsChanged -= OnMonitorsChanged;
		GetWindow().ScreenChanged -= OnScreenChanged;
		GetWindow().SizeAllocated -= OnWindowSizeAllocated;
		GetWindow().ConfigureEvent -= OnWindowConfigure;
		GetWindow().Screen.SizeChanged -= OnScreenSizeChanged;
	}

	private Window GetWindow() => GtkHost.Current.InitialWindow;

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

	internal float GetNativeDpi()
	{
		if (GetWindow().Window == null)
		{
			return DisplayInformation.BaseDpi; // GDK Window not initialized yet
		}

		float dpi;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			dpi = DpiUtilities.GetDpiForWindow(DpiUtilities.GetWin32Hwnd(GetWindow().Window));
		}
		else
		{
			dpi = GetWindow().Display.GetMonitorAtWindow(GetWindow().Window).ScaleFactor * DisplayInformation.BaseDpi;
		}
		return dpi;
	}
}
