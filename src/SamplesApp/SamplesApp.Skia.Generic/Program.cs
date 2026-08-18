#nullable enable

using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Runtime.Skia.Win32;
using Uno.WinUI.Runtime.Skia.X11;

namespace SkiaSharpExample
{
	class MainClass
	{
		static SamplesApp.App? _app;

		[STAThread]
		public static void Main(string[] args)
		{
			// Ensures that we're loading the Skia assemblies properly
			// as we're manipulating the output based on _UnoOverrideReferenceCopyLocalPaths
			// and _UnoAdjustUserRuntimeAssembly to avoid getting reference assemblies in the
			// output folder.
			AssemblyLoadContext.Default.Resolving += Default_Resolving;

			Run();
		}

		private static void Run()
		{
			SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

			UnoPlatformHost? host = default;
			var builder = UnoPlatformHostBuilder.Create()
				.App(() => _app = new SamplesApp.App())
				.AfterInit(() =>
				{
					if (host is X11ApplicationHost)
					{
						global::Uno.Foundation.Extensibility.ApiExtensibility.Register<Microsoft.Web.WebView2.Core.CoreWebView2>(typeof(Microsoft.Web.WebView2.Core.INativeWebViewProvider), o => new global::Uno.UI.WebView.Skia.X11.X11NativeWebViewProvider(o));
					}
				})
				.UseX11(hostBuilder =>
				{
					hostBuilder.PreloadMediaPlayer(true);
					// Dev/test affordance: force the X11 render backend via env (e.g. UNO_X11_RENDERER=Vulkan|OpenGL|OpenGLES|Software).
					if (Environment.GetEnvironmentVariable("UNO_X11_RENDERER") is { } rb
						&& Enum.TryParse<global::Uno.UI.Hosting.X11RenderingBackend>(rb, ignoreCase: true, out var backend))
					{
						hostBuilder.RenderingBackend(backend);
					}
				})
				.UseWin32(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
				.UseLinuxFrameBuffer(hostBuilder => hostBuilder.XkbKeymap(new(layout: "us,ara", options: "grp:alt_shift_toggle")))
				.UseMacOS();

			ConfigureDrawingBackend(builder);

			host = builder.Build();

			host.Run();
		}

		// Composition root: the drawing/render backend and the independent content seams (font, image decode) are
		// registered through the host builder — the app never calls the low-level static registrars. A backend is one
		// unit that owns its renderer AND its drawing factory; a GPU backend (WebGPU) takes the geometry engine it
		// needs via its own constructor, not a separate registration. Backend availability is gated by the app build
		// flags (UNO_DRAWING_SKIA / UNO_DRAWING_WEBGPU); env vars pick among them and toggle the managed seams.
		private static void ConfigureDrawingBackend(IUnoPlatformHostBuilder builder)
		{
#if UNO_DRAWING_WEBGPU
			if (Environment.GetEnvironmentVariable("UNO_WEBGPU") is "neutral" or "1" or "true" or "swapchain")
			{
				// WebGPU renderer; geometry is the managed (SkiaSharp-free) engine — WebGPU flattens it.
				builder.GraphicsBackend(new global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider());
				builder.GeometryFactory(new ManagedGeometryFactory());
			}
			else
#endif
			{
#if UNO_DRAWING_SKIA
				builder.GraphicsBackend(new SkiaGraphicsProvider());
				// UNO_MANAGED_GEOMETRY swaps the geometry seam to the managed engine (rasterized on Skia pixels).
				if (Environment.GetEnvironmentVariable("UNO_MANAGED_GEOMETRY") is "1" or "true")
				{
					builder.GeometryFactory(new ManagedGeometryFactory());
				}
#elif UNO_DRAWING_WEBGPU
				// SkiaSharp-free build: WebGPU is the only renderer, over the managed geometry engine.
				builder.GraphicsBackend(new global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider());
				builder.GeometryFactory(new ManagedGeometryFactory());
#endif
			}

#if !UNO_DRAWING_SKIA
			// SkiaSharp-free build: no Skia backend assembly ships to supply defaults, so the render-independent content
			// seams must be registered explicitly here — the host builder throws at Build() otherwise. (Geometry is
			// already set to the managed engine above.) The dev toggles below can still override in a Skia build.
			builder.FontProvider(new ManagedFontProvider());
			builder.ImageEncoderDecoder(new ManagedImageDecoderBackend());
			builder.SvgRenderer(new ManagedSvgRenderer());
#endif

			// Independent content seams (dev toggles). Left unset in a Skia build, they fall back to their Skia impls.
			if (Environment.GetEnvironmentVariable("UNO_MANAGED_FONTS") is "1" or "true")
			{
				builder.FontProvider(new ManagedFontProvider());
			}

			if (Environment.GetEnvironmentVariable("UNO_MANAGED_IMAGE_DECODER") is "1" or "true")
			{
				builder.ImageEncoderDecoder(new ManagedImageDecoderBackend());
			}

			if (Environment.GetEnvironmentVariable("UNO_MANAGED_SVG") is "1" or "true")
			{
				builder.SvgRenderer(new ManagedSvgRenderer());
			}
		}

		private static System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext alc, System.Reflection.AssemblyName assemblyName)
		{
			try
			{
				if (Uri.TryCreate(typeof(MainClass).Assembly.Location, UriKind.Absolute, out var asm))
				{
					var appPath = Path.GetDirectoryName(asm.LocalPath)!;

					var asmPath = Path.Combine(appPath, assemblyName.Name! + ".dll");

					if (File.Exists(asmPath))
					{
						return alc.LoadFromAssemblyPath(asmPath);
					}
				}

				return null;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine($"Error processing {assemblyName.Name}. SamplesApp.Skia.Generic assembly location: {typeof(MainClass).Assembly.Location}");
				throw;
			}
		}
	}
}
