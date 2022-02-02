using System;
using System.Linq;

namespace Uno.UI.Samples.Tests
{
	public class UnitTestEngineConfig
	{
		public const int DefaultRepeatCount = 3;
		public const bool DefaultIsScrollerEnabled = true;

		public static UnitTestEngineConfig Default { get; } = new UnitTestEngineConfig();

		public string[] Filters { get; set; }

		public int Attempts { get; set; } = DefaultRepeatCount;

		public bool IsConsoleOutputEnabled { get; set; }

		public bool IsRunningIgnored { get; set; }

		public bool IsScrollerEnabled { get; set; } = DefaultIsScrollerEnabled;
	}
}
