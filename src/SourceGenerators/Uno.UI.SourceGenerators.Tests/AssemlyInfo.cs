[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public class AssemblyHooks
{
	// Keeps the generators run by these tests from sending build telemetry.
	[AssemblyInitialize]
	public static void OptOutOfTelemetry(TestContext context)
		=> Environment.SetEnvironmentVariable("UNO_PLATFORM_TELEMETRY_OPTOUT", "true");
}
