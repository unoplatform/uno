#nullable enable

using SkiaSharp;
using Microsoft.CodeAnalysis.PooledObjects;
using Uno.Disposables;
using SKPaint = SkiaSharp.SKPaint;

namespace Windows.UI.Composition
{
	internal static class SkiaHelper
	{
		private static readonly ObjectPool<SKPaint> _paintPool = new(() => new SKPaint(), 8);
		private static readonly ObjectPool<SKPath> _pathPool = new(() => new SKPath(), 8);

		public static DisposableStruct<SKPath> GetTempSKPath(out SKPath path)
		{
			path = _pathPool.Allocate();
			// Note the difference between Rewind and Reset
			// https://api.skia.org/classSkPath.html#a8dc858ee4c95a59b3dd4bdd3f7b85fdc : "Use rewind() instead of reset() if SkPath storage will be reused and performance is critical."
			path.Rewind();
			return new DisposableStruct<SKPath>(p => _pathPool.Free(p), path);
		}

		public static DisposableStruct<SKPaint> GetTempSKPaint(out SKPaint paint)
		{
			paint = _paintPool.Allocate();
			// https://api.skia.org/classSkPaint.html#a6c7118c97a0e8819d75aa757afbc4c49
			// "This is equivalent to replacing SkPaint with the result of SkPaint()."
			paint.Reset();
			return new DisposableStruct<SKPaint>(p => _paintPool.Free(p), paint);
		}
	}
}
