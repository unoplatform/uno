#nullable enable

using System;
using System.Threading.Tasks;
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
