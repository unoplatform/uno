using System.Collections.Generic;

namespace Uno.UI.TestComparer.Comparer
{
	internal class CompareResult
	{
		public CompareResult(string platform)
			=> Platform = platform;

		public int TotalTests { get; internal set; }
		public int UnchangedTests { get; internal set; }
		public List<CompareResultFile> Tests { get;} = new List<CompareResultFile>();
		public List<(int index, string path)> Folders { get; } = new List<(int index, string path)>();
		public string Platform { get; }
	}
}
