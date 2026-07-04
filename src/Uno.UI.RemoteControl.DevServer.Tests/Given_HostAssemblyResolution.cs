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

	// ------------------------------------------------------------------ asymmetric PKT match

	[TestMethod]
	[Description("Signed request (non-empty PublicKeyToken) whose token does not match the loaded assembly's must return null. Identity validation, not an allow-list.")]
	public void TryBridgeBySimpleName_RefusesBridge_WhenSignedRequestPktDoesNotMatchLoaded()
	{
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
	[Description("Signed request whose PublicKeyToken matches the loaded assembly's must return the loaded assembly — the normal signed-framework bridge path (e.g. Kiota → System.Text.Encodings.Web).")]
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

	[TestMethod]
	[Description("Unsigned request must skip the PKT check entirely: cross-add-in unsigned contracts are bridged by name only. This is asymmetric on purpose — a request without an identity claim cannot demand identity matching.")]
	public void TryBridgeBySimpleName_Bridges_WhenUnsignedRequestMatchesSignedLoaded()
	{
		var hostAssembly = typeof(System.Text.Json.JsonDocument).Assembly;

		// Unsigned request — no PKT set.
		var requested = new AssemblyName("System.Text.Json");

		var result = HostAssemblyResolution.TryBridgeBySimpleName(
			AssemblyLoadContext.Default, requested);

		result.Should().BeSameAs(hostAssembly,
			"an unsigned request must bridge to a same-named loaded assembly regardless of the loaded PKT");
	}

	// ------------------------------------------------------------------ version downgrade

	[TestMethod]
	[Description("When the loaded assembly version is lower than the requested version, TryBridgeBySimpleName must return null to avoid silently serving a downgraded version.")]
	public void TryBridgeBySimpleName_RejectsDowngrade_WhenLoadedVersionLowerThanRequested()
	{
		// Touch JavaScriptEncoder to force the assembly into Default.Assemblies.
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

	// ------------------------------------------------------------------ on-demand: runtime

	[TestMethod]
	[Description("When nothing with the requested simple name is loaded yet but the runtime/TPA carries it, Resolve must load it on demand — the load-order-independent path (e.g. Kiota → System.Text.Encodings.Web before anything touched it).")]
	public void Resolve_LoadsFrameworkAssemblyOnDemand_WhenNotYetLoaded()
	{
		var frameworkDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;

		var alreadyLoaded = LoadedSimpleNames();

		// Pick a shared-framework assembly that is on the TPA but not yet loaded
		// in this process, so the bridge misses and the on-demand path must serve it.
		string? pickedFile = System.IO.Directory.EnumerateFiles(frameworkDir, "System.*.dll")
			.FirstOrDefault(path =>
			{
				if (alreadyLoaded.Contains(System.IO.Path.GetFileNameWithoutExtension(path)))
				{
					return false;
				}

				try
				{
					// Skip resource/native files that are not managed assemblies, and
					// candidates whose version would fail the lower-version request below.
					return AssemblyName.GetAssemblyName(path).Version >= new Version(1, 0, 0, 0);
				}
				catch
				{
					return false;
				}
			});

		if (pickedFile is null)
		{
			Assert.Inconclusive(
				"Every System.*.dll of the shared framework is already loaded in this process; " +
				"the on-demand runtime path cannot be exercised here.");
			return;
		}

		var pickedName = AssemblyName.GetAssemblyName(pickedFile);

		// Mirror a compiled AssemblyRef from an older package: same identity, lower version.
		var requested = new AssemblyName(pickedName.Name!)
		{
			Version = new Version(1, 0, 0, 0),
		};
		requested.SetPublicKeyToken(pickedName.GetPublicKeyToken());

		var result = HostAssemblyResolution.Resolve(AssemblyLoadContext.Default, requested);

		result.Should().NotBeNull(
			"'{0}' is available from the runtime and must be loaded on demand", pickedName.Name);
		result!.GetName().Name.Should().Be(pickedName.Name);
	}

	// ------------------------------------------------------------------ on-demand: probing directory

	[TestMethod]
	[Description("When the assembly is neither loaded nor on the TPA but exists in a registered probing directory (add-in folder), Resolve must load it from there.")]
	public void Resolve_LoadsFromRegisteredProbingDirectory_WhenAssemblyNotOnTpa()
	{
		// The staged fixture output is deliberately kept out of this test project's
		// deps.json (ReferenceOutputAssembly=false), so its assemblies are NOT on the
		// TPA — only a probing-directory hit can resolve them in this process.
		var fixturesRoot = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures");
		if (!System.IO.Directory.Exists(fixturesRoot))
		{
			Assert.Inconclusive("Fixture staging directory not found; run a full project build first.");
			return;
		}

		var alreadyLoaded = LoadedSimpleNames();

		// Restrict to fixture-authored assemblies: NuGet payloads staged next to them
		// (Microsoft.Extensions.*, …) may also exist on this process's TPA, and the
		// on-demand runtime pass would legitimately serve those from there instead.
		string? pickedFile = System.IO.Directory
			.EnumerateFiles(fixturesRoot, "*.dll", System.IO.SearchOption.AllDirectories)
			.Where(path => System.IO.Path.GetFileName(path) is { } f
				&& (f.StartsWith("AddInWith", StringComparison.Ordinal)
					|| f.StartsWith("Uno.Licensing.TestContracts", StringComparison.Ordinal)))
			.FirstOrDefault(path => !alreadyLoaded.Contains(System.IO.Path.GetFileNameWithoutExtension(path)));

		if (pickedFile is null)
		{
			Assert.Inconclusive(
				"Every fixture assembly is already loaded in this process; " +
				"the probing-directory path cannot be exercised here.");
			return;
		}

		var pickedName = AssemblyName.GetAssemblyName(pickedFile);
		HostAssemblyResolution.RegisterProbingDirectory(System.IO.Path.GetDirectoryName(pickedFile)!);

		var result = HostAssemblyResolution.Resolve(
			AssemblyLoadContext.Default, new AssemblyName(pickedName.Name!));

		result.Should().NotBeNull(
			"'{0}' exists in a registered probing directory and must be loaded from there", pickedName.Name);
		result!.Location.Should().Be(pickedFile,
			"the assembly is not on the TPA, so only the probing directory can have served it");
	}

	// ------------------------------------------------------------------ on-demand: guards preserved

	[TestMethod]
	[Description("The on-demand path must preserve the no-downgrade guard: a request for a version higher than anything available must return null (and must not recurse infinitely through the Resolving handler).")]
	public void Resolve_StillRejectsDowngrade_WhenRequestedVersionHigherThanAvailable()
	{
		var loaded = typeof(System.Text.Encodings.Web.JavaScriptEncoder).Assembly;
		var loadedVersion = loaded.GetName().Version!;

		var requested = new AssemblyName("System.Text.Encodings.Web")
		{
			Version = new Version(loadedVersion.Major + 1, 0, 0, 0),
		};

		var result = HostAssemblyResolution.Resolve(AssemblyLoadContext.Default, requested);

		result.Should().BeNull(
			"neither the bridge nor the on-demand path may serve v{0} for a v{1} request",
			loadedVersion, requested.Version);
	}

	private static HashSet<string> LoadedSimpleNames()
	{
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var asm in AssemblyLoadContext.Default.Assemblies)
		{
			if (!asm.IsDynamic && asm.GetName().Name is { } n)
			{
				names.Add(n);
			}
		}

		return names;
	}
}
