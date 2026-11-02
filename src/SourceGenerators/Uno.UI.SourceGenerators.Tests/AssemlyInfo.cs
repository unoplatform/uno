[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public static class AssemblyHooks
{
	// The telemetry library reads this, so the clients that these tests' generator runs create stay disabled.
	// The generator's own check never sees it: the test harness doesn't pass it as a build property.
	[AssemblyInitialize]
	public static void OptOutOfTelemetry(TestContext _)
		=> Environment.SetEnvironmentVariable("UNO_PLATFORM_TELEMETRY_OPTOUT", "true");
}
