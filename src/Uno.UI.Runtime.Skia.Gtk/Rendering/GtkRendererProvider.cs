using System;
using System.Threading.Tasks;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Gtk.Hosting;
using Uno.UI.Runtime.Skia.Gtk.UI;

namespace Uno.UI.Runtime.Skia.Gtk.Rendering;

internal static class GtkRendererProvider
{
	public static async Task<IGtkRenderer> CreateForHostAsync(IGtkXamlRootHost host)
	{
		RenderSurfaceType? renderSurfaceType = host.RenderSurfaceType;
		if (TryReadRenderSurfaceTypeEnvironment(out var overridenSurfaceType))
		{
			renderSurfaceType = overridenSurfaceType;
		}

		if (!OpenGLRenderSurface.IsSupported && !OpenGLESRenderSurface.IsSupported)
		{
			// Pre-validation is required to avoid initializing OpenGL on macOS
			// where the whole app may get visually corrupted even if OpenGL is not
			// used in the app.

			if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(GtkRendererProvider).Log().Debug($"Neither OpenGL or OpenGL ES are supporting, using software rendering");
			}

			renderSurfaceType = RenderSurfaceType.Software;
		}

		if (renderSurfaceType is null)
		{
			TaskCompletionSource<RenderSurfaceType> validationCompletionSource = new();

			// Create a temporary surface to automatically detect
			// the OpenGL environment that can be used on the system.
			GLValidationSurface validationSurface = new();

			host.RootContainer.Add(validationSurface);
			host.RootContainer.ShowAll();

			GtkDispatch.DispatchNativeSingle(ValidateSurface, Dispatching.NativeDispatcherPriority.Normal);

			renderSurfaceType = await validationCompletionSource.Task;

			async void ValidateSurface()
			{
				try
				{
					if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(GtkRendererProvider).Log().Debug($"Auto-detecting surface type");
					}

					// Wait for a realization of the GLValidationSurface
					var validatedRenderSurfaceType = await validationSurface.GetSurfaceTypeAsync();

					// Continue on the GTK main thread
					GtkDispatch.DispatchNativeSingle(() =>
					{
						if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Debug))
						{
							typeof(GtkRendererProvider).Log().Debug($"Auto-detected {renderSurfaceType} rendering");
						}

						host.RootContainer.Remove(validationSurface);

						validationCompletionSource.TrySetResult(validatedRenderSurfaceType);
					}, Dispatching.NativeDispatcherPriority.Normal);
				}
				catch (Exception e)
				{
					if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Error))
					{
						typeof(GtkRendererProvider).Log().Error($"Auto-detected failed", e);
					}

					validationCompletionSource.TrySetResult(RenderSurfaceType.Software);
				}
			}
		}

		return BuildRenderSurfaceType(renderSurfaceType.Value, host);
	}

	private static IGtkRenderer BuildRenderSurfaceType(RenderSurfaceType renderSurfaceType, IGtkXamlRootHost host)
		=> renderSurfaceType switch
		{
			RenderSurfaceType.OpenGLES => new OpenGLESRenderSurface(host),
			RenderSurfaceType.OpenGL => new OpenGLRenderSurface(host),
			RenderSurfaceType.Software => new SoftwareRenderSurface(host),
			_ => throw new InvalidOperationException($"Unsupported RenderSurfaceType {GtkHost.Current!.RenderSurfaceType}")
		};

	private static bool TryReadRenderSurfaceTypeEnvironment(out RenderSurfaceType surfaceType)
	{
		if (Enum.TryParse(Environment.GetEnvironmentVariable("UNO_RENDER_SURFACE_TYPE"), out surfaceType))
		{
			if (typeof(GtkRendererProvider).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(GtkRendererProvider).Log().Debug($"Overriding RnderSurfaceType using command line with {surfaceType}");
			}

			return true;
		}

		return false;
	}
}
