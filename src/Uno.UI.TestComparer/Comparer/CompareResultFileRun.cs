#nullable disable

namespace Uno.UI.TestComparer.Comparer
{
	internal class CompareResultFileRun
	{
		public int ImageId { get; internal set; }
		public string ImageSha { get; internal set; }
		public string DiffResultImage { get; internal set; }
		public string SourceFile { get; internal set; }
		public string FilePath { get; internal set; }
		public bool HasChanged { get; internal set; }
		public int FolderIndex { get; internal set; }
	}
}
