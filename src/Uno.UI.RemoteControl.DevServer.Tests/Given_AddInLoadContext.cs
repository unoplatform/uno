using System.Reflection;
using System.Runtime.Loader;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Unit tests for <see cref="AddInLoadContext"/> assembly-bridging behaviour.
/// These tests verify that Load() returns the host's already-loaded Assembly for
/// framework and contract assemblies (step 1 of the three-step resolution chain),
/// preserving Type identity across the host/add-in ALC boundary.
/// </summary>
[TestClass]
public class Given_AddInLoadContext
{
	// ------------------------------------------------------------------ step 1: bridge via Default.Assemblies

	[TestMethod]
	[Description("Step 1 must return the host's System.Text.Json instance so JsonDocument/JsonElement types are identical between host and add-in.")]
	public void Load_ResolvesFrameworkOobAssembly_ToHostInstance()
	{
		// Force the assembly into Default.Assemblies (reference a type from it first).
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("System.Text.Json"));

		loaded.Should().BeSameAs(hostAssembly,
			"System.Text.Json must bridge to the host's already-loaded instance");
	}

	[TestMethod]
	[Description("Step 1 must return the host's Logging.Abstractions so ILogger contracts are identical between host and add-in.")]
	public void Load_ResolvesLoggingAbstractions_ToHostInstance()
	{
		var hostAssembly = typeof(ILogger).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("Microsoft.Extensions.Logging.Abstractions"));

		loaded.Should().BeSameAs(hostAssembly,
			"ILogger assembly must bridge to the host's instance for log sinks to work across the boundary");
	}

	[TestMethod]
	[Description("Step 1 must return the host's System.Text.Encodings.Web — the assembly whose version mismatch triggered the original crash (PR #23287).")]
	public void Load_ResolvesEncodingsWeb_ToHostInstance()
	{
		// Touching JavaScriptEncoder forces the assembly into Default.Assemblies.
		var hostAssembly = typeof(System.Text.Encodings.Web.JavaScriptEncoder).Assembly;

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(new AssemblyName("System.Text.Encodings.Web"));

		loaded.Should().BeSameAs(hostAssembly,
			"System.Text.Encodings.Web must bridge to the host's instance regardless of which major version the add-in compiled against");
	}

	// ------------------------------------------------------------------ IsDynamic guard

	[TestMethod]
	[Description("Even when a dynamic assembly is created with the same simple name as a real assembly in the same process, the bridge must return the static assembly and never the dynamic one.")]
	public void TryBridgeBySimpleName_SkipsDynamicAssemblies_WhenCandidateIsDynamic()
	{
		var staticAsm = typeof(System.Text.Json.JsonDocument).Assembly;

		// Create a dynamic assembly inside the Default ALC with the same simple
		// name as the static target. AssemblyBuilder.DefineDynamicAssembly with
		// AssemblyBuilderAccess.Run registers the dynamic assembly with the
		// currently-executing ALC (Default for test code). The IsDynamic guard
		// in TryBridgeBySimpleName must skip it so that the static assembly is
		// returned, not the dynamic one.
		_ = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName(staticAsm.GetName().Name!),
			System.Reflection.Emit.AssemblyBuilderAccess.Run);

		var requested = new AssemblyName("System.Text.Json");
		requested.SetPublicKeyToken(staticAsm.GetName().GetPublicKeyToken());

		var result = HostAssemblyResolution.TryBridgeBySimpleName(AssemblyLoadContext.Default, requested);

		result.Should().NotBeNull("the static assembly is present in Default.Assemblies");
		result!.IsDynamic.Should().BeFalse(
			"the bridge must never return a dynamic assembly even when one with a matching simple name exists");
		result.Should().BeSameAs(staticAsm,
			"the bridge must return the static assembly when a same-named dynamic one is present");
	}

	// ------------------------------------------------------------------ step 1 bridges without engaging Default.Resolving

	[TestMethod]
	[Description("Step 1 must bridge an assembly that is in Default.Assemblies but is NOT in TPA, proving step-1 is the active code path. " +
		"If step-1 is absent, step-2 (TPA lookup) throws FileNotFoundException for non-framework assemblies, making the test genuinely non-vacuous.")]
	public void Load_Step1_BridgesHostAssembly_WithoutGoingThroughDefaultResolving()
	{
		// The test assembly is loaded into Default.Assemblies by the test runner, but it
		// is NOT a TPA assembly — Default.LoadFromAssemblyName("...DevServer.Tests") will
		// throw FileNotFoundException because TPA has no entry for it. Step-2 therefore
		// cannot satisfy this request; only step-1 can. Removing the step-1 bridge in
		// AddInLoadContext.Load causes ctx.LoadFromAssemblyName to throw here, turning
		// this into a genuine red test.
		var hostAssembly = typeof(Given_AddInLoadContext).Assembly;
		AssemblyLoadContext.Default.Assemblies.Should().Contain(hostAssembly,
			"the test assembly must be in Default.Assemblies for this test to be valid");

		var requested = new AssemblyName(hostAssembly.GetName().Name!);

		var ctx = new AddInLoadContext(Array.Empty<string>());
		var loaded = ctx.LoadFromAssemblyName(requested);

		loaded.Should().BeSameAs(hostAssembly,
			"step 1 must bridge to the host's already-loaded test assembly; " +
			"without step-1, step-2 TPA lookup throws FileNotFoundException and this assertion is never reached");
	}

	// ------------------------------------------------------------------ null / miss

	[TestMethod]
	[Description("Load returns null for an assembly not in Default.Assemblies, not in TPA, and not in any resolver — the ALC then throws FileNotFoundException as specified.")]
	public void LoadFromAssemblyName_ThrowsFileNotFoundException_WhenAssemblyIsUnknown()
	{
		var ctx = new AddInLoadContext(Array.Empty<string>());

		// Totally unknown assembly: Load returns null → ALC throws FNFE.
		var act = () => ctx.LoadFromAssemblyName(new AssemblyName("Totally.Unknown.Assembly.XYZ.DoesNotExist"));

		act.Should().Throw<FileNotFoundException>(
			"when Load returns null for all contexts, the runtime raises FileNotFoundException");
	}

	// ------------------------------------------------------------------ step 4: file-system probe across add-in directories
	//
	// Steps 1-3 do not probe the file system: step 1 only consults already-loaded assemblies in Default
	// ALC, step 2 only consults the host's TPA, and step 3 only consults each add-in's .deps.json (see
	// AssemblyDependencyResolver — https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblydependencyresolver).
	// When a contract DLL is physically present in an add-in's directory but absent from every add-in's
	// deps.json (a packaging quirk that occurs when a transitive contract assembly is not exposed as a
	// runtime asset), the first three steps all return null and the call site sees a FileNotFoundException.
	// The step-4 file-system probe iterates the known add-in directories — exactly what Assembly.LoadFrom's
	// default runtime probing used to do implicitly before PR #23287 — without requiring the host to know
	// the assembly name in advance. Zero white-list, zero add-in modification.

	private static string StagedConsumerDll => Path.Combine(
		Path.GetDirectoryName(typeof(Given_AddInLoadContext).Assembly.Location)!,
		"Fixtures", "AddInWithSharedContractConsumer", "AddInWithSharedContractConsumer.dll");

	private static string StagedContractsDll => Path.Combine(
		Path.GetDirectoryName(typeof(Given_AddInLoadContext).Assembly.Location)!,
		"Fixtures", "AddInWithSharedContractProvider", "Uno.Licensing.TestContracts.dll");

	private static void AssertContractsNotInDefaultAlc()
	{
		AssemblyLoadContext.Default.Assemblies
			.Should().NotContain(a => string.Equals(a.GetName().Name, "Uno.Licensing.TestContracts", StringComparison.OrdinalIgnoreCase),
				"this test exercises the file-system probe; the contracts assembly is not allowed in Default ALC at this point");
	}

	/// <summary>
	/// Stages an add-in's <c>.dll</c> and its real <c>.deps.json</c> side-by-side in
	/// <paramref name="targetDir"/>. Copying the genuine deps.json is what makes the
	/// test reproduce the production failure: with a well-formed deps.json that does
	/// not list the contract assembly, <see cref="AssemblyDependencyResolver"/> reports
	/// "no match" instead of falling back to a permissive directory scan it sometimes
	/// performs when no deps.json is found at all. Without this fidelity step 3 of
	/// <see cref="AddInLoadContext.Load"/> would resolve the contract from the directory
	/// and bypass step 4 entirely, making the test green even without the fix.
	/// </summary>
	private static string StageAddIn(string sourceDll, string targetDir, string newDllName)
	{
		var anchor = Path.Combine(targetDir, newDllName);
		File.Copy(sourceDll, anchor);

		// Copy the matching .deps.json so AssemblyDependencyResolver behaves as in
		// production (well-formed graph that simply does not contain the target
		// contract assembly), not in a degraded "no deps.json" mode.
		var sourceDepsJson = Path.ChangeExtension(sourceDll, ".deps.json");
		if (File.Exists(sourceDepsJson))
		{
			var targetDepsJson = Path.ChangeExtension(anchor, ".deps.json");
			File.Copy(sourceDepsJson, targetDepsJson);
		}

		return anchor;
	}

	[TestMethod]
	[Description("File-system probe: when an assembly is physically present in an add-in directory but absent from every add-in's deps.json, AddInLoadContext.Load must locate and load it by enumerating the add-in directories.")]
	public void Load_ResolvesAssemblyFromAddInDirectory_WhenNotInAnyDepsJson()
	{
		File.Exists(StagedConsumerDll).Should().BeTrue(
			"the consumer add-in fixture must be staged by _BuildAndCopyAddInTestFixtures");
		File.Exists(StagedContractsDll).Should().BeTrue(
			"the contracts fixture must be staged by _BuildAndCopyAddInTestFixtures");

		AssertContractsNotInDefaultAlc();

		var tempDir = Path.Combine(Path.GetTempPath(), "uno-step4-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);
		try
		{
			// Stage the anchor add-in WITH its real .deps.json (which intentionally
			// does NOT list Uno.Licensing.TestContracts). Then drop the contract DLL
			// next to it on disk. Step 3 returns null because the resolver respects
			// the deps.json; only the new step-4 file-system probe can satisfy the
			// request.
			var anchorPath = StageAddIn(StagedConsumerDll, tempDir, "AddInWithSharedContractConsumer.dll");
			File.Copy(StagedContractsDll, Path.Combine(tempDir, "Uno.Licensing.TestContracts.dll"));

			var ctx = new AddInLoadContext([anchorPath]);

			var resolved = ctx.LoadFromAssemblyName(new AssemblyName("Uno.Licensing.TestContracts"));

			resolved.Should().NotBeNull("the file-system probe step must locate the DLL co-located with the add-in");
			resolved.GetName().Name.Should().Be("Uno.Licensing.TestContracts");
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
		}
	}

	[TestMethod]
	[Description("File-system probe must scan every add-in directory, not only the directory of the first add-in — this is what makes cross-add-in type sharing work when the contract DLL is physically present in one add-in's directory but missing from another's.")]
	public void Load_ProbesAllAddInDirectories_NotOnlyTheFirst()
	{
		File.Exists(StagedConsumerDll).Should().BeTrue();
		File.Exists(StagedContractsDll).Should().BeTrue();

		AssertContractsNotInDefaultAlc();

		var dirA = Path.Combine(Path.GetTempPath(), "uno-step4-A-" + Guid.NewGuid().ToString("N"));
		var dirB = Path.Combine(Path.GetTempPath(), "uno-step4-B-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dirA);
		Directory.CreateDirectory(dirB);
		try
		{
			// dirA holds the consumer-style anchor with its real .deps.json (no
			// contracts entry). dirB holds another anchor plus the contracts DLL
			// on disk. The probe must reach dirB even though the first registered
			// directory is dirA — mirrors the production scenario where consumer
			// and provider live in separate per-add-in directories.
			var anchorA = StageAddIn(StagedConsumerDll, dirA, "AddInWithSharedContractConsumer.dll");
			var anchorB = StageAddIn(StagedConsumerDll, dirB, "AddInWithSharedContractConsumer.dll");
			File.Copy(StagedContractsDll, Path.Combine(dirB, "Uno.Licensing.TestContracts.dll"));

			var ctx = new AddInLoadContext([anchorA, anchorB]);

			var resolved = ctx.LoadFromAssemblyName(new AssemblyName("Uno.Licensing.TestContracts"));

			resolved.Should().NotBeNull(
				"the probe must consider every registered add-in directory; dirB contains the contracts DLL even though dirA does not");
			resolved.GetName().Name.Should().Be("Uno.Licensing.TestContracts");
		}
		finally
		{
			try { Directory.Delete(dirA, recursive: true); } catch { /* best-effort */ }
			try { Directory.Delete(dirB, recursive: true); } catch { /* best-effort */ }
		}
	}

	[TestMethod]
	[Description("When all four resolution steps fail (no Default match, no TPA hit, no resolver, no file on disk in any add-in directory), Load returns null so the runtime surfaces a clean FileNotFoundException at the call site.")]
	public void Load_ReturnsNull_WhenAssemblyNotFoundAnywhere()
	{
		File.Exists(StagedConsumerDll).Should().BeTrue();

		var tempDir = Path.Combine(Path.GetTempPath(), "uno-step4-miss-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);
		try
		{
			var anchorPath = StageAddIn(StagedConsumerDll, tempDir, "AddInWithSharedContractConsumer.dll");

			var ctx = new AddInLoadContext([anchorPath]);

			// Use a name that cannot possibly resolve through any of the four steps.
			Action act = () => ctx.LoadFromAssemblyName(new AssemblyName("Totally.Unknown.Assembly.XYZ.DoesNotExist"));

			act.Should().Throw<FileNotFoundException>(
				"when no step (including the file-system probe) can satisfy the request, Load returns null and the runtime throws");
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
		}
	}
}
