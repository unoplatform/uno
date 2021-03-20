#nullable enable
using System;
using System.Drawing;
using System.IO;

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

		public Bitmap GetBitmap() => _bitmap ??= new Bitmap(File.FullName);
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
