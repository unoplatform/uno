using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

/// <summary>
/// The EnC baseline of a project is its assembly on disk. The manager may only create the ones
/// that are missing: overwriting an existing one (the real build the application runs, or one it
/// references) with a fresh compile changes its MVID, and every delta is then dropped by the app
/// ("Invalid metadata update, ignore it.").
/// </summary>
[TestClass]
public sealed class Given_HotReloadManager_CompilationOutput
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"Only one project of the solution has no assembly on disk (e.g. a library flavor the " +
		"running head never builds). It must be emitted, while the assemblies that already exist " +
		"(the real build) keep their exact bytes, MVID and timestamp.")]
	public async Task When_OneProjectAssemblyIsMissing_Then_OnlyThatOneIsEmitted()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var enc = await EnCHarness.CreateAsync(temp, ct, otherText: EnCHarness.AppText("\"other\""));
		enc.OwnsSession = false; // the manager's Dispose ends the session it creates itself

		var appDll = Path.Join(temp.Path, "bin", "App.dll");
		var otherDll = Path.Join(temp.Path, "bin", "Other.dll");

		// Stand-in for the real build: a different compile of the same assembly name, with a different MVID.
		var realBuild = EnCHarness.EmitLibrary(temp, "App", "9.9.9.0", "public class RealBuild {}", subdir: "real");
		File.Copy(realBuild, appDll, overwrite: true);
		var oldTime = DateTime.UtcNow.AddDays(-3);
		File.SetLastWriteTimeUtc(appDll, oldTime);
		File.Delete(otherDll);

		var bytesBefore = await File.ReadAllBytesAsync(appDll, ct);
		var mvidBefore = ReadMvid(appDll);

		using var manager = await HotReloadManager.CreateAsync(
			enc.Solution,
			["Baseline"],
			new HotReloadManagerHarness.RecordingHandler(),
			new NoChangesDetector(),
			new HotReloadTracker((_, _) => ValueTask.CompletedTask),
			ct);

		(await File.ReadAllBytesAsync(appDll, ct)).Should().Equal(bytesBefore, "an existing assembly is the real build");
		ReadMvid(appDll).Should().Be(mvidBefore);
		File.GetLastWriteTimeUtc(appDll).Should().Be(oldTime, "an existing assembly must not even be touched");
		File.Exists(otherDll).Should().BeTrue("the missing assembly is the one that gets emitted");
	}

	[TestMethod]
	[Description("forceEmitCompilationOutput keeps its meaning: every project is re-emitted.")]
	public async Task When_ForceEmit_Then_ExistingAssembliesAreRewritten()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var enc = await EnCHarness.CreateAsync(temp, ct);
		enc.OwnsSession = false;

		var appDll = Path.Join(temp.Path, "bin", "App.dll");
		var realBuild = EnCHarness.EmitLibrary(temp, "App", "9.9.9.0", "public class RealBuild {}", subdir: "real");
		File.Copy(realBuild, appDll, overwrite: true);
		var mvidBefore = ReadMvid(appDll);

		using var manager = await HotReloadManager.CreateAsync(
			enc.Solution,
			["Baseline"],
			new HotReloadManagerHarness.RecordingHandler(),
			new NoChangesDetector(),
			new HotReloadTracker((_, _) => ValueTask.CompletedTask),
			ct,
			forceEmitCompilationOutput: true);

		ReadMvid(appDll).Should().NotBe(mvidBefore);
	}

	private static Guid ReadMvid(string path)
	{
		using var pe = new PEReader(File.OpenRead(path));
		var reader = pe.GetMetadataReader();
		return reader.GetGuid(reader.GetModuleDefinition().Mvid);
	}

	private sealed class NoChangesDetector : IChangesDetector
	{
		public ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
			=> ValueTask.FromResult(ChangeSet.IgnoreAll([]));
	}
}
