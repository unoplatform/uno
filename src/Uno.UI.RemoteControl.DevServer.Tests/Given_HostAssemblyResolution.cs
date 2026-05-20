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
	[Description("EagerLoadFromDirectory must actually load an assembly from the supplied directory that was not previously present in Default.Assemblies (non-vacuous).")]
	public void EagerLoadFromDirectory_LoadsAssemblyNotPreviouslyInDefault()
	{
		var sourceDir = System.IO.Path.GetDirectoryName(typeof(HostAssemblyResolution).Assembly.Location)!;

		// Build the set of simple names currently in Default.Assemblies so we
		// can pick a candidate that is NOT already loaded — otherwise the
		// eager-load would be a no-op for our test target.
		var alreadyLoaded = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var asm in AssemblyLoadContext.Default.Assemblies)
		{
			if (!asm.IsDynamic && asm.GetName().Name is { } n)
			{
				alreadyLoaded.Add(n);
			}
		}

		string? pickedFile = null;
		foreach (var pattern in new[] { "System.*.dll", "Microsoft.Extensions.*.dll" })
		{
			foreach (var path in System.IO.Directory.EnumerateFiles(sourceDir, pattern))
			{
				var simple = System.IO.Path.GetFileNameWithoutExtension(path);
				if (!alreadyLoaded.Contains(simple))
				{
					pickedFile = path;
					break;
				}
			}

			if (pickedFile is not null)
			{
				break;
			}
		}

		if (pickedFile is null)
		{
			Assert.Inconclusive(
				"Could not find any System.*.dll or Microsoft.Extensions.*.dll under the test " +
				"directory that is not already loaded in AssemblyLoadContext.Default. " +
				"EagerLoadFromDirectory cannot be exercised on this runtime.");
			return;
		}

		// Stage the picked file into an isolated temp directory so the helper
		// has only one candidate to consider (keeps the assertion focused).
		var tempDir = System.IO.Path.Combine(
			System.IO.Path.GetTempPath(),
			"uno-eager-load-test-" + Guid.NewGuid().ToString("N"));
		System.IO.Directory.CreateDirectory(tempDir);
		try
		{
			var stagedPath = System.IO.Path.Combine(tempDir, System.IO.Path.GetFileName(pickedFile));
			System.IO.File.Copy(pickedFile, stagedPath);

			var pickedSimpleName = System.IO.Path.GetFileNameWithoutExtension(pickedFile);

			var loadedCount = HostAssemblyResolution.EagerLoadFromDirectory(tempDir);

			loadedCount.Should().BeGreaterThan(0,
				"the staged candidate '{0}' was not in Default.Assemblies and must be eager-loaded",
				pickedSimpleName);

			var nowPresent = AssemblyLoadContext.Default.Assemblies.Any(a =>
				!a.IsDynamic &&
				string.Equals(a.GetName().Name, pickedSimpleName, StringComparison.OrdinalIgnoreCase));

			nowPresent.Should().BeTrue(
				"'{0}' must appear in Default.Assemblies after EagerLoadFromDirectory",
				pickedSimpleName);
		}
		finally
		{
			try
			{
				System.IO.Directory.Delete(tempDir, recursive: true);
			}
			catch
			{
				/* best-effort cleanup */
			}
		}
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
			// We cannot introspect the Resolving delegate on this runtime.
			// Refuse to silently return 0 — that would let a regression in
			// Install()'s idempotency slip through unnoticed.
			Assert.Inconclusive(
				"Cannot reflect the Resolving handler field on AssemblyLoadContext " +
				"on this runtime; idempotency assertion is not verifiable here.");
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
