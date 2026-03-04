namespace Uno.UI.Samples.Tests;

public class UnitTestEngineConfig
{
	public const int DefaultRepeatCount = 3;

	public static UnitTestEngineConfig Default { get; } = new UnitTestEngineConfig();

	public string[] Filters { get; set; }

	public int Attempts { get; set; } = DefaultRepeatCount;

	public bool IsConsoleOutputEnabled { get; set; }

	public bool IsRunningIgnored { get; set; }

	public bool IsUnloadingTestContent { get; set; }

	public int Iterations { get; set; } = 1;

	public int TestStartIndex { get; set; } = 0;

	public int TestCount { get; set; } = -1; // -1 = run all
}
