using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Graphics.Display;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;

namespace Uno.UI.Runtime.Skia;

public class FramebufferHostBuilder : IPlatformHostBuilder
{
	internal FramebufferHostBuilder()
	{
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsLinux();

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new FrameBufferHost(appBuilder, this);

	/// <summary>
	/// Shows the mouse cursor as a small circle. If this method is not called,
	/// then by default, the cursor will be shown only after the first mouse
	/// event received from libinput and will not be shown if only touch events
	/// are received. This behavior is useful if you're using a touch screen
	/// and don't need to see a cursor.
	/// </summary>
	public FramebufferHostBuilder EnableMouseCursor(float radius, Color color)
	{
		MouseCursorRadius = radius;
		MouseCursorColor = color;
		ShowMouseCursor = true;
		return this;
	}

	/// <summary>
	/// Hides the mouse cursor. If this method is not called,
	/// then by default, the cursor will be shown only after the first mouse
	/// event received from libinput and will not be shown if only touch events
	/// are received. This behavior is useful if you're using a touch screen
	/// and don't need to see a cursor.
	/// </summary>
	public FramebufferHostBuilder DisableMouseCursor()
	{
		ShowMouseCursor = false;
		return this;
	}

	public FramebufferHostBuilder Orientation(DisplayOrientations orientation)
	{
		DisplayOrientation = orientation;
		return this;
	}

	/// <summary>
	/// Determines if OpenGLES+EGL initialized with DRM+GBM should be used for hardware-accelerated rendering on the
	/// Linux Framebuffer target instead of software rendering. If not called, we try to create an OpenGLES context if possible.
	/// Otherwise, software rendering will be used.
	/// </summary>
	/// <param name="cardPath">The path to the DRM device file. If null, the first device found of the form /dev/dri/cardX will be used.</param>
	/// <param name="connectorChooser">A delegate that picks which of the available connectors to use. If not supplied, the first one found will be used.</param>
	/// <param name="gbmSurfaceColorFormat">
	/// The FourCC color format used for the GBM surface created for rendering
	/// (this is passed to gbm_surface_create). For more details on the FourCC
	/// format and valid values, see https://github.com/torvalds/linux/blob/master/include/uapi/drm/drm_fourcc.h
	/// </param>
	public FramebufferHostBuilder UseKMSDRM(string? cardPath = null, DRMFourCCColorFormat? gbmSurfaceColorFormat = null, DRMConnectorChooserDelegate? connectorChooser = null)
	{
		UseDRM = true;
		DRMCardPath = cardPath;
		GBMSurfaceColorFormat = gbmSurfaceColorFormat ?? DRMFourCCColorFormat.Argb8888;
		DRMConnectorChooser = connectorChooser;
		return this;
	}

	/// <summary>
	/// Disables the usage of KMS/DRM for hardware acceleration and forces software rendering. 
	/// </param>
	public FramebufferHostBuilder DisableKMSDRM()
	{
		UseDRM = false;
		return this;
	}

	/// <summary>
	/// Sets the RMLVO parameters to be passed to libxkbcommon's xkb_rule_names for keyboard keymap creation. If unset,
	/// the system default is used.
	/// For more details on RMLVO, see https://xkbcommon.org/doc/current/xkb-intro.html#RMLVO-intro
	/// and https://github.com/xkbcommon/libxkbcommon/blob/99e9b0fc558fb838a04c568bea033c52ffbe704b/include/xkbcommon/xkbcommon.h#L468
	/// </summary>
	public FramebufferHostBuilder XkbKeymap(XKBKeymapParams keymapParams)
	{
		KeymapParams = keymapParams;
		return this;
	}

	internal XKBKeymapParams KeymapParams { get; private set; }

	internal bool? ShowMouseCursor { get; private set; }

	internal Color MouseCursorColor { get; private set; } = Color.FromArgb(255, 0, 0, 0);

	internal float MouseCursorRadius { get; private set; } = 5;

	internal DisplayOrientations DisplayOrientation { get; private set; } = DisplayOrientations.Landscape;

	internal bool? UseDRM { get; private set; }

	internal string? DRMCardPath { get; private set; }

	internal DRMFourCCColorFormat GBMSurfaceColorFormat { get; private set; } = DRMFourCCColorFormat.Argb8888;

	internal DRMConnectorChooserDelegate? DRMConnectorChooser { get; private set; }

	public readonly record struct DRMFourCCColorFormat(char C1, char C2, char C3, char C4)
	{
		internal uint ToInt() => (uint)C1 | (uint)C2 << 8 | (uint)C3 << 16 | (uint)C4 << 24;

		internal static DRMFourCCColorFormat Argb8888 { get; } = new('A', 'R', '2', '4');
	}

	public readonly record struct DRMConnector(uint connectorType, uint connectorTypeId, uint connectorId, string connectorStringRepresentation);

	/// <returns>The index of the chosen connector or -1.</returns>
	public delegate int DRMConnectorChooserDelegate(IReadOnlyList<DRMConnector> connector);

	public readonly record struct XKBKeymapParams(string? model = null, string? rules = null, string? layout = null, string? variant = null, string? options = null);
}
