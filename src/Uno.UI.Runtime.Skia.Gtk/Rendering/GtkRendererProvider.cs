using System;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.Hosting;

namespace Uno.UI.Runtime.Skia.GTK.Rendering;

internal static class GtkRendererProvider
{
	public static IGtkRenderer CreateForHost(IGtkXamlRootHost host)
	{
		var requestedRenderer = host.RenderSurfaceType ?? RenderSurfaceType.OpenGL;

		if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(GtkRendererProvider).Log().Debug($"Validating {host.RenderSurfaceType} rendering");
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

	private static void TryReadRenderSurfaceTypeEnvironment()
	{
		if (Enum.TryParse(Environment.GetEnvironmentVariable("UNO_RENDER_SURFACE_TYPE"), out RenderSurfaceType surfaceType))
		{
			if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(GtkRendererProvider).Log().Debug($"Overriding RnderSurfaceType using command line with {surfaceType}");
			}

			RenderSurfaceType = surfaceType;
		}
	}

	private void SetupRenderSurface()
	{
		TryReadRenderSurfaceTypeEnvironment();

		if (!OpenGLRenderSurface.IsSupported && !OpenGLESRenderSurface.IsSupported)
		{
			// Pre-validation is required to avoid initializing OpenGL on macOS
			// where the whole app may get visually corrupted even if OpenGL is not
			// used in the app.

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Neither OpenGL or OpenGL ES are supporting, using software rendering");
			}

			GtkHost.Current!.RenderSurfaceType = Skia.RenderSurfaceType.Software;
		}

		if (GtkHost.Current!.RenderSurfaceType == null)
		{
			// Create a temporary surface to automatically detect
			// the OpenGL environment that can be used on the system.
			GLValidationSurface validationSurface = new();

			Add(validationSurface);
			ShowAll();

			DispatchNativeSingle(ValidatedSurface);

			async void ValidatedSurface()
			{
				try
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Auto-detecting surface type");
					}

					// Wait for a realization of the GLValidationSurface
					RenderSurfaceType = await validationSurface.GetSurfaceTypeAsync();

					// Continue on the GTK main thread
					DispatchNativeSingle(() =>
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"Auto-detected {RenderSurfaceType} rendering");
						}

						_window.Remove(validationSurface);

						FinalizeStartup();
					});
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Auto-detected failed", e);
					}
				}
			}
		}
		else
		{
			FinalizeStartup();
		}
	}
}
