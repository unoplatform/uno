#nullable enable

using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
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

			ApplyManagedBackendOptions();

			// Install the drawing backend at the app entry (the framework is backend-agnostic and packaged once).
			// Gated by the app-level build flags: UNO_DRAWING_SKIA installs the Skia backend (its default renderer +
			// SKCanvasElement factory); otherwise the SkiaSharp-free managed backend is installed and the head's
			// WebGPU render view provides the renderer. Must run before Application.Start reaches DrawingFactory.Current.
#if UNO_DRAWING_SKIA
			global::Uno.UI.Composition.Skia.SkiaBackend.Register();
#else
			global::Uno.UI.Composition.Drawing.ManagedBackend.Register();
#endif

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

			host = builder
				.Build();

			host.Run();
		}

		// Dev/test affordance: let the host opt into the SkiaSharp-free managed engines via environment
		// variables. Backend selection lives in DrawingBackendOptions (init options); the framework itself no
		// longer reads these variables — only this host does, as a convenience for exercising managed mode.
		private static void ApplyManagedBackendOptions()
		{
			if (Environment.GetEnvironmentVariable("UNO_MANAGED_FONTS") is "1" or "true")
			{
				Uno.UI.Composition.Drawing.DrawingBackendOptions.FontProvider = new Uno.UI.Composition.Drawing.ManagedFontProvider();
			}

			if (Environment.GetEnvironmentVariable("UNO_MANAGED_GEOMETRY") is "1" or "true")
			{
				Uno.UI.Composition.Drawing.DrawingBackendOptions.UseManagedGeometry = true;
			}

			if (Environment.GetEnvironmentVariable("UNO_MANAGED_IMAGE_DECODER") is "1" or "true")
			{
				Uno.UI.Composition.Drawing.DrawingBackendOptions.UseManagedImageDecoder = true;
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
