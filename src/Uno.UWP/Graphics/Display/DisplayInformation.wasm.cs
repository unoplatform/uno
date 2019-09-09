#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		// devicePixelRatio of 1 = https://developer.mozilla.org/en-US/docs/Web/API/Window/devicePixelRatio
		private const float BaseDPI = 96.0f;

		partial void Initialize()
		{
			UpdateProperties();
		}

		private void UpdateProperties()
		{
			UpdateLogicalProperties();
			UpdateRawProperties();
		}

		private void UpdateLogicalProperties()
		{
			if (ReadJSFloat("window.devicePixelRatio", out var devicePixelRatio))
			{
				LogicalDpi = devicePixelRatio * BaseDPI;
				ResolutionScale = (ResolutionScale)LogicalDpi;
			}
			else
			{
				LogicalDpi = BaseDPI;
				ResolutionScale = ResolutionScale.Scale100Percent;
			}
		}

		private static bool ReadJSFloat(string property, out float value)
		{
			return float.TryParse(WebAssembly.Runtime.InvokeJS(property), out value);
		}

		private void UpdateRawProperties()
		{
			if (ReadJSFloat("window.screen.width", out var width)
				&& ReadJSFloat("window.screen.height", out var height))
			{
				var scale = (double)LogicalDpi / BaseDPI;
				ScreenWidthInRawPixels = (uint)((double)width * scale);
				ScreenHeightInRawPixels = (uint)((double)height * scale);
				RawPixelsPerViewPixel = scale;
			}
		}
	}
}
#endif
