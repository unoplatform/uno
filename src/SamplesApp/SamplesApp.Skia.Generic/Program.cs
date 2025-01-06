#nullable enable

using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

			SkiaHost? host = default;
			host = SkiaHostBuilder.Create()
				.App(() => _app = new SamplesApp.App())
				.AfterInit(() =>
				{
					if (_app is not null && OperatingSystem.IsWindows())
					{
						_app.MainWindowActivated += delegate
						{
							var windowContent = System.Windows.Application.Current.Windows[0].Content;
							Assert.IsInstanceOfType(windowContent, typeof(System.Windows.UIElement));
							var windowContentAsUIElement = (System.Windows.UIElement)windowContent;
							Assert.IsTrue(windowContentAsUIElement.IsFocused);
						};
					}

					if (host is X11ApplicationHost)
					{
						global::Uno.Foundation.Extensibility.ApiExtensibility.Register<global::Windows.Media.Playback.MediaPlayer>(typeof(global::Uno.Media.Playback.IMediaPlayerExtension), o => new global::Uno.UI.MediaPlayer.Skia.SharedMediaPlayerExtension(o));
						global::Uno.Foundation.Extensibility.ApiExtensibility.Register<global::Microsoft.UI.Xaml.Controls.MediaPlayerPresenter>(typeof(global::Microsoft.UI.Xaml.Controls.IMediaPlayerPresenterExtension), o => new global::Uno.UI.MediaPlayer.Skia.X11.X11MediaPlayerPresenterExtension(o));
					}
					else if (host is Win32Host)
					{
						global::Uno.Foundation.Extensibility.ApiExtensibility.Register<global::Windows.Media.Playback.MediaPlayer>(typeof(global::Uno.Media.Playback.IMediaPlayerExtension), o => new global::Uno.UI.MediaPlayer.Skia.SharedMediaPlayerExtension(o));
						global::Uno.Foundation.Extensibility.ApiExtensibility.Register<global::Microsoft.UI.Xaml.Controls.MediaPlayerPresenter>(typeof(global::Microsoft.UI.Xaml.Controls.IMediaPlayerPresenterExtension), o => new global::Uno.UI.MediaPlayer.Skia.Win32.Win32MediaPlayerPresenterExtension(o));
					}
				})
				.UseX11()
				.UseWin32()
				.UseWindows()
				.UseLinuxFrameBuffer()
				.UseWindows(b => b
					.WpfApplication(() =>
					{
						// optional app creation
						return new System.Windows.Application();
					}))
				.UseMacOS()
				.Build();

			host.Run();
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
