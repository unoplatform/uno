using System;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Skia;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal static class WpfRendererProvider
{
	public static IWpfRenderer CreateForHost(IWpfXamlRootHost host)
	{
		// TODO:MZ: Do this only once, not for every window
		if (WpfHost.Current!.RenderSurfaceType is null)
		{
			WpfHost.Current!.RenderSurfaceType = RenderSurfaceType.OpenGL;
		}

		if (typeof(WpfRendererProvider).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(WpfRendererProvider).Log().Debug($"Using {WpfHost.Current!.RenderSurfaceType} rendering");
		}

		IWpfRenderer renderer = WpfHost.Current!.RenderSurfaceType switch
		{
			RenderSurfaceType.Software => new SoftwareWpfRenderer(host),
			RenderSurfaceType.OpenGL => new OpenGLWpfRenderer(host),
			_ => throw new InvalidOperationException($"Render Surface type {WpfHost.Current!.RenderSurfaceType} is not supported")
		};

		if (!renderer.TryInitialize())
		{
			// OpenGL initialization failed, fallback to software rendering
			// This may happen on headless systems or containers.

			if (typeof(WpfRendererProvider).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(WpfRendererProvider).Log().Warn($"OpenGL failed to initialize, using software rendering");
			}

			WpfHost.Current!.RenderSurfaceType = RenderSurfaceType.Software;
			return CreateForHost(host);
		}
		else
		{
			return renderer;
		}
	}
}
