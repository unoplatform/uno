#nullable enable

using System;
using System.IO;
using System.Runtime.Loader;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Runtime.Skia.Win32;
using Uno.WinUI.Runtime.Skia.X11;

namespace SamplesApp.Head;

internal static class Program
{
	private static SamplesApp.App? _app;

	[STAThread]
	public static void Main(string[] args)
	{
		// args are not forwarded to the host: SamplesApp reads the command line through
		// Environment.GetCommandLineArgs() (see App.xaml.cs), which the runtime-test harness relies on.

		// Ensures that we're loading the Skia assemblies properly as the output is
		// adjusted to avoid getting reference assemblies in the output folder.
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
			.UseX11(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
			.UseWin32(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
			.UseLinuxFrameBuffer(hostBuilder => hostBuilder.XkbKeymap(new(layout: "us,ara", options: "grp:alt_shift_toggle")))
			.UseMacOS();

		host = builder
			.Build();

		host.Run();
	}

	private static System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext alc, System.Reflection.AssemblyName assemblyName)
	{
		try
		{
			if (Uri.TryCreate(typeof(Program).Assembly.Location, UriKind.Absolute, out var asm))
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
			Console.WriteLine($"Error processing {assemblyName.Name}. SamplesApp head assembly location: {typeof(Program).Assembly.Location}");
			throw;
		}
	}
}
