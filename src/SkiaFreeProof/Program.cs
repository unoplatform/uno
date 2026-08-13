using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace SkiaFreeProof;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		// SkiaSharp-free composition root, wired entirely through the host builder. No Skia backend is registered or
		// referenced: the WebGPU render backend draws over the managed (SkiaSharp-free) geometry factory, and the
		// managed font + image engines fill the independent content seams. The WebGPU context is created internally
		// (the WebGPU assembly self-registers its context factory) — the app never wires one.
		UnoPlatformHostBuilder.Create()
			.App(() => new App())
			.UseX11()
			.UseWin32()
			.GraphicsBackend(new global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider(new ManagedDrawingFactory()))
			.FontProvider(new ManagedFontProvider())
			.ImageDecoder(new ManagedImageDecoderBackend())
			.Build()
			.Run();
	}
}
