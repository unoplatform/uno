using System.Reflection;
using System.Runtime.Loader;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Unit tests for <see cref="HostAssemblyResolution.TryBridgeBySimpleName"/>.
/// </summary>
[TestClass]
public class Given_HostAssemblyResolution
{
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
