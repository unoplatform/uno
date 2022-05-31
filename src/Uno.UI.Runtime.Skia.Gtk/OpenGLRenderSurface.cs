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
using Silk.NET.OpenGL;
using Silk.NET.Core.Loader;

namespace Uno.UI.Runtime.Skia
{

	internal class OpenGLRenderSurface : GLRenderSurfaceBase
	{
		public OpenGLRenderSurface()
		{
			SetRequiredVersion(3, 3);

			_gl = new GL(new Silk.NET.Core.Contexts.DefaultNativeContext(new GLCoreLibraryNameContainer().GetLibraryName()));
		}

		public static bool IsSupported
		{
			get
			{
				// OpenGL support on macOS is currently broken
				var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

				try
				{
					var ctx = new Silk.NET.Core.Contexts.DefaultNativeContext(new GLCoreLibraryNameContainer().GetLibraryName());

					var isAvailable = ctx.TryGetProcAddress("glGetString", out _);

					return isAvailable && !isMacOs;
				}
				catch(Exception e)
				{
					if (typeof(OpenGLESRenderSurface).Log().IsEnabled(LogLevel.Information))
					{
						typeof(OpenGLESRenderSurface).Log().LogInfo($"OpenGL is not available {e.Message}");
					}

					return false;
				}
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
		{
			var glInterface = GRGlInterface.Create();

			if(glInterface == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system");
			}

			return GRContext.CreateGl(glInterface);
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
