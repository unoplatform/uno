using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace SkiaFreeProof;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		// SkiaSharp-free composition root: managed drawing engines (fonts/geometry/image decode) + the WebGPU GPU
		// backend. No Skia backend is registered or referenced.
		ManagedBackend.Register();
		GraphicsRegistry.Register(new IGraphicsProvider[]
		{
			new global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider(),
		});

		var host = UnoPlatformHostBuilder.Create()
			.App(() => new App())
			.UseX11()
			.UseWin32()
			.Build();

		host.Run();
	}
}
