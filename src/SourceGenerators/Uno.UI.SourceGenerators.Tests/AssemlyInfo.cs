[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public static class AssemblyHooks
{
	// The generators these tests run would otherwise send build telemetry from developers' machines.
	[AssemblyInitialize]
	public static void OptOutOfTelemetry(TestContext _)
		=> Environment.SetEnvironmentVariable("UNO_PLATFORM_TELEMETRY_OPTOUT", "true");
}
