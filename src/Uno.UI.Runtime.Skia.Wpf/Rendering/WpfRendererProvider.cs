using System;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Hosting;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal static class WpfRendererProvider
{
	public static IWpfRenderer CreateForHost(IWpfXamlRootHost host)
	{
		var requestedRenderer = host.RenderSurfaceType ?? RenderSurfaceType.OpenGL;

		if (typeof(WpfRendererProvider).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(WpfRendererProvider).Log().Debug($"Validating {host.RenderSurfaceType} rendering");
		}

		IWpfRenderer renderer = null;
		while (renderer is null)
		{
			renderer = requestedRenderer switch
			{
				RenderSurfaceType.Software => new SoftwareWpfRenderer(host),
				RenderSurfaceType.OpenGL => new OpenGLWpfRenderer(host),
				_ => throw new InvalidOperationException($"Render Surface type {host.RenderSurfaceType} is not supported")
			};

			if (!renderer.TryInitialize())
			{
				renderer.Dispose();
				renderer = null;

				// OpenGL initialization failed, fallback to software rendering
				// This may happen on headless systems or containers.

				if (typeof(WpfRendererProvider).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(WpfRendererProvider).Log().Warn($"OpenGL failed to initialize, using software rendering");
				}

				requestedRenderer = RenderSurfaceType.Software;
			}
		}

		return renderer;
	}
}
