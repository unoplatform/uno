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

	// ------------------------------------------------------------------ pattern constants

	[TestMethod]
	[Description("DefaultEagerLoadPatterns must include the System.* and Microsoft.Extensions.* globs that the host has always eager-loaded.")]
	public void DefaultEagerLoadPatterns_ContainsExpectedSystemAndExtensionsGlobs()
	{
		HostAssemblyResolution.DefaultEagerLoadPatterns.Should().Contain("System.*.dll",
			"System.* assemblies must be eager-loaded to bridge cross-major OOB package refs");
		HostAssemblyResolution.DefaultEagerLoadPatterns.Should().Contain("Microsoft.Extensions.*.dll",
			"Microsoft.Extensions.* assemblies must be eager-loaded to bridge Logging/Configuration refs");
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
	[Description("When the default ALC has an assembly with the requested simple name and the request carries no PKT, TryBridgeBySimpleName must return that instance — the unsigned-request path used by cross-add-in contract assemblies.")]
	public void TryBridgeBySimpleName_ReturnsLoadedAssembly_WhenUnsignedRequestMatchesByName()
	{
		// Force the assembly into Default.Assemblies.
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		// Unsigned request: no PKT set. This matches how an unsigned add-in
		// contract assembly arrives at the bridge.
		var requested = new AssemblyName("System.Text.Json");

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeSameAs(hostAssembly,
			"System.Text.Json is loaded in the default ALC and the simple name matches");
	}

	[TestMethod]
	[Description("When the requested AssemblyName carries a PublicKeyToken, TryBridgeBySimpleName must require the loaded assembly's PKT to match — otherwise a same-named assembly with a different (or absent) PKT could silently substitute the wrong identity.")]
	public void TryBridgeBySimpleName_RefusesBridge_WhenSignedRequestPktDoesNotMatchLoaded()
	{
		// Force the strong-named host assembly into Default.Assemblies.
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;
		var realPkt = hostAssembly.GetName().GetPublicKeyToken()!;
		realPkt.Length.Should().BeGreaterThan(0, "the bridged framework assembly is expected to be strong-named");

		// Build a fake PKT distinct from the real one (flip the last byte).
		var fakePkt = (byte[])realPkt.Clone();
		fakePkt[^1] = (byte)(fakePkt[^1] ^ 0xFF);

		var requested = new AssemblyName("System.Text.Json");
		requested.SetPublicKeyToken(fakePkt);

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeNull(
			"the loaded assembly's PKT differs from the signed request's PKT — bridging would be an identity swap");
	}

	[TestMethod]
	[Description("When the requested AssemblyName carries a PublicKeyToken matching the loaded assembly's PKT, TryBridgeBySimpleName must return the loaded instance — the normal signed-framework bridge path (e.g. Kiota → System.Text.Encodings.Web).")]
	public void TryBridgeBySimpleName_Bridges_WhenSignedRequestPktMatchesLoaded()
	{
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;
		var realPkt = hostAssembly.GetName().GetPublicKeyToken()!;

		var requested = new AssemblyName("System.Text.Json");
		requested.SetPublicKeyToken(realPkt);

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeSameAs(hostAssembly,
			"the signed request's PKT matches the loaded assembly — bridge must serve it");
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

		var requested = new AssemblyName("System.Text.Json");
		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeSameAs(hostAssembly,
			"the bridge must return the real assembly, not a dynamic one");
		result!.IsDynamic.Should().BeFalse("returned assembly must never be dynamic");
	}

	// ------------------------------------------------------------------ version downgrade

	[TestMethod]
	[Description("When the loaded assembly version is lower than the requested version, TryBridgeBySimpleName must return null to avoid silently serving a downgraded version.")]
	public void TryBridgeBySimpleName_RejectsDowngrade_WhenLoadedVersionLowerThanRequested()
	{
		// Use System.Text.Encodings.Web — forced into Default by Given_AddInLoadContext.
		var loaded = typeof(System.Text.Encodings.Web.JavaScriptEncoder).Assembly;
		var loadedVersion = loaded.GetName().Version!;

		// Request the same assembly one major version higher (simulates the
		// add-in requesting v(N+1) while the host has only vN loaded).
		var higherVersion = new Version(loadedVersion.Major + 1, 0, 0, 0);
		var requested = new AssemblyName("System.Text.Encodings.Web")
		{
			Version = higherVersion,
		};

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeNull(
			"loaded v{0} must not satisfy a request for v{1} — that would be a downgrade",
			loadedVersion, higherVersion);
	}
}
