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
using Uno.UI.Runtime.Skia.Gtk.Helpers.Windows;
using Uno.UI.Runtime.Skia.Gtk.Helpers.Dpi;
using Windows.Graphics.Display;
using Gdk;
using System.Reflection;
using Gtk;
using Silk.NET.OpenGLES;
using Silk.NET.Core.Loader;
using Silk.NET.Core.Contexts;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Hosting;

namespace Uno.UI.Runtime.Skia.Gtk
{
	internal class OpenGLESRenderSurface : GLRenderSurfaceBase
	{
		private static DefaultNativeContext? _nativeContext;

		private static DefaultNativeContext NativeContext
			=> _nativeContext ??= new Silk.NET.Core.Contexts.DefaultNativeContext(new OpenGLESLibraryNameContainer().GetLibraryName());

		public OpenGLESRenderSurface(IGtkXamlRootHost host) : base(host)
		{
			_glES = new GL(NativeContext);
			_isGLES = true;
		}

		public static bool IsSupported
		{
			get
			{
				// OpenGL support on macOS is currently broken
				var isMacOS = OperatingSystem.IsMacOS();

				// WSL2 is not supported because of a low version for GLSL (https://github.com/unoplatform/uno/issues/8643#issuecomment-1114392827)
				var isWSL2 = OperatingSystem.IsLinux()
					// https://github.com/microsoft/WSL/issues/423#issuecomment-844418910
					&& File.Exists("/proc/sys/kernel/osrelease")
					&& File.ReadAllText("/proc/sys/kernel/osrelease").Trim().EndsWith("WSL2", StringComparison.Ordinal);

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
					using var glContext = CreateGlEsInterface();

					return glContext != null;
				}
				catch (Exception e)
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
			=> CreateGRGLContext();

		internal static GRContext CreateGRGLContext()
		{
			var glInterface = CreateGlEsInterface();

			var context = GRContext.CreateGl(glInterface);

			if (context == null)
			{
				throw new InvalidOperationException($"OpenGL ES is not available on this platform (Context creation failed)");
			}

			return context;
		}

		private static GRGlInterface CreateGlEsInterface()
		{
			var glInterface = GRGlInterface.CreateGles(proc =>
			{
				try
				{
					if (NativeContext.TryGetProcAddress(proc, out var addr))
					{
						return addr;
					}
				}
				catch (Exception e)
				{
					// In this context, the lamda is executed from a native context, where
					// unhandled exceptions terminate the process. In this case, we can simply
					// return NULL, causing glInterface to be null in return.


					if (typeof(OpenGLESRenderSurface).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(OpenGLESRenderSurface).Log().Debug($"OpenGL ES is not available {e.Message}");
					}
				}

				return IntPtr.Zero;
			});

			if (glInterface == null)
			{
				throw new InvalidOperationException($"OpenGL ES is not available on this platform (GL Interface creation failed)");
			}

			return glInterface;
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
