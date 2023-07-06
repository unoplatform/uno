#nullable enable
using System;
using System.IO;
using SamplesApp.UITests.TestFramework;
using SkiaSharp;

namespace SamplesApp.UITests
{
	public partial class ScreenshotInfo : IDisposable
	{
		private SKBitmap? _bitmap;
		public FileInfo File { get; }

		public string StepName { get; }

		public ScreenshotInfo(FileInfo file, string stepName)
		{
			File = file;
			StepName = stepName;
		}

		public static implicit operator FileInfo(ScreenshotInfo si) => si.File;

		public static implicit operator ScreenshotInfo(FileInfo fi) => new ScreenshotInfo(fi, fi.Name);

		public PlatformBitmap GetBitmap()
		{
			ImageAssert.TryIgnoreImageAssert();

			if (_bitmap is null)
			{
				using var input = System.IO.File.OpenRead(File.FullName);
				using var inputStream = new SKManagedStream(input);

				_bitmap = SKBitmap.Decode(inputStream);
			}

			return new(_bitmap);
		}

		public int Width => GetBitmap().Width;

		public int Height => GetBitmap().Height;

		public void Dispose()
		{
			_bitmap?.Dispose();
			_bitmap = null;
		}

		~ScreenshotInfo()
		{
			Dispose();
		}
	}

	public class PlatformBitmap : IDisposable
	{
		SKBitmap _bitmap;

		public PlatformBitmap(SKBitmap bitmap)
		{
			_bitmap = bitmap;
		}

		public PlatformBitmap(Stream bitmap)
		{
			using var inputStream = new SKManagedStream(bitmap);
			_bitmap = SKBitmap.Decode(inputStream);
		}


		public int Width => _bitmap.Width;

		public int Height => _bitmap.Height;

		public System.Drawing.Size Size => new(_bitmap.Width, _bitmap.Height);

		public System.Drawing.Color GetPixel(int x, int y)
			=> _bitmap.GetPixel(x, y).ToColor();

		public void Dispose()
			=> _bitmap.Dispose();
	}
}
