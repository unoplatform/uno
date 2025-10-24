using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Uno.UI.UI.Input.Internal;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

partial class Win32WindowWrapper : INativeInputNonClientPointerSource
{
	private readonly Dictionary<NonClientRegionKind, List<RectInt32>> _regionRects = new();

	private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	// --- Win32 constants
	private const int WM_NCHITTEST = 0x0084;
	private const int GWLP_WNDPROC = -4;
	private const int HTCLIENT = 1;
	private const int HTCAPTION = 2;
	private const int HTSYSMENU = 3;
	private const int HTMINBUTTON = 8;
	private const int HTMAXBUTTON = 9;
	private const int HTLEFT = 10;
	private const int HTRIGHT = 11;
	private const int HTTOP = 12;
	private const int HTBOTTOM = 15;
	private const int HTCLOSE = 20;

	// 🪟 Implement the INativeInputNonClientPointerSource contract
	public void ClearAllRegionRects()
		=> _regionRects.Clear();

	public void ClearRegionRects(NonClientRegionKind region)
		=> _regionRects.Remove(region);

	public RectInt32[] GetRegionRects(NonClientRegionKind region)
		=> _regionRects.TryGetValue(region, out var list) ? list.ToArray() : Array.Empty<RectInt32>();

	public void SetRegionRects(NonClientRegionKind region, RectInt32[] rects)
	{
		_regionRects[region] = new List<RectInt32>(rects);
	}

	private bool TryHandleNonClientHitTest(HWND hWnd, WPARAM wParam, LPARAM lParam, out IntPtr result)
	{
		var defResult = PInvoke.DefWindowProc(hWnd, WM_NCHITTEST, wParam, lParam);

		if ((int)defResult == HTCLIENT && _regionRects.Count > 0)
		{
			int x = (short)(lParam.Value.ToInt32() & 0xFFFF);
			int y = (short)((lParam.Value.ToInt32() >> 16) & 0xFFFF);

			var p = new System.Drawing.Point { X = x, Y = y };
			PInvoke.ScreenToClient(hWnd, ref p);

			foreach (var kv in _regionRects)
			{
				foreach (var rect in kv.Value)
				{
					var contains = p.X >= rect.X && p.X < rect.X + rect.Width &&
								   p.Y >= rect.Y && p.Y < rect.Y + rect.Height;
					if (contains)
					{
						result = RegionKindToHitTest(kv.Key);
						return true;
					}
				}
			}
		}

		result = (IntPtr)defResult;
		return true;
	}

	private static int RegionKindToHitTest(NonClientRegionKind kind) => kind switch
	{
		NonClientRegionKind.Close => HTCLOSE,
		NonClientRegionKind.Maximize => HTMAXBUTTON,
		NonClientRegionKind.Minimize => HTMINBUTTON,
		NonClientRegionKind.Icon => HTSYSMENU,
		NonClientRegionKind.Caption => HTCAPTION,
		NonClientRegionKind.TopBorder => HTTOP,
		NonClientRegionKind.LeftBorder => HTLEFT,
		NonClientRegionKind.BottomBorder => HTBOTTOM,
		NonClientRegionKind.RightBorder => HTRIGHT,
		NonClientRegionKind.Passthrough => HTCLIENT,
		_ => HTCLIENT
	};
}
