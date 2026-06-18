using System;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private interface IRenderer : IDisposable
	{
		void StartPaint();
		void EndPaint();

		SKSurface UpdateSize(int width, int height);
		void CopyPixels(int width, int height);
		bool IsSoftware();
		void Reinitialize();

		/// <summary>
		/// True if the surface that composition renders onto retains the previous frame's pixels between
		/// presents, so the present can be clipped to the damage region. The software (persistent DIB) and
		/// Vulkan (persistent intermediate image) backends retain directly; the GL backend renders onto a
		/// persistent layer blitted to the swapchain each frame.
		/// </summary>
		bool SurfaceRetainsContents { get; }
	}
}
