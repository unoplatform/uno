using System;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;
using System.Threading.Tasks;

namespace SkiaSharpExample
{
	class MainClass
	{
		[STAThread]
		public static async Task Main(string[] args)
		{
			var host = new PlatformHost(() => new SamplesApp.App());
			await host.Run();
		}
	}
}
