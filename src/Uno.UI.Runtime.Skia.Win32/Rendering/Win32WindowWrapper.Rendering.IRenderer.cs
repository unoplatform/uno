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
		/// Notifies the renderer of a screen refresh rate change. VSync-paced renderers ignore
		/// this; software-timer-paced ones (e.g. the DwmFlush degraded fallback) retarget.
		/// </summary>
		void UpdateRefreshRate(double fps);
	}
}
