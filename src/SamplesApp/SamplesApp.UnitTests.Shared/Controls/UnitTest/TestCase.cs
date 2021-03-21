using System;

namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl
	{
		private class TestCase
		{
			public TestResult TestResult { get; set; }
			public string TestName { get; set; }
			public TimeSpan Duration { get; set; }
			public string Message { get; set; }
		}
	}
}
