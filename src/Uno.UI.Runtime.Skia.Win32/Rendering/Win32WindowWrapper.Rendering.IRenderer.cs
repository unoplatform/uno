using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	// Host render surface abstraction. Hands the composition a backend-neutral IRenderTarget
	// (ISoftwareRenderTarget / IGLRenderTarget) — the host owns the native surface + present, the Skia
	// backend owns the SKSurface/GRContext. No Skia type crosses this boundary.
	private interface IRenderer : IDisposable
	{
		void StartPaint();
		void EndPaint();

		IRenderTarget UpdateSize(int width, int height);
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
