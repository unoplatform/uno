using System;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.Hosting;

namespace Uno.UI.Runtime.Skia.GTK.Rendering;

internal static class GTKRendererProvider
{
	public static IGtkRenderer CreateForHost(IGtkXamlRootHost host)
	{
		var requestedRenderer = host.RenderSurfaceType ?? RenderSurfaceType.OpenGL;

		if (typeof(GTKRendererProvider).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(GTKRendererProvider).Log().Debug($"Validating {host.RenderSurfaceType} rendering");
		}

		IGtkRenderer renderer = null;
		while (renderer is null)
		{
			renderer = requestedRenderer switch
			{
				RenderSurfaceType.Software => new SoftwareGTKRenderer(host),
				RenderSurfaceType.OpenGL => new OpenGLGTKRenderer(host),
				_ => throw new InvalidOperationException($"Render Surface type {host.RenderSurfaceType} is not supported")
			};

			if (!renderer.TryInitialize())
			{
				renderer.Dispose();
				renderer = null;

				// OpenGL initialization failed, fallback to software rendering
				// This may happen on headless systems or containers.

				if (typeof(GTKRendererProvider).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(GTKRendererProvider).Log().Warn($"OpenGL failed to initialize, using software rendering");
				}

				requestedRenderer = RenderSurfaceType.Software;
			}
		}

		return renderer;
	}

	private static IGtkRenderer BuildRenderSurfaceType(RenderSurfaceType renderSurfaceType, IGtkXamlRootHost host)
		=> renderSurfaceType switch
		{
			Skia.RenderSurfaceType.OpenGLES => new OpenGLESRenderSurface(host),
			Skia.RenderSurfaceType.OpenGL => new OpenGLRenderSurface(host),
			Skia.RenderSurfaceType.Software => new SoftwareRenderSurface(host),
			_ => throw new InvalidOperationException($"Unsupported RenderSurfaceType {GtkHost.Current!.RenderSurfaceType}")
		};
}
