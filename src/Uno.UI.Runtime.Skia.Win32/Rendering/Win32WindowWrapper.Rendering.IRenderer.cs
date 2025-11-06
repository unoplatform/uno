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
	}
}
