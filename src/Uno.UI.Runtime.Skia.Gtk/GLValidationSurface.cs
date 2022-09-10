#nullable enable

using System;
using System.Threading.Tasks;
using GLib;
using Gtk;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia
{
	/// <summary>
	/// Validation surface for OpenGL rendering, as CreateGRGLContext
	/// needs to be invoked with an active OpenGL context, and that Skia
	/// does additional validation that cannot be extracted at an earlier stage.
	/// </summary>
	internal class GLValidationSurface : GLArea
	{
		private TaskCompletionSource<RenderSurfaceType> _result = new();

		public GLValidationSurface()
		{
			HasDepthBuffer = false;
			HasStencilBuffer = false;
			AutoRender = true;

			Render += GLValidationSurface_Render;
			Realized += GLValidationSurface_Realized;
		}

		private void GLValidationSurface_Realized(object? sender, EventArgs e)
		{
			if (Context == null)
			{
				// In this case, the UI is displaying "Unable to create a GL context"
				// meaning that GTK was not able to be detected. This can happen on Raspberry Pi
				// running using Wayland.
				// This can be adjusted by setting `GDK_BACKEND=x11` to force a fallback on X11 rendering
				// and enable the use of OpenGL (non-ES) rendering.

				if (typeof(GLValidationSurface).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(GLValidationSurface).Log().Debug($"GL Context realization failed");
				}
				
				_result.TrySetResult(RenderSurfaceType.Software);
			}
		}

		private void GLValidationSurface_Render(object o, RenderArgs args)
		{
			if (typeof(GLValidationSurface).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(GLValidationSurface).Log().Debug($"GLValidationSurface: UseEs={UseEs}");
			}

			args.Context.MakeCurrent();

			ValidateOpenGLSupport();
		}

		private void ValidateOpenGLSupport()
		{
			try
			{
				if (OpenGLESRenderSurface.IsSupported)
				{
					using var ctx = OpenGLESRenderSurface.CreateGRGLContext();

					if (ctx != null)
					{
						_result.TrySetResult(RenderSurfaceType.OpenGLES);
						return;
					}
				}
			}
			catch (Exception e)
			{
				if (typeof(GLValidationSurface).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(GLValidationSurface).Log().Debug($"OpenGL ES cannot be used ({e.Message})");
				}
			}

			try
			{
				if (OpenGLRenderSurface.IsSupported)
				{
					using var ctx = OpenGLRenderSurface.CreateGRGLContext();

					if (ctx != null)
					{
						_result.TrySetResult(RenderSurfaceType.OpenGL);
						return;
					}
				}
			}
			catch (Exception e)
			{
				if (typeof(GLValidationSurface).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(GLValidationSurface).Log().Debug($"OpenGL cannot be used ({e.Message})");
				}
			}

			_result.TrySetResult(RenderSurfaceType.Software);
		}

		internal Task<RenderSurfaceType> GetSurfaceTypeAsync()
			=> _result.Task;
	}
}
