using Tizen.Applications;
using ElmSharp;
using SkiaSharp.Views.Tizen;
using SkiaSharp;
using WUX = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia;
using System.Diagnostics;

namespace SamplesApp.Skia.Tizen
{
	class MainClass
	{
		static void Main(string[] args)
		{
			var host = new TizenHost(() => new SamplesApp.App(), args);
			host.Run();
		}
	}
}
