#nullable enable

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public static class SkiaHelper
	{
		[ThreadStatic] private static readonly SKPath _tempPath = new SKPath();
		[ThreadStatic] private static readonly SKPaint _tempPaint = new SKPaint();
		[ThreadStatic] private static readonly SKPaint _tempPaint2 = new SKPaint();

		public static SKPath GetTempSKPath()
		{
			// Note the difference between Rewind and Reset
			// https://api.skia.org/classSkPath.html#a8dc858ee4c95a59b3dd4bdd3f7b85fdc : "Use rewind() instead of reset() if SkPath storage will be reused and performance is critical."
			_tempPath.Rewind();
			return _tempPath;
		}

		public static SKPaint GetTempSKPaint()
		{
			// https://api.skia.org/classSkPaint.html#a6c7118c97a0e8819d75aa757afbc4c49
			// "This is equivalent to replacing SkPaint with the result of SkPaint()."
			_tempPaint.Reset();
			return _tempPaint;
		}

		public static SKPaint GetAnotherTempSKPaint()
		{
			// https://api.skia.org/classSkPaint.html#a6c7118c97a0e8819d75aa757afbc4c49
			// "This is equivalent to replacing SkPaint with the result of SkPaint()."
			_tempPaint2.Reset();
			return _tempPaint2;
		}
	}
}
