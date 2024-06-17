#nullable enable

using System;
using System.IO;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia;

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

			var host = SkiaHostBuilder.Create()
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
				})
				.UseX11()
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
				if (Uri.TryCreate(typeof(MainClass).Assembly.Location, UriKind.RelativeOrAbsolute, out var asm))
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
