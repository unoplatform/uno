using System;

namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl
	{
		private class TestRun
		{
			public TestRun()
			{
				StartTime = DateTimeOffset.UtcNow;
			}

			public int Run { get; set; }

			public int Ignored { get; set; }

			public int Inconclusive { get; set; }

			public int Succeeded { get; set; }

			public int Failed { get; set; }

			public int CurrentRepeatCount { get; set; }

			public DateTimeOffset StartTime { get; }
		}
	}
}
