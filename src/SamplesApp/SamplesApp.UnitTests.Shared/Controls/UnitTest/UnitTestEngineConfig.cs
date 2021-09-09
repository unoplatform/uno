using System;
using System.Linq;

namespace Uno.UI.Samples.Tests
{
	public class UnitTestEngineConfig
	{
		public string[] Filters { get; set; }

		public int Attempts { get; set; }

		public bool IsConsoleOutputEnabled { get; set; }

		public bool IsRunningIgnored { get; set; }
	}
}
