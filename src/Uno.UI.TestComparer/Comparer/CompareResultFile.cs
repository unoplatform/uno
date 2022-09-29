#nullable disable

using System.Collections.Generic;

namespace Uno.UI.TestComparer.Comparer
{
	internal class CompareResultFile
	{
		public CompareResultFile()
		{
		}

		public List<CompareResultFileRun> ResultRun { get; internal set; } = new List<CompareResultFileRun>();
		public string TestName { get; internal set; }
		public bool HasChanged { get; internal set; }
	}
}
