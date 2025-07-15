#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Icu;
using Microsoft.UI.Xaml.Documents;
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
			// Icu.Wrapper.Verbose = true;
			// Icu.Wrapper.Init();

			// Console.WriteLine($"Text: {text}");
			// var lineBoundaries = Icu.BreakIterator.GetBoundaries(BreakIterator.UBreakIteratorType.LINE, new Locale("en", "US"), text);
			// Console.WriteLine("Line boundaries:");
			// foreach (var boundary in lineBoundaries)
			// {
			// 	Console.WriteLine($"[{text[boundary.Start..boundary.End]}]");
			// }
			// var wordBoundaries = Icu.BreakIterator.GetWordBoundaries(new Locale("en", "US"), text, true);
			// Console.WriteLine("Word boundaries:");
			// foreach (var boundary in wordBoundaries)
			// {
			// 	Console.WriteLine($"[{text[boundary.Start..boundary.End]}]");
			// }
			//
			// var bidi = new Icu.BiDi();
			// bidi.SetPara(text, BiDi.DEFAULT_RTL, null);
			// var runCount = bidi.CountRuns();
			// Console.WriteLine("BiDi runs logical to visual:");
			// for (var i = 0; i < runCount; i++)
			// {
			// 	var level = bidi.GetVisualRun(i, out var logicalStart, out var length);
			// 	Console.WriteLine(
			// 		$"Visual Run {i} level: {level}: [{text[logicalStart..(logicalStart + length)]}]");
			// }

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
						global::Uno.Foundation.Extensibility.ApiExtensibility.Register<Microsoft.Web.WebView2.Core.CoreWebView2>(typeof(Microsoft.Web.WebView2.Core.INativeWebViewProvider), o => new global::Uno.UI.WebView.Skia.X11.X11NativeWebViewProvider(o));
					}
				})
				.UseX11(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
				.UseWin32(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
				.UseWindows()
				.UseLinuxFrameBuffer()
				.UseWindows(b => b
					.WpfApplication(() =>
					{
						// optional app creation
						return new System.Windows.Application();
					}))
				.UseMacOS();

			host = builder
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
