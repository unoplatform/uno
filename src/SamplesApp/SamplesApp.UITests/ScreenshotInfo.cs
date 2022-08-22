#nullable enable
using System;
using System.Drawing;
using System.IO;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests
{
	public partial class ScreenshotInfo : IDisposable
	{
		private Bitmap? _bitmap;
		public FileInfo File { get; }

		public string StepName { get; }

		public ScreenshotInfo(FileInfo file, string stepName)
		{
			File = file;
			StepName = stepName;
		}

		public static implicit operator FileInfo(ScreenshotInfo si) => si.File;

		public static implicit operator ScreenshotInfo(FileInfo fi) => new ScreenshotInfo(fi, fi.Name);

		public Bitmap GetBitmap()
		{
			ImageAssert.TryIgnoreImageAssert();

			return _bitmap ??= new Bitmap(File.FullName);
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
}
