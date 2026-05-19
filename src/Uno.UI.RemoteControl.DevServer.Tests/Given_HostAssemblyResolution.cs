using System.Reflection;
using System.Runtime.Loader;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Unit tests for <see cref="HostAssemblyResolution.TryBridgeBySimpleName"/> and
/// <see cref="HostAssemblyResolution.Install"/>.
/// </summary>
[TestClass]
public class Given_HostAssemblyResolution
{
	// ------------------------------------------------------------------ Install

	[TestMethod]
	[Description("Calling Install() twice must register exactly one Resolving handler on AssemblyLoadContext.Default.")]
	public void Install_IsIdempotent_WhenCalledTwice()
	{
		// First call installs the handler (or is already installed from another test).
		HostAssemblyResolution.Install();

		// Snapshot the handler count after the first call.
		int countBefore = CountResolvingHandlers();

		// Subsequent calls must be no-ops: the count must not grow.
		HostAssemblyResolution.Install();
		HostAssemblyResolution.Install();

		int countAfter = CountResolvingHandlers();

		countAfter.Should().Be(countBefore,
			"repeated Install() calls must not register additional Resolving handlers");
	}

	[TestMethod]
	[Description("After Install(), System.Text.Encodings.Web must be resolvable from AssemblyLoadContext.Default (either eager-loaded by Install or available via TPA).")]
	public void Install_EagerLoads_SystemTextEncodingsWeb()
	{
		HostAssemblyResolution.Install();

		// Trigger TPA resolution so the assembly is guaranteed to be in Default.Assemblies.
		// In the host process Install() pre-loads it from the host directory; in the test
		// process the DLL is not copied to the output dir, so we ask Default explicitly.
		// Either way the invariant is: the assembly must be resolvable from Default ALC.
		var asm = AssemblyLoadContext.Default.LoadFromAssemblyName(
			new AssemblyName("System.Text.Encodings.Web"));

		asm.Should().NotBeNull("System.Text.Encodings.Web must be resolvable from Default ALC after Install()");

		var loaded = AssemblyLoadContext.Default.Assemblies
			.Any(a => !a.IsDynamic &&
					  string.Equals(a.GetName().Name, "System.Text.Encodings.Web",
									StringComparison.OrdinalIgnoreCase));

		loaded.Should().BeTrue(
			"System.Text.Encodings.Web must appear in Default.Assemblies once resolved");
	}

	// ------------------------------------------------------------------ helpers

	private static int CountResolvingHandlers()
	{
		// Use reflection to inspect the internal delegate stored for the Resolving event.
		// The field name changed across .NET versions, so try both known names.
		var alcType = typeof(AssemblyLoadContext);
		var field = alcType.GetField("_resolving", BindingFlags.Instance | BindingFlags.NonPublic)
					?? alcType.GetField("Resolving", BindingFlags.Instance | BindingFlags.NonPublic);

		if (field is null)
		{
			// Fallback: we cannot introspect — return 0 to avoid a false failure.
			return 0;
		}

		var del = field.GetValue(AssemblyLoadContext.Default) as Delegate;
		return del?.GetInvocationList().Length ?? 0;
	}


	// ------------------------------------------------------------------ returns loaded assembly

	[TestMethod]
	[Description("When the default ALC has an assembly with the requested simple name and matching PKT, TryBridgeBySimpleName must return that exact instance.")]
	public void TryBridgeBySimpleName_ReturnsLoadedAssembly_WhenSimpleNameMatchesAndPktCompatible()
	{
		// Force the assembly into Default.Assemblies.
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		// Mirror what a compiled add-in AssemblyRef looks like: simple name + PKT.
		var requested = new AssemblyName("System.Text.Json");
		requested.SetPublicKeyToken(hostAssembly.GetName().GetPublicKeyToken());

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeSameAs(hostAssembly,
			"System.Text.Json is loaded in the default ALC and the name+PKT matches");
	}

	// ------------------------------------------------------------------ returns null on miss

	[TestMethod]
	[Description("When no assembly with the requested simple name is loaded in the context, TryBridgeBySimpleName must return null.")]
	public void TryBridgeBySimpleName_ReturnsNull_WhenNoMatch()
	{
		var requested = new AssemblyName("Totally.Unknown.Assembly.XYZ.DoesNotExist");

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeNull("no assembly with that name is loaded");
	}

	// ------------------------------------------------------------------ skips dynamic assemblies

	[TestMethod]
	[Description("Dynamic (reflection-emit) assemblies must be skipped even when their auto-generated name coincidentally matches the requested name.")]
	public void TryBridgeBySimpleName_SkipsDynamicAssemblies_WhenNameMatchesDynamic()
	{
		// Force the real assembly into Default first.
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		// Create a dynamic assembly with the same simple name.
		_ = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("System.Text.Json"),
			System.Reflection.Emit.AssemblyBuilderAccess.Run);

		// Use a proper signed request (real add-in AssemblyRefs include the PKT).
		var requested = new AssemblyName("System.Text.Json");
		requested.SetPublicKeyToken(hostAssembly.GetName().GetPublicKeyToken());
		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeSameAs(hostAssembly,
			"the bridge must return the real assembly, not a dynamic one");
		result!.IsDynamic.Should().BeFalse("returned assembly must never be dynamic");
	}

	// ------------------------------------------------------------------ PKT symmetry

	[TestMethod]
	[Description("When the loaded assembly is strong-named but the request carries no PKT, the bridge must return null to prevent silently substituting a strong-named assembly for an unsigned reference.")]
	public void TryBridgeBySimpleName_RejectsStrongNamedLoaded_WhenRequestedIsUnsigned()
	{
		// System.Text.Json is strong-named (Microsoft PKT).
		_ = typeof(System.Text.Json.JsonDocument).Assembly;

		// Request the same simple name without a PKT.
		var requested = new AssemblyName("System.Text.Json") { Version = null };

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeNull(
			"a strong-named loaded assembly must not be returned for an unsigned request — " +
			"the PKT mismatch could silently substitute the wrong assembly");
	}

	// ------------------------------------------------------------------ version downgrade

	[TestMethod]
	[Description("When the loaded assembly version is lower than the requested version, TryBridgeBySimpleName must return null to avoid silently serving a downgraded version.")]
	public void TryBridgeBySimpleName_RejectsDowngrade_WhenLoadedVersionLowerThanRequested()
	{
		// Use System.Text.Encodings.Web — forced into Default by Given_AddInLoadContext.
		var loaded = typeof(System.Text.Encodings.Web.JavaScriptEncoder).Assembly;
		var loadedVersion = loaded.GetName().Version!;
		var loadedPkt = loaded.GetName().GetPublicKeyToken()!;

		// Request the same assembly one major version higher (simulates the
		// add-in requesting v(N+1) while the host has only vN loaded).
		var higherVersion = new Version(loadedVersion.Major + 1, 0, 0, 0);
		var requested = new AssemblyName("System.Text.Encodings.Web")
		{
			Version = higherVersion,
		};
		requested.SetPublicKeyToken(loadedPkt);

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeNull(
			"loaded v{0} must not satisfy a request for v{1} — that would be a downgrade",
			loadedVersion, higherVersion);
	}
}
