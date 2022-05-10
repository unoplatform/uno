#nullable enable

using System;
using System.IO;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml.Input;
using WUX = Windows.UI.Xaml;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Helpers.Windows;
using Uno.UI.Runtime.Skia.Helpers.Dpi;
using Windows.Graphics.Display;
using Gdk;
using System.Reflection;
using Gtk;
using Silk.NET.OpenGLES;
using Silk.NET.Core.Loader;

namespace Uno.UI.Runtime.Skia
{

	internal class OpenGLESRenderSurface : GLRenderSurfaceBase
	{
		public OpenGLESRenderSurface()
		{
			_glES = new GL(new Silk.NET.Core.Contexts.DefaultNativeContext(new GLCoreLibraryNameContainer().GetLibraryName()));
			_isGLES = true;
		}

		public static bool IsSupported
		{
			get
			{
				// OpenGL support on macOS is currently broken
				var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

				// WSL2 is not supported because of a low version for GLSL (https://github.com/unoplatform/uno/issues/8643#issuecomment-1114392827)
				var isWSL2 = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
					// https://github.com/microsoft/WSL/issues/423#issuecomment-844418910
					&& File.Exists("/proc/sys/kernel/osrelease")
					&& File.ReadAllText("/proc/sys/kernel/osrelease").Trim().EndsWith("WSL2");

				var isGDKGL_GLES = Environment.GetEnvironmentVariable("GDK_GL")?.Equals("gles", StringComparison.OrdinalIgnoreCase) ?? false;

				if (typeof(OpenGLESRenderSurface).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(OpenGLESRenderSurface).Log().LogDebug($"OpenGL ES conditions: isMacOS={isMacOS} isWSL2={isWSL2} isGDKGL_GLES={isGDKGL_GLES}");
				}

				if (isMacOS || (isWSL2 && !isGDKGL_GLES))
				{
					return false;
				}

				try
				{
					var ctx = new Silk.NET.Core.Contexts.DefaultNativeContext(new OpenGLESLibraryNameContainer().GetLibraryName());

					using var glContext = CreateGRGLContext(ctx);

					return glContext != null;
				}
				catch(Exception e)
				{
					if (typeof(OpenGLESRenderSurface).Log().IsEnabled(LogLevel.Information))
					{
						typeof(OpenGLESRenderSurface).Log().LogInfo($"OpenGL ES is not available {e.Message}");
					}

					return false;
				}
			}
		}

		protected override (int framebuffer, int stencil, int samples) GetGLBuffers()
		{
			_glES.GetInteger(GLEnum.FramebufferBinding, out var framebuffer);
			_glES.GetInteger(GLEnum.Stencil, out var stencil);
			_glES.GetInteger(GLEnum.Samples, out var samples);

			return (framebuffer, stencil, samples);
		}

		protected override GRContext TryBuildGRContext()
		{
			var glInterface = CreateGRGLContext(_glES.Context);

			if(glInterface == null)
			{
				throw new InvalidOperationException($"OpenGL ES is not available on this platform");
			}

			return GRContext.CreateGl(glInterface);
		}

		private static GRGlInterface? CreateGRGLContext(Silk.NET.Core.Contexts.INativeContext context)
		{
			return GRGlInterface.CreateGles(proc =>
			{
				if (context.TryGetProcAddress(proc, out var addr))
				{
					return addr;
				}

				return IntPtr.Zero;
			});
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

		// https://github.com/dotnet/Silk.NET/blob/23f9bd4d67ad21c69fbd69cc38a62fb2c0ec3927/src/OpenGL/Silk.NET.OpenGLES/OpenGLESLibraryNameContainer.cs
		internal class OpenGLESLibraryNameContainer : SearchPathContainer
		{
			public override string Linux => "libGLESv2.so";

			public override string MacOS => "/System/Library/Frameworks/OpenGLES.framework/OpenGLES";

			public override string Android => "libGLESv2.so";

			public override string IOS => "/System/Library/Frameworks/OpenGLES.framework/OpenGLES";

			public override string Windows64 => "libGLESv2.dll";

			public override string Windows86 => "libGLESv2.dll";
		}
	}
}
