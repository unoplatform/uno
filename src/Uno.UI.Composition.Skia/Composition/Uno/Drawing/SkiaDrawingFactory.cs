#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The default <see cref="IDrawingFactory"/>, backed by SkiaSharp. Installed as <see cref="DrawingFactory.Current"/>
/// only through negotiation — it is the factory the Skia provider (<see cref="SkiaGraphicsProvider"/>) carries, so
/// when Skia wins negotiation the registry installs this. No module-initializer self-registration: the graphics
/// backend is registered up front and the factory is set once, never as a load-time fallback later overwritten.
/// </summary>
internal sealed class SkiaDrawingFactory :
	IDrawingFactory<IGLRenderTarget>,
	IDrawingFactory<ISoftwareRenderTarget>,
	IDrawingFactory<IMetalRenderTarget>,
	IDrawingFactory<IVulkanRenderTarget>,
	System.IDisposable
{
	// GL state (built lazily on the first GL present, once the host's GL context is current; per backend
	// instance, i.e. per graphics context). Null for the software / host-canvas cases.
	private GRContext? _glContext;
	private GRBackendRenderTarget? _glRenderTarget;
	private SKSurface? _glSurface;
	// Persistent GPU offscreen the frame composes into; retained across frames (blitted whole onto the swapchain
	// framebuffer each present) so the compositor can repaint only the damaged region. Rebuilt on resize.
	private SKSurface? _glOffscreen;
	private int _glWidth;
	private int _glHeight;

	// Software state: a persistent CPU offscreen the frame composes into (retained across frames for partial repaint),
	// read back in full into the host's framebuffer each present. Rebuilt on size/format change. Backend-owned, so
	// software retention matches the GPU paths — the host framebuffer needn't itself persist.
	private SKSurface? _softwareOffscreen;
	private int _softwareWidth;
	private int _softwareHeight;
	private SKColorType _softwareColorType;

	// Metal state (built lazily on the first Metal present from the host's device/queue). The per-frame drawable
	// changes, so the drawable surface is recreated each present; the GRContext is cached. The offscreen is a
	// persistent GPU surface the frame composes into (retained across frames for partial repaint), blitted onto the
	// per-frame drawable each present; rebuilt on resize.
	private GRContext? _metalContext;
	private SKSurface? _metalOffscreen;
	private int _metalWidth;
	private int _metalHeight;

	// Vulkan state (built lazily on the first Vulkan present from the host's device context). The render image is
	// stable across frames (recreated on resize), so the render target + surface are cached and rebuilt only when
	// the image handle/size changes; the GRContext is cached.
	private GRContext? _vulkanContext;
	private GRBackendRenderTarget? _vulkanRenderTarget;
	private SKSurface? _vulkanSurface;
	private ulong _vulkanImage;
	private int _vulkanWidth;
	private int _vulkanHeight;

	// Device face of the bound context (set by the provider from the typed context; one is non-null per kind).
	// The device details (GL loader/flavor, Metal device/queue, Vulkan device handles) come from here; the
	// per-frame surface comes from the render target handed to BeginPresent.
	private readonly IGLDeviceContext? _glDevice;
	private readonly IMetalDeviceContext? _metalDevice;
	private readonly IVulkanDeviceContext? _vulkanDevice;

	public SkiaDrawingFactory(IGLDeviceContext? glDevice = null, IMetalDeviceContext? metalDevice = null, IVulkanDeviceContext? vulkanDevice = null)
	{
		_glDevice = glDevice;
		_metalDevice = metalDevice;
		_vulkanDevice = vulkanDevice;
	}

	public ICommandRecorder CreateRecording() => SkiaDrawingSession.StartRecording();


	// Typed present per kind the Skia backend serves — the target arrives already narrowed, so there is no
	// cast/switch here. CPU framebuffer → SKSurface over pixels; GL framebuffer → GRContext-GL; Metal texture →
	// GRContext-Metal.
	public IPresentSession BeginPresent(IGLRenderTarget target) => PresentForGL(target);

	public IPresentSession BeginPresent(ISoftwareRenderTarget target) => PresentForSoftware(target);

	public IPresentSession BeginPresent(IMetalRenderTarget target) => PresentForMetal(target);

	public IPresentSession BeginPresent(IVulkanRenderTarget target) => PresentForVulkan(target);

	// Compose into a persistent CPU offscreen (retained across frames), read back in full into the host framebuffer
	// on present. Backend-owned retention, so partial repaint works uniformly with the GPU paths regardless of
	// whether the host reuses its own buffer.
	private IPresentSession PresentForSoftware(ISoftwareRenderTarget target)
	{
		var colorType = target.ColorFormat == GraphicsColorFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		if (_softwareOffscreen is null || target.Width != _softwareWidth || target.Height != _softwareHeight || colorType != _softwareColorType)
		{
			_softwareWidth = target.Width;
			_softwareHeight = target.Height;
			_softwareColorType = colorType;
			_softwareOffscreen?.Dispose();
			_softwareOffscreen = SKSurface.Create(new SKImageInfo(target.Width, target.Height, colorType, SKAlphaType.Premul));
		}

		return SkiaPresentSession.ForRetainedSoftware(_softwareOffscreen!, target);
	}

	// The host owns the Vulkan device/swapchain (IVulkanDeviceContext) and hands the per-frame render VkImage
	// (IVulkanRenderTarget). Build/reuse a GRContext-Vulkan from the device context and wrap the image as an
	// SKSurface (cached, rebuilt on image/size change); the host's Present() blits the rendered image to the
	// swapchain. ResetContext each frame because that external blit/present mutates Vulkan state Skia can't track.
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
			_vulkanWidth = vk.Width;
			_vulkanHeight = vk.Height;
			_vulkanImage = vk.Image;
			_vulkanRenderTarget?.Dispose();
			_vulkanSurface?.Dispose();

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
			_vulkanRenderTarget = new GRBackendRenderTarget(vk.Width, vk.Height, info);
			_vulkanSurface = SKSurface.Create(_vulkanContext, _vulkanRenderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888, SKColorSpace.CreateSrgb());
		}

		return SkiaPresentSession.ForCachedGpuSurface(_vulkanSurface!, _vulkanContext);
	}

	// The host hands the per-frame MTLTexture (+ its device/queue); build/reuse a GRContext-Metal and wrap the
	// texture as an SKSurface to compose into. Present flushes the GRContext so the render lands in the texture
	// before the host commits/presents the drawable. Recreated each frame (the texture differs per call).
	private IPresentSession PresentForMetal(IMetalRenderTarget metal)
	{
		_metalContext ??= GRContext.CreateMetal(new GRMtlBackendContext { DeviceHandle = _metalDevice!.Device, QueueHandle = _metalDevice!.Queue })
			?? throw new System.NotSupportedException("Failed to create a Metal GRContext.");

		var colorType = metal.ColorFormat == GraphicsColorFormat.Bgra8888 ? SKColorType.Bgra8888 : SKColorType.Rgba8888;
		var target = new GRBackendRenderTarget(metal.Width, metal.Height, new GRMtlTextureInfo(metal.Texture));
		var drawableSurface = SKSurface.Create(_metalContext, target, GRSurfaceOrigin.TopLeft, colorType);
		// The render target descriptor is consumed by SKSurface.Create; the drawable surface is disposed on present.
		target.Dispose();

		if (_metalOffscreen is null || metal.Width != _metalWidth || metal.Height != _metalHeight)
		{
			_metalWidth = metal.Width;
			_metalHeight = metal.Height;
			_metalOffscreen?.Dispose();
			_metalOffscreen = SKSurface.Create(_metalContext, budgeted: true, new SKImageInfo(metal.Width, metal.Height, colorType, SKAlphaType.Premul));
		}

		// Compose into the retained offscreen, then blit it onto the per-frame drawable on present.
		return SkiaPresentSession.ForRetainedGpuOffscreen(_metalOffscreen!, drawableSurface, _metalContext, ownsFramebuffer: true);
	}

	// The host has made its GL context current; build/reuse a GRContext-GL and an SKSurface over the window
	// framebuffer, then compose into its (borrowed, cached-across-frames) canvas. The context swaps on present.
	private IPresentSession PresentForGL(IGLRenderTarget gl)
	{
		// GLES/WebGL: assemble the interface from the host's neutral proc loader (the seam mandates one, so this
		// path is backend-agnostic). Desktop GL: SkiaSharp's proc-assembled interface (CreateOpenGl/Create(getProc))
		// segfaults on Mesa/llvmpipe — validated with three loader variants — whereas its compiled-in native
		// interface (Create()) renders correctly, so desktop GL uses that. The loader still rides the seam for any
		// non-Skia backend; this is purely SkiaSharp's own desktop-GL assembly being unstable.
		var loader = _glDevice!.GetProcAddress;
		_glContext ??= GRContext.CreateGl(
				(_glDevice!.Flavor switch
				{
					GLFlavor.OpenGLES => GRGlInterface.CreateGles(name => loader(name)),
					GLFlavor.WebGL => GRGlInterface.CreateWebGl(name => loader(name)),
					_ => GRGlInterface.Create(),
				})
				?? throw new System.NotSupportedException("OpenGL is not available (GRGlInterface create failed)."))
			?? throw new System.NotSupportedException("Failed to create an OpenGL GRContext.");

		if (_glSurface is null || gl.Width != _glWidth || gl.Height != _glHeight)
		{
			_glWidth = gl.Width;
			_glHeight = gl.Height;
			_glRenderTarget?.Dispose();
			_glSurface?.Dispose();
			_glOffscreen?.Dispose();

			var info = new GRGlFramebufferInfo(gl.FramebufferId, SKColorType.Rgba8888.ToGlSizedFormat());
			_glRenderTarget = new GRBackendRenderTarget(gl.Width, gl.Height, gl.SampleCount, gl.StencilBits, info);
			// BottomLeft to match OpenGL's origin.
			_glSurface = SKSurface.Create(_glContext, _glRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
			// Retained across frames; Skia handles each surface's GL origin so the whole-offscreen blit isn't flipped.
			_glOffscreen = SKSurface.Create(_glContext, budgeted: true, new SKImageInfo(gl.Width, gl.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
		}

		return SkiaPresentSession.ForRetainedGpuOffscreen(_glOffscreen!, _glSurface!, _glContext!);
	}

	public void Dispose()
	{
		_softwareOffscreen?.Dispose();
		_glOffscreen?.Dispose();
		_glSurface?.Dispose();
		_glRenderTarget?.Dispose();
		_glContext?.Dispose();
		_metalOffscreen?.Dispose();
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
		render(new SkiaDrawingSession(surface.Canvas));
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

		return System.Threading.Tasks.Task.FromResult<IImage>(new SkiaImage(skia.Image));
	}

	public ITexture CreateTexture(IImage image)
	{
		var info = new SKImageInfo(image.PixelWidth, image.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		var pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
		image.CopyPixels(pixels);
		return new SkiaTexture(SKImage.FromPixelCopy(info, pixels));
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
		// SkiaSharp radial gradients take a single radius, so squash the larger axis onto the smaller with a
		// scale-down matrix and use one radius.
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

		// Offset origin: SkiaSharp has no focal radial gradient, so approximate with a two-point conical
		// gradient (reversed stops) composed over the last color, which fills the region the conical leaves
		// uncovered.
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

	// SkiaSharp doesn't allow explicit RadiusX/RadiusY on a radial gradient, so we build a scale-down
	// transform that squashes the larger axis onto the smaller and use a single radius.
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

	public IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode)
		=> new SkiaColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SkiaDrawingSession.ToSKBlendMode(mode)));

	public IColorFilter CreateColorMatrixColorFilter(float[] matrix)
		=> new SkiaColorFilter(SKColorFilter.CreateColorMatrix(matrix));

	public IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds)
	{
		var filter = new SkiaEffectFuser().Fuse(tree, bounds.ToSKRect());
		return filter is null ? null : new SkiaEffectFilter(filter);
	}

	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color)
		=> new SkiaEffectFilter(SKImageFilter.CreateOffset(dx, dy, SKImageFilter.CreateCompose(
			SKImageFilter.CreateBlur(sigmaX, sigmaY),
			SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SKBlendMode.Modulate)))));

	private static SKShaderTileMode ToSK(GradientTileMode mode) => mode switch
	{
		GradientTileMode.Repeat => SKShaderTileMode.Repeat,
		GradientTileMode.Mirror => SKShaderTileMode.Mirror,
		_ => SKShaderTileMode.Clamp,
	};
}
