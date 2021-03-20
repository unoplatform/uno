namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl
	{
		private class TestRun
		{
			public int Run { get; set; }
			public int Ignored { get; set; }
			public int Succeeded { get; set; }
			public int Failed { get; set; }
		}
	}
}
