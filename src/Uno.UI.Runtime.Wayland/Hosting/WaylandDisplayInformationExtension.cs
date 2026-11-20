#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Wayland;

// Per-window scale comes from the surface (fractional-scale-v1 or preferred_buffer_scale), never output millimetres.
// Resolution and size describe the output the window is on; the window raises DpiChanged when that changes.
internal sealed class WaylandDisplayInformationExtension : IDisplayInformationExtension
{
	private readonly DisplayInformation _owner;
	private readonly WaylandConnection _connection;
	private WaylandWindowWrapper? _window;

	internal WaylandDisplayInformationExtension(object owner, WaylandConnection connection)
	{
		_owner = (DisplayInformation)owner;
		_connection = connection;
	}

	// Resolved lazily: DisplayInformation can be created before the window wrapper is registered.
	private WaylandWindowWrapper? Window
	{
		get
		{
			if (_window is null
				&& AppWindow.GetFromWindowId(_owner.WindowId) is { } appWindow
				&& Microsoft.UI.Xaml.Window.GetFromAppWindow(appWindow) is { } window
				&& _connection.FindWindow(window) is { } wrapper)
			{
				_window = wrapper;
				wrapper.SetDisplayInformationExtension(this);
			}
			return _window;
		}
	}

	private double Scale => Window?.Scale ?? 1;

	private WaylandOutputInfo? Output => Window?.OutputInfo ?? _connection.DefaultOutputInfo;

	public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

	public uint ScreenWidthInRawPixels => (uint)Math.Max(Output?.PixelWidth ?? 0, 0);

	public uint ScreenHeightInRawPixels => (uint)Math.Max(Output?.PixelHeight ?? 0, 0);

	public float LogicalDpi => (float)(DisplayInformation.BaseDpi * Scale);

	public double RawPixelsPerViewPixel => Scale;

	public ResolutionScale ResolutionScale => (ResolutionScale)(int)Math.Round(Scale * 100);

	public double? DiagonalSizeInInches => Output?.DiagonalSizeInInches;

	internal void NotifyDpiChanged() => _owner.NotifyDpiChanged();
}
