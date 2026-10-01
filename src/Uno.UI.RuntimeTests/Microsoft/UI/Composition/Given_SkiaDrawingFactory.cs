#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#if __SKIA__
using SkiaSharp;
using Uno.UI.Composition.Drawing;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_SkiaDrawingFactory
{
#if __SKIA__
	// Ganesh's default stencil-and-cover path renderer anti-aliases arbitrary paths (glyph outlines, icon strokes)
	// with only ~4 coverage levels on single-sampled surfaces. The GPU contexts must be created so that such paths
	// get the same coverage AA as the CPU rasterizer.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Gpu_Context_Draws_NonConvex_Path_Then_Antialiasing_Matches_Cpu()
	{
		const string Star = "M 10,0 L 13,7 L 20,7 L 14,12 L 16,20 L 10,15 L 4,20 L 6,12 L 0,7 L 7,7 Z";
		var info = new SKImageInfo(160, 160, SKColorType.Rgba8888, SKAlphaType.Premul);

		using var cpu = SKSurface.Create(info);
		var cpuLevels = DrawAndCountCoverageLevels(cpu, Star);

		int? gpuLevels = null;
		var thread = new Thread(() =>
		{
			using var gl = HiddenWglContext.TryCreate();
			if (gl is null)
			{
				return;
			}

			using var glInterface = GRGlInterface.Create();
			using var context = glInterface is null ? null : GRContext.CreateGl(glInterface, SkiaDrawingFactory.CreateContextOptions());
			if (context is null)
			{
				return;
			}

			using var gpu = SKSurface.Create(context, false, info, 0, GRSurfaceOrigin.TopLeft);
			gpuLevels = DrawAndCountCoverageLevels(gpu, Star);
			context.Flush(true, true);
		});
		thread.Start();
		await Task.Run(thread.Join);

		if (gpuLevels is null)
		{
			Assert.Inconclusive("Hardware OpenGL is not available.");
		}

		Assert.IsGreaterThan(16, cpuLevels, $"CPU baseline should anti-alias the path ({cpuLevels} levels).");
		Assert.IsGreaterThanOrEqualTo(cpuLevels / 2, gpuLevels.Value, $"GPU anti-aliasing produced {gpuLevels} coverage levels, CPU produced {cpuLevels}.");
	}

	// Number of distinct partial-coverage values over a white non-convex path drawn at fractional offsets on black.
	private static int DrawAndCountCoverageLevels(SKSurface surface, string svgPath)
	{
		using var path = SKPath.ParseSvgPathData(svgPath);
		using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };

		var canvas = surface.Canvas;
		canvas.Clear(SKColors.Black);
		canvas.Translate(10.37f, 10.29f);
		canvas.Scale(6);
		canvas.DrawPath(path, paint);
		canvas.Flush();

		using var image = surface.Snapshot();
		using var bitmap = SKBitmap.FromImage(image);

		var levels = new HashSet<byte>();
		for (var y = 0; y < bitmap.Height; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				var value = bitmap.GetPixel(x, y).Red;
				if (value is > 0 and < 255)
				{
					levels.Add(value);
				}
			}
		}

		return levels.Count;
	}

	private sealed class HiddenWglContext : IDisposable
	{
		private readonly IntPtr _hwnd;
		private readonly IntPtr _hdc;
		private readonly IntPtr _context;

		private HiddenWglContext(IntPtr hwnd, IntPtr hdc, IntPtr context)
		{
			_hwnd = hwnd;
			_hdc = hdc;
			_context = context;
		}

		public static HiddenWglContext? TryCreate()
		{
			var hwnd = CreateWindowExW(0, "STATIC", "uno-gl-test", 0, 0, 0, 16, 16, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			var hdc = GetDC(hwnd);
			var pfd = new PixelFormatDescriptor
			{
				Size = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
				Version = 1,
				Flags = 0x4 | 0x20 | 0x1, // PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER
				ColorBits = 32,
				DepthBits = 24,
				StencilBits = 8,
			};

			var format = ChoosePixelFormat(hdc, ref pfd);
			var context = format != 0 && SetPixelFormat(hdc, format, ref pfd) ? wglCreateContext(hdc) : IntPtr.Zero;
			if (context == IntPtr.Zero || !wglMakeCurrent(hdc, context))
			{
				DestroyWindow(hwnd);
				return null;
			}

			return new HiddenWglContext(hwnd, hdc, context);
		}

		public void Dispose()
		{
			wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
			wglDeleteContext(_context);
			ReleaseDC(_hwnd, _hdc);
			DestroyWindow(_hwnd);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct PixelFormatDescriptor
		{
			public ushort Size, Version;
			public uint Flags;
			public byte PixelType, ColorBits, RedBits, RedShift, GreenBits, GreenShift, BlueBits, BlueShift, AlphaBits, AlphaShift;
			public byte AccumBits, AccumRedBits, AccumGreenBits, AccumBlueBits, AccumAlphaBits, DepthBits, StencilBits, AuxBuffers, LayerType, Reserved;
			public uint LayerMask, VisibleMask, DamageMask;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr CreateWindowExW(int exStyle, string className, string windowName, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

		[DllImport("user32.dll")]
		private static extern bool DestroyWindow(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern int ChoosePixelFormat(IntPtr hdc, ref PixelFormatDescriptor pfd);

		[DllImport("gdi32.dll")]
		private static extern bool SetPixelFormat(IntPtr hdc, int format, ref PixelFormatDescriptor pfd);

		[DllImport("opengl32.dll")]
		private static extern IntPtr wglCreateContext(IntPtr hdc);

		[DllImport("opengl32.dll")]
		private static extern bool wglDeleteContext(IntPtr context);

		[DllImport("opengl32.dll")]
		private static extern bool wglMakeCurrent(IntPtr hdc, IntPtr context);
	}
#endif
}
