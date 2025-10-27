using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Runtime.Skia.Win32.UI.Xaml.Window;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Skia.Win32;

partial class Win32WindowWrapper
{
	private static System.Drawing.Point PointFromLParam(LPARAM lParam)
	{
		return new System.Drawing.Point((int)(lParam.Value) & 0xffff, (int)(lParam.Value) >> 16);
	}

	private bool TryHandleCustomCaptionMessage(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam, out LRESULT result)
	{
		var handled = PInvoke.DwmDefWindowProc(hWnd, msg, wParam, lParam, out result);

		switch (msg)
		{
			case PInvoke.WM_NCHITTEST:
				if (result == IntPtr.Zero)
				{
					var hittestResult = NonClientAreaHitTest(hWnd, wParam, lParam);

					if (hittestResult != Win32NonClientHitTestKind.HTNOWHERE)
					{
						result = new LRESULT((IntPtr)hittestResult);
						handled = true;
					}
				}
				break;
		}

		return handled;
	}
}
