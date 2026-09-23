#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The default <see cref="IDrawingFactory"/>, backed by SkiaSharp. Installed as <see cref="DrawingFactory.Current"/>
/// only through negotiation — it is the factory the Skia provider (<see cref="SkiaGraphicsProvider"/>) carries, so
/// the registry installs it when Skia wins negotiation.
/// </summary>
internal sealed class SkiaDrawingFactory :
	IDrawingFactory<IGLRenderTarget>,
	IDrawingFactory<ISoftwareRenderTarget>,
	IDrawingFactory<IMetalRenderTarget>,
	IDrawingFactory<IVulkanRenderTarget>,
	System.IDisposable
{
	private const long WebGlResourceCacheBytes = 256 * 1024 * 1024;

	// GL state (built lazily on the first GL present, once the host's GL context is current; per backend
	// instance, i.e. per graphics context). Null for the software / host-canvas cases.
	private GRContext? _glContext;
	private GRBackendRenderTarget? _glRenderTarget;
	private SKSurface? _glSurface;
	private SKSurface? _glLayer;
	private int _glWidth;
	private int _glHeight;

	// Metal state (built lazily on the first Metal present from the host's device/queue). The per-frame texture
	// changes, so the render target + surface are recreated each present; the GRContext is cached.
	private GRContext? _metalContext;

	// Vulkan state (built lazily on the first Vulkan present). The render image is stable across frames, so the
	// render target + surface are cached and rebuilt only when the image handle/size changes.
	private GRContext? _vulkanContext;
	private GRBackendRenderTarget? _vulkanRenderTarget;
	private SKSurface? _vulkanSurface;
	private ulong _vulkanImage;
	private int _vulkanWidth;
	private int _vulkanHeight;

	// Device face of the bound context (one is non-null per kind): the device details come from here, the
	// per-frame surface from the render target handed to BeginPresent.
	private readonly IGLDeviceContext? _glDevice;
	private readonly IMetalDeviceContext? _metalDevice;
	private readonly IVulkanDeviceContext? _vulkanDevice;

	public SkiaDrawingFactory(IGLDeviceContext? glDevice = null, IMetalDeviceContext? metalDevice = null, IVulkanDeviceContext? vulkanDevice = null)
	{
		_glDevice = glDevice;
		_metalDevice = metalDevice;
		_vulkanDevice = vulkanDevice;
	}

	public ICommandRecorder CreateRecording() => SkiaDrawingSession.StartRecording(this);


	// Typed present per kind: the target arrives already narrowed, so there is no cast/switch here.
	public IPresentSession BeginPresent(IGLRenderTarget target) => PresentForGL(target);

	public IPresentSession BeginPresent(ISoftwareRenderTarget target) => SkiaPresentSession.ForSoftware(target, this);

	public IPresentSession BeginPresent(IMetalRenderTarget target) => PresentForMetal(target);

	public IPresentSession BeginPresent(IVulkanRenderTarget target) => PresentForVulkan(target);

	// Build/reuse a GRContext-Vulkan from the host's device context and wrap the per-frame render VkImage as an
	// SKSurface (cached, rebuilt on image/size change). ResetContext each frame because the host's external
	// blit/present mutates Vulkan state Skia can't track.
	private IPresentSession PresentForVulkan(IVulkanRenderTarget vk)
	{
		_vulkanContext ??= GRContext.CreateVulkan(new GRVkBackendContext
		{
			VkInstance = _vulkanDevice!.Instance,
			VkPhysicalDevice = _vulkanDevice!.PhysicalDevice,
			VkDevice = _vulkanDevice!.Device,
			VkQueue = _vulkanDevice!.Queue,
			GraphicsQueueIndex = _vulkanDevice!.GraphicsQueueFamilyIndex,
			MaxAPIVersion = _vulkanDevice!.MaxApiVersion,
			// SkiaSharp 4.x requires the enabled extensions + max API version declared or CreateVulkan returns null.
			Extensions = GRVkExtensions.Create(
				(name, inst, dev) => _vulkanDevice!.GetProcAddress(name, inst, dev),
				_vulkanDevice!.Instance, _vulkanDevice!.PhysicalDevice,
				_vulkanDevice!.InstanceExtensions, _vulkanDevice!.DeviceExtensions),
			GetProcedureAddress = (name, inst, dev) => _vulkanDevice!.GetProcAddress(name, inst, dev),
		}) ?? throw new System.NotSupportedException("Failed to create a Vulkan GRContext.");

		_vulkanContext.ResetContext();

		if (_vulkanSurface is null || _vulkanImage != vk.Image || vk.Width != _vulkanWidth || vk.Height != _vulkanHeight)
		{
			// Drop the cache (fields and keys together) before rebuilding: a failed wrap must leave nothing behind
			// that a later frame could mistake for a live surface, or the window never recovers.
			_vulkanRenderTarget?.Dispose();
			_vulkanSurface?.Dispose();
			_vulkanRenderTarget = null;
			_vulkanSurface = null;
			_vulkanImage = 0;
			_vulkanWidth = 0;
			_vulkanHeight = 0;

			var info = new GRVkImageInfo
			{
				Image = vk.Image,
				Format = vk.Format,
				ImageTiling = vk.ImageTiling,
				ImageLayout = vk.ImageLayout,
				ImageUsageFlags = vk.ImageUsageFlags,
				SampleCount = vk.SampleCount,
				LevelCount = vk.LevelCount,
				CurrentQueueFamily = vk.CurrentQueueFamily,
				Protected = vk.Protected,
				Alloc = new GRVkAlloc { Memory = vk.Memory, Size = vk.MemorySize },
			};
			var renderTarget = new GRBackendRenderTarget(vk.Width, vk.Height, info);
			// The colour type must match the image's own Vulkan format or Skia refuses the wrap and returns null.
			var colorType = ToColorType(vk.ColorFormat);
			var surface = SKSurface.Create(_vulkanContext, renderTarget, GRSurfaceOrigin.TopLeft, colorType, SKColorSpace.CreateSrgb());
			if (surface is null)
			{
				renderTarget.Dispose();
				throw new System.NotSupportedException($"Skia could not wrap the host's Vulkan image ({colorType}, format {vk.Format}).");
			}

			_vulkanRenderTarget = renderTarget;
			_vulkanSurface = surface;
			_vulkanImage = vk.Image;
			_vulkanWidth = vk.Width;
			_vulkanHeight = vk.Height;
		}

		return SkiaPresentSession.ForCachedGpuSurface(_vulkanSurface, _vulkanContext, this);
	}

	// Build/reuse a GRContext-Metal and wrap the host's per-frame MTLTexture as an SKSurface. Present flushes the
	// GRContext so the render lands in the texture before the host commits the drawable. Recreated each frame.
	private IPresentSession PresentForMetal(IMetalRenderTarget metal)
	{
		_metalContext ??= GRContext.CreateMetal(new GRMtlBackendContext { DeviceHandle = _metalDevice!.Device, QueueHandle = _metalDevice!.Queue })
			?? throw new System.NotSupportedException("Failed to create a Metal GRContext.");

		var colorType = ToColorType(metal.ColorFormat);
		// The render target descriptor is consumed by SKSurface.Create; the surface is disposed on present.
		using var target = new GRBackendRenderTarget(metal.Width, metal.Height, new GRMtlTextureInfo(metal.Texture));
		var surface = SKSurface.Create(_metalContext, target, GRSurfaceOrigin.TopLeft, colorType)
			?? throw new System.NotSupportedException($"Skia could not wrap the host's Metal texture ({colorType}).");

		// Render straight into the host's texture; retention (partial repaint) is the host's business (it hands back a
		// stable texture when it preserves contents).
		return SkiaPresentSession.ForGpuTexture(surface, _metalContext, this);
	}

	// Build/reuse a GRContext-GL and an SKSurface over the host's (already-current) window framebuffer.
	internal static SKColorType ToColorType(GraphicsColorFormat format) => format switch
	{
		GraphicsColorFormat.Rgba8888 => SKColorType.Rgba8888,
		GraphicsColorFormat.Rgb888x => SKColorType.Rgb888x,
		_ => SKColorType.Bgra8888,
	};

	private IPresentSession PresentForGL(IGLRenderTarget gl)
	{
		// GLES/WebGL assemble the interface from the host's proc loader. Desktop GL uses SkiaSharp's compiled-in
		// native interface (Create()) instead: the proc-assembled variant segfaults on Mesa/llvmpipe.
		var loader = _glDevice!.GetProcAddress;
		if (_glContext is null)
		{
			_glContext = GRContext.CreateGl(
					(_glDevice!.Kind switch
					{
						GraphicsContextKind.OpenGLES => GRGlInterface.CreateGles(name => loader(name)),
						GraphicsContextKind.WebGL => GRGlInterface.CreateWebGl(name => loader(name)),
						_ => GRGlInterface.Create(),
					})
					?? throw new System.NotSupportedException("OpenGL is not available (GRGlInterface create failed)."))
				?? throw new System.NotSupportedException("Failed to create an OpenGL GRContext.");

			if (_glDevice.Kind == GraphicsContextKind.WebGL)
			{
				// The browser host needs a far larger resource cache than Skia's default: with the default the
				// canvas-sized GPU resources are evicted and rebuilt constantly.
				_glContext.SetResourceCacheLimit(WebGlResourceCacheBytes);
			}
		}

		if (_glSurface is null || gl.Width != _glWidth || gl.Height != _glHeight)
		{
			// Drop the cache (fields and keys together) before rebuilding: a failed wrap must leave nothing behind
			// that a later frame could mistake for a live surface, or the window never recovers.
			_glRenderTarget?.Dispose();
			_glSurface?.Dispose();
			_glLayer?.Dispose();
			_glRenderTarget = null;
			_glSurface = null;
			_glLayer = null;
			_glWidth = 0;
			_glHeight = 0;

			var colorType = ToColorType(gl.ColorFormat);
			var info = new GRGlFramebufferInfo(gl.FramebufferId, colorType.ToGlSizedFormat());
			// A host can report more GL_SAMPLES than Skia can wrap for this color type (common on Android GLES),
			// and an unclamped count makes SKSurface.Create return null instead of quietly downgrading MSAA.
			var samples = Math.Min(gl.SampleCount, _glContext.GetMaxSurfaceSampleCount(colorType));
			var renderTarget = new GRBackendRenderTarget(gl.Width, gl.Height, samples, gl.StencilBits, info);
			// BottomLeft to match OpenGL's origin.
			var surface = SKSurface.Create(_glContext, renderTarget, GRSurfaceOrigin.BottomLeft, colorType);
			if (surface is null)
			{
				renderTarget.Dispose();
				throw new System.NotSupportedException("Skia could not wrap the host's OpenGL framebuffer.");
			}

			// A GL swap discards the framebuffer, so composing into it directly would make the damage region
			// meaningless and repaint every frame whole. Compose into an offscreen that survives the swap instead,
			// and blit it over at present.
			var layerInfo = new SKImageInfo(Math.Max(1, gl.Width), Math.Max(1, gl.Height), colorType, SKAlphaType.Premul);
			var layer = SKSurface.Create(_glContext, budgeted: true, layerInfo);
			layer?.Canvas.Clear(SKColors.Transparent);
			if (layer is null && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Could not create the {gl.Width}x{gl.Height} retained layer; every frame will repaint whole.");
			}

			_glRenderTarget = renderTarget;
			_glSurface = surface;
			_glLayer = layer;
			_glWidth = gl.Width;
			_glHeight = gl.Height;
		}

		// A failed layer is not fatal: composing straight into the framebuffer still renders, it just repaints whole.
		return _glLayer is { } retained
			? SkiaPresentSession.ForRetainedLayer(retained, _glSurface, _glContext, this)
			: new SkiaPresentSession(_glSurface.Canvas, this);
	}

	public void Dispose()
	{
		_glLayer?.Dispose();
		_glSurface?.Dispose();
		_glRenderTarget?.Dispose();
		_glContext?.Dispose();
		_vulkanSurface?.Dispose();
		_vulkanRenderTarget?.Dispose();
		_vulkanContext?.Dispose();
		_metalContext?.Dispose();
	}

	public ITexture RenderOffscreen(int pixelWidth, int pixelHeight, System.Action<IDrawingSession> render)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.Transparent);
		render(new SkiaDrawingSession(surface.Canvas, this));
		// Snapshot detaches from the surface (copy-on-write), so the returned texture outlives it. On Skia an
		// SKImage is already the sampleable form, so there is no readback here.
		return new SkiaTexture(surface.Snapshot());
	}

	// Skia rasterizes on the CPU, so the readback is synchronous — return an already-completed task.
	public System.Threading.Tasks.Task<IImage> SnapshotAsync(ITexture texture)
	{
		if (texture is not SkiaTexture skia)
		{
			throw new System.ArgumentException("Texture was not produced by SkiaDrawingFactory.", nameof(texture));
		}

		// The returned image must own its pixels: the texture stays independently alive and disposable, and both
		// wrappers free their SKImage.
		using var bitmap = new SKBitmap(new SKImageInfo(skia.PixelWidth, skia.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
		if (!skia.Image.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
		{
			throw new System.InvalidOperationException("Failed to read the texture's pixels back.");
		}

		return System.Threading.Tasks.Task.FromResult<IImage>(new SkiaImage(SKImage.FromBitmap(bitmap)));
	}

	public ITexture CreateTexture(IImage image)
	{
		var info = new SKImageInfo(image.PixelWidth, image.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		var pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
		image.CopyPixels(pixels);
		return new SkiaTexture(SKImage.FromPixelCopy(info, pixels));
	}

	public ITexture CreateTexture(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		return new SkiaTexture(SKImage.FromPixelCopy(info, bgraPremul));
	}

	public IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
	{
		var shader = SKShader.CreateLinearGradient(
			new SKPoint(start.X, start.Y),
			new SKPoint(end.X, end.Y),
			ToSKColors(colors),
			colorPositions,
			ToSK(tileMode),
			localMatrix.ToSKMatrix());

		return new SkiaShader(shader);
	}

	public IShader CreateRadialGradientShader(
		Vector2 center,
		Vector2 gradientOrigin,
		float radiusX,
		float radiusY,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
	{
		// SkiaSharp radial gradients take a single radius, so squash the larger axis onto the smaller.
		ComputeRadiusAndScale(center, radiusX, radiusY, out var radius, out var squash);

		if (radius <= 0)
		{
			// Radius 0: match the last gradient color everywhere.
			return new SkiaShader(SKShader.CreateColor(LastColor(colors).ToSKColor()));
		}

		// The scale-down matrix is applied before the brush transform (SKMatrix.PreConcat), which in the
		// row-vector convention is `squash * localMatrix`.
		var totalMatrix = squash * localMatrix;
		var skTotal = totalMatrix.ToSKMatrix();
		var skTile = ToSK(tileMode);

		if (center == gradientOrigin)
		{
			return new SkiaShader(SKShader.CreateRadialGradient(
				new SKPoint(center.X, center.Y), radius, ToSKColors(colors), colorPositions, skTile, skTotal));
		}

		// Offset origin: SkiaSharp has no focal radial gradient, so approximate with a two-point conical gradient
		// (reversed stops) composed over the last color, which fills the region the conical leaves uncovered.
		var reversedColors = new SKColor[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			reversedColors[i] = colors[colors.Length - 1 - i].ToSKColor();
		}

		var reversedPositions = new float[colorPositions.Length];
		for (var i = 0; i < colorPositions.Length; i++)
		{
			var p = colorPositions[i];
			reversedPositions[i] = (p > 0 && p < 1) ? System.Math.Abs(1 - p) : p;
		}

		Matrix3x2.Invert(totalMatrix, out var inverse);
		var origin = Vector2.Transform(gradientOrigin, inverse);

		var conical = SKShader.CreateTwoPointConicalGradient(
			new SKPoint(center.X, center.Y), radius, new SKPoint(origin.X, origin.Y), 0,
			reversedColors, reversedPositions, skTile, skTotal);
		var fallback = SKShader.CreateColor(LastColor(colors).ToSKColor());
		return new SkiaShader(SKShader.CreateCompose(fallback, conical));
	}

	private static Color LastColor(Color[] colors) => colors.Length > 0 ? colors[^1] : default;   // default(Color) == transparent

	// SkiaSharp allows only a single radius, so build a scale-down transform squashing the larger axis onto the smaller.
	private static void ComputeRadiusAndScale(Vector2 center, float radiusX, float radiusY, out float radius, out Matrix3x2 matrix)
	{
		matrix = Matrix3x2.Identity;
		if (radiusX == 0 || radiusY == 0)
		{
			// Handle this specific case as zero division would cause us troubles.
			radius = 0;
			return;
		}

		if (radiusX >= radiusY)
		{
			// radiusX is larger, use it and scale down radiusY.
			radius = radiusX;
			var scaleDownRatio = radiusY / radiusX;
			matrix = new Matrix3x2(1, 0, 0, scaleDownRatio, 0, center.Y - scaleDownRatio * center.Y);
		}
		else
		{
			// radiusY is larger, use it and scale down radiusX.
			radius = radiusY;
			var scaleDownRatio = radiusX / radiusY;
			matrix = new Matrix3x2(scaleDownRatio, 0, 0, 1, center.X - scaleDownRatio * center.X, 0);
		}
	}

	private static SKColor[] ToSKColors(Color[] colors)
	{
		var skColors = new SKColor[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			skColors[i] = colors[i].ToSKColor();
		}
		return skColors;
	}

	public IColorFilter CreateTintColorFilter(Color color)
		=> new SkiaColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SKBlendMode.SrcIn));

	public IColorFilter CreateColorMatrixColorFilter(float[] matrix)
		=> new SkiaColorFilter(SKColorFilter.CreateColorMatrix(matrix));

	public IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds)
	{
		var fuser = new SkiaEffectFuser();
		var filter = fuser.FuseGraph(tree, bounds.ToSKRect());
		if (filter is null)
		{
			// Nothing downstream surfaces this: the brush then paints nothing at all, so name what could not be realized.
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"The Skia backend could not realize the effect graph rooted at '{tree.GetType().Name}': {fuser.FailureReason}.");
			}

			return null;
		}

		return new SkiaEffectFilter(filter);
	}

	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color)
	{
		// Skia filters are natively refcounted and each parent takes its own reference, so the interior wrappers are
		// released as soon as the root is built.
		using var tint = SKColorFilter.CreateBlendMode(color.ToSKColor(), SKBlendMode.Modulate);
		using var tintFilter = SKImageFilter.CreateColorFilter(tint);
		using var blur = SKImageFilter.CreateBlur(sigmaX, sigmaY);
		using var composed = SKImageFilter.CreateCompose(blur, tintFilter);
		return new SkiaEffectFilter(SKImageFilter.CreateOffset(dx, dy, composed));
	}

	private static SKShaderTileMode ToSK(GradientTileMode mode) => mode switch
	{
		GradientTileMode.Repeat => SKShaderTileMode.Repeat,
		GradientTileMode.Mirror => SKShaderTileMode.Mirror,
		_ => SKShaderTileMode.Clamp,
	};
}
