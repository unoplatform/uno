using System.IO;

namespace SamplesApp.UITests
{
	public class ScreenshotInfo
	{
		public FileInfo File { get; }

		public string StepName { get; }

		public ScreenshotInfo(FileInfo file, string stepName)
		{
			File = file;
			StepName = stepName;
		}

		public static implicit operator FileInfo(ScreenshotInfo si) => si.File;

		public static implicit operator ScreenshotInfo(FileInfo fi) => new ScreenshotInfo(fi, fi.Name);
	}
}
