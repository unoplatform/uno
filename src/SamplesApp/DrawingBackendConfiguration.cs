#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace SamplesApp;

/// <summary>
/// Composition root for the drawing backend + content seams, shared by every SamplesApp head (linked into each,
/// so its per-head build flags and gated backend references apply). Available backends are gated by build flags
/// (UNO_DRAWING_SKIA / UNO_DRAWING_WEBGPU, from UnoDrawingBackend{Skia,WebGpu}); env vars pick among them and
/// toggle the individual content seams. A SkiaSharp-free build (UnoDrawingBackendSkia=false) registers the WebGPU
/// renderer over the managed geometry engine and the managed font/image/SVG seams explicitly.
/// </summary>
internal static class DrawingBackendConfiguration
{
	public static void Configure(IUnoPlatformHostBuilder builder)
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
		// SkiaSharp-free build: no Skia assembly supplies defaults, so the content seams must be registered
		// explicitly here or the host builder throws at Build(). (Geometry is already set to the managed engine above.)
		builder.FontProvider(new ManagedFontProvider(TryGetBundledDefaultFont()));
		builder.ImageEncoderDecoder(new ManagedImageDecoderBackend());
		builder.SvgRenderer(new ManagedSvgRenderer());
#endif

		// Independent content seams (dev toggles). Left unset in a Skia build, they fall back to their Skia impls.
		if (Environment.GetEnvironmentVariable("UNO_MANAGED_FONTS") is "1" or "true")
		{
			builder.FontProvider(new ManagedFontProvider(TryGetBundledDefaultFont()));
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

	// The managed font provider enumerates system fonts, which platforms like WASM don't have — feed it the
	// head-embedded default face there (see the head csproj's EmbeddedResource).
	private static byte[]? TryGetBundledDefaultFont()
	{
		if (!OperatingSystem.IsBrowser())
		{
			return null;
		}

		if (typeof(DrawingBackendConfiguration).Assembly.GetManifestResourceStream("SamplesApp.DefaultFont.ttf") is not { } stream)
		{
			return null;
		}

		using (stream)
		{
			using var ms = new System.IO.MemoryStream();
			stream.CopyTo(ms);
			return ms.ToArray();
		}
	}
}
