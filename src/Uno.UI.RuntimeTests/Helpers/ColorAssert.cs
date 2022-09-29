#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class ColorAssert
	{
		public static void IsLight(Color? actualColor, int tolerance = 50)
		{
			if (!(actualColor is { } colorValue))
			{
				throw new AssertFailedException("Expected a valid color but received null");
			}

			if (colorValue.A == 0)
			{
				throw new AssertFailedException("Expected a valid color but Alpha was 0");
			}

			var threshold = 255 - tolerance;
			if (colorValue.R < threshold || colorValue.G < threshold || colorValue.B < threshold)
			{
				throw new AssertFailedException($"Expected light color but received {colorValue}");
			}
		}

		public static void IsDark(Color? actualColor, int tolerance = 50)
		{
			if (!(actualColor is { } colorValue))
			{
				throw new AssertFailedException("Expected a valid color but received null");
			}

			if (colorValue.A == 0)
			{
				throw new AssertFailedException("Expected a valid color but Alpha was 0");
			}

			var threshold = tolerance;
			if (colorValue.R > threshold || colorValue.G > threshold || colorValue.B > threshold)
			{
				throw new AssertFailedException($"Expected dark color but received {colorValue}");
			}
		}
	}
}
