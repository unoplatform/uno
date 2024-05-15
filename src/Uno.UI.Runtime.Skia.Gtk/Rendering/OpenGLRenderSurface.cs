#nullable enable

using System;
using SkiaSharp;
using Uno.Foundation.Logging;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Silk.NET.Core.Loader;
using Silk.NET.Core.Contexts;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Hosting;

namespace Uno.UI.Runtime.Skia.Gtk
{
	internal class OpenGLRenderSurface : GLRenderSurfaceBase
	{
		private static DefaultNativeContext? _nativeContext;

		private static DefaultNativeContext NativeContext
			=> _nativeContext ??= new Silk.NET.Core.Contexts.DefaultNativeContext(new GLCoreLibraryNameContainer().GetLibraryName());

		public OpenGLRenderSurface(IGtkXamlRootHost host) : base(host)
		{
			SetRequiredVersion(3, 3);

			_gl = new GL(NativeContext);
		}

		public static bool IsSupported
		{
			get
			{
				// OpenGL support on macOS is currently broken
				var isMacOs = OperatingSystem.IsMacOS();

				try
				{
					var isAvailable = NativeContext.TryGetProcAddress("glGetString", out var getString);

					if (typeof(OpenGLRenderSurface).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(OpenGLRenderSurface).Log().Debug($"OpenGL support: isAvailable:{isAvailable} isMacOs:{isMacOs}");
					}

					return isAvailable && !isMacOs;
				}
				catch (Exception e)
				{
					if (typeof(OpenGLRenderSurface).Log().IsEnabled(LogLevel.Information))
					{
						typeof(OpenGLRenderSurface).Log().LogInfo($"OpenGL is not available {e.Message}");
					}

					return false;
				}
			}
		}

		public static void TryValidateExtensions()
		{
			if (typeof(OpenGLRenderSurface).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(OpenGLRenderSurface).Log().Debug($"Validating OpenGL Extensions");
			}

			var extensions = new GL(NativeContext).GetStringS(GLEnum.Extensions);
			var hasARBVertexArrayObject = extensions?.Contains("GL_ARB_vertex_array_object");

			if (hasARBVertexArrayObject.HasValue
				&& hasARBVertexArrayObject == false
				&& typeof(OpenGLRenderSurface).Log().IsEnabled(LogLevel.Error))
			{
				// In this case, the GTK runtime will terminate the app
				// if some OpenGL extensions cannot be found. This can happen
				// when the target video device does not provide the required support.
				//
				// For example, this is the error that may be raised:
				//
				// No provider of glGenVertexArrays found.  Requires one of:
				// Desktop OpenGL 3.0

				// GL_ARB_vertex_array_object
				// OpenGL ES 3.0
				// GL_APPLE_vertex_array_object
				// GL_OES_vertex_array_object


				typeof(OpenGLRenderSurface).Log().Error(
					$"OpenGL support on this system is missing extension \"GL_ARB_vertex_array_object\", you may need to enable software " +
					$"rendering. (https://platform.uno/docs/articles/features/using-skia-gtk.html#changing-the-rendering-target)");
			}

		}

		protected override (int framebuffer, int stencil, int samples) GetGLBuffers()
		{
			_gl.GetInteger(GLEnum.FramebufferBinding, out var framebuffer);
			_gl.GetInteger(GLEnum.Stencil, out var stencil);
			_gl.GetInteger(GLEnum.Samples, out var samples);

			return (framebuffer, stencil, samples);
		}

		protected override GRContext TryBuildGRContext()
			=> CreateGRGLContext();

		internal static GRContext CreateGRGLContext()
		{
			var glInterface = GRGlInterface.Create();

			if (glInterface == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system");
			}

			var context = GRContext.CreateGl(glInterface);

			if (context == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system (failed to create context)");
			}

			return context;
		}

		// Extracted from https://github.com/dotnet/Silk.NET/blob/23f9bd4d67ad21c69fbd69cc38a62fb2c0ec3927/src/OpenGL/Silk.NET.OpenGL/GLCoreLibraryNameContainer.cs
		internal class GLCoreLibraryNameContainer : SearchPathContainer
		{
			/// <inheritdoc />
			public override string Linux => "libGL.so.1";

			/// <inheritdoc />
			public override string MacOS => "/System/Library/Frameworks/OpenGL.framework/OpenGL";

			/// <inheritdoc />
			public override string Android => "libGL.so.1";

			/// <inheritdoc />
			public override string IOS => "/System/Library/Frameworks/OpenGL.framework/OpenGL";

			/// <inheritdoc />
			public override string Windows64 => "opengl32.dll";

			/// <inheritdoc />
			public override string Windows86 => "opengl32.dll";
		}
	}
}
