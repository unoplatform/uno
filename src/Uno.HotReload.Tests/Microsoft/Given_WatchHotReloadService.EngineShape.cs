using AwesomeAssertions;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests.Microsoft;

/// <summary>
/// The shim reads Roslyn's Edit-and-Continue results reflectively, resolving every member once at
/// session creation. A required member that moves throws there, naming it; an OPTIONAL member that
/// is present but changed shape cannot throw — the older Roslyn line legitimately does not have
/// some of them — so it degrades to an empty value and records a warning instead. This is the
/// canary for that silent half: on the Roslyn line actually embedded, nothing should degrade.
/// </summary>
[TestClass]
public sealed class Given_WatchHotReloadService_EngineShape
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"Every member of Roslyn's emit results this shim reads is readable on the embedded Roslyn. A " +
		"failure here names the member and what it became — and means the corresponding hot-reload " +
		"information is being reported as empty for every session.")]
	public async Task When_SessionStarted_Then_NoEngineShapeWarning()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct);

		harness.Watch.EngineShapeWarnings.Should().BeEmpty();
	}
}
