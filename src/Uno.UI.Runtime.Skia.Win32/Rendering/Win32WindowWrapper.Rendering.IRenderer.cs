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
		/// Notifies the renderer of a screen refresh rate change. Renderers that pace
		/// internally via VSync (GL via wglSwapInterval, software DwmFlush) are
		/// inherently aligned to the refresh rate and can ignore this; renderers that
		/// pace via a software timer (e.g. the software DwmFlush degraded fallback
		/// FramePacer) can use it to retarget.
		/// </summary>
		void UpdateRefreshRate(double fps);
	}
}
