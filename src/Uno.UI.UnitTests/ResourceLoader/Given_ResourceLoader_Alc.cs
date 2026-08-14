#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;
using _ResourceLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace Uno.UI.Tests.ResourceLoaderTests;

/// <summary>
/// A downstream host that loads previewed apps into their own collectible
/// <see cref="AssemblyLoadContext"/>s registers each app assembly via
/// <see cref="_ResourceLoader.AddLookupAssembly"/>. The process-lifetime lookup-assembly list
/// then keeps a strong reference to every app assembly for the process lifetime, pinning the
/// context after unload. <see cref="_ResourceLoader.ClearNonDefaultAlcAssemblies"/> (invoked from
/// the ALC cleanup hook) removes those non-default-ALC assemblies while keeping default-ALC ones.
/// </summary>
[TestClass]
public class Given_ResourceLoader_Alc
{
	[TestMethod]
	public void When_ClearNonDefaultAlcAssemblies_Then_Collectible_Removed_And_Default_Kept()
	{
		// A default-ALC assembly (this test assembly) plays the framework/host role; it must survive.
		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;

		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.collectible", isCollectible: true);
		try
		{
			// Loading into a collectible ALC yields a distinct Assembly instance whose load context
			// is the collectible ALC — the previewed-app-assembly stand-in.
			var collectibleAssembly = collectibleAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			Assert.AreSame(collectibleAlc, AssemblyLoadContext.GetLoadContext(collectibleAssembly), "Pre-condition: the loaded assembly must belong to the collectible ALC.");

			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			_ResourceLoader.AddLookupAssembly(collectibleAssembly);

			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(defaultAlcAssembly), "Pre-condition: the default-ALC assembly must be registered.");
			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(collectibleAssembly), "Pre-condition: the collectible-ALC assembly must be registered.");

			_ResourceLoader.ClearNonDefaultAlcAssemblies();

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(collectibleAssembly),
				"The sweep must drop the collectible-ALC lookup assembly; otherwise the static list pins the unloaded context.");
			Assert.IsTrue(
				_ResourceLoader.ContainsLookupAssembly(defaultAlcAssembly),
				"The sweep must keep default-ALC (framework/host) lookup assemblies.");
		}
		finally
		{
			collectibleAlc.Unload();
		}
	}

	[TestMethod]
	public void When_ClearAlcAssemblies_Scoped_Then_Other_Alc_Kept()
	{
		// Two live secondary apps: tearing one down must not destroy the other's registrations.
		// Removal is destructive (a dropped registration is never re-added), so the ALC-scoped
		// sweep — used when the dying ALC is identifiable — must only remove the dying context's
		// lookup assemblies.
		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;

		var dyingAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.dying", isCollectible: true);
		var siblingAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.sibling", isCollectible: true);
		try
		{
			var dyingAssembly = dyingAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			var siblingAssembly = siblingAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);

			_ResourceLoader.AddLookupAssembly(dyingAssembly);
			_ResourceLoader.AddLookupAssembly(siblingAssembly);

			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(dyingAssembly), "Pre-condition: the dying ALC's assembly must be registered.");
			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(siblingAssembly), "Pre-condition: the sibling ALC's assembly must be registered.");

			_ResourceLoader.ClearAlcAssemblies(dyingAlc);

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(dyingAssembly),
				"The scoped sweep must drop the dying ALC's lookup assembly; otherwise the static list pins the unloaded context.");
			Assert.IsTrue(
				_ResourceLoader.ContainsLookupAssembly(siblingAssembly),
				"The scoped sweep must keep a live sibling secondary app's lookup assembly — dropping it would break the sibling's resource lookups for the rest of the process lifetime.");
		}
		finally
		{
			// Remove the sibling registration so this test does not leak state into other tests.
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			dyingAlc.Unload();
			siblingAlc.Unload();
		}
	}

	[TestMethod]
	public void When_ClearNonDefaultAlcAssemblies_Then_Default_Resources_Still_Resolve()
	{
		// Regression guard: the sweep removes only the dying assemblies' registrations and parsed
		// markers — it must NOT disturb the surviving (default-ALC) assemblies' loaded resources
		// (an earlier destroy-and-rebuild implementation could leave every loader permanently
		// empty when a rebuild step failed). A default-ALC resource must still resolve after the
		// sweep. Uses the .upri resources embedded in this (default-ALC) test assembly.
		const string defaultLanguage = "en";
		const string uiTestResources = "Uno.UI.UnitTests/Resources";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;
		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.resolve", isCollectible: true);
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = defaultLanguage;
			_ResourceLoader.DefaultLanguage = defaultLanguage;

			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			Assert.AreEqual(
				"App70-en",
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the default-ALC resource resolves before the sweep.");

			var collectibleAssembly = collectibleAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			_ResourceLoader.AddLookupAssembly(collectibleAssembly);

			_ResourceLoader.ClearNonDefaultAlcAssemblies();

			Assert.AreEqual(
				"App70-en",
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"The sweep must rebuild the loaders from the remaining default-ALC assemblies; otherwise cleared dictionaries stay empty.");
		}
		finally
		{
			collectibleAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	[TestMethod]
	public void When_ClearAlcAssemblies_Scoped_Then_Collectible_Override_Removed_And_Host_Value_Restored()
	{
		// Reviewer scenario: a collectible previewed app overrides the SAME loader/culture/key as the
		// host. While the collectible assembly is registered its value must win; after its ALC is torn
		// down the key must resolve back to the host value — the collectible override must NOT outlive
		// its ALC (an earlier implementation dropped only the lookup assembly + parsed markers, leaving
		// the overriding value merged into loader._resources forever).
		//
		// Both ALCs load the same physical test assembly, so their embedded .upri are byte-identical
		// and cannot naturally diverge. The differing collectible value is therefore injected via
		// reflection at the exact merge-dictionary seam (loader._resources[culture][key]) that a
		// collectible .upri carrying its own ApplicationName would have written (last writer wins).
		// Everything else — the removal and the rebuild-from-survivors — exercises production code.
		const string defaultLanguage = "en";
		const string uiTestResources = "Uno.UI.UnitTests/Resources";
		const string overrideValue = "Collectible-Override";
		const string hostValue = "App70-en";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;
		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.override", isCollectible: true);
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = defaultLanguage;
			_ResourceLoader.DefaultLanguage = defaultLanguage;

			// Host registration establishes the baseline value and the loader context.
			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the host resource resolves before the collectible override.");

			// A collectible previewed app registers and overrides the same key with a different value.
			var collectibleAssembly = collectibleAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			_ResourceLoader.AddLookupAssembly(collectibleAssembly);
			OverrideMergedResource(uiTestResources, defaultLanguage, "ApplicationName", overrideValue);

			Assert.AreEqual(
				overrideValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"The collectible override must win while its assembly is registered.");

			// Tear the collectible app down via the ALC-scoped sweep.
			_ResourceLoader.ClearAlcAssemblies(collectibleAlc);

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(collectibleAssembly),
				"The scoped sweep must drop the collectible-ALC lookup assembly.");
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"After the collectible ALC is torn down, the key must resolve back to the host value — the override must not outlive its ALC.");
		}
		finally
		{
			// Drop any leftover non-default registration so this test does not leak state.
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			collectibleAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	[TestMethod]
	public void When_ClearAlcAssemblies_With_Malformed_Survivor_Then_Rebuild_Completes_And_Override_Removed()
	{
		// Reviewer scenario: RebuildLoaderResourcesFromSurvivors guards each survivor with a
		// data-format catch, but ProcessResourceFile used to surface malformed .upri content as
		// InvalidOperationException (bad magic / unsupported version) and a bare Exception (null
		// stream) — escaping the guard, aborting the rebuild BEFORE the apply phase, and leaving
		// the removed ALC's merged overrides observable. A malformed survivor must be
		// logged-and-skipped, never fatal to the sweep.
		const string defaultLanguage = "en";
		const string uiTestResources = "Uno.UI.UnitTests/Resources";
		const string overrideValue = "Collectible-Override-MalformedSurvivor";
		const string hostValue = "App70-en";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;
		var survivorAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.malformedSurvivor", isCollectible: true);
		var dyingAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.dyingWithSurvivor", isCollectible: true);
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = defaultLanguage;
			_ResourceLoader.DefaultLanguage = defaultLanguage;

			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the host resource resolves before the collectible override.");

			// A dying collectible app that overrides a host key (same seam as the override test above).
			var dyingAssembly = dyingAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			_ResourceLoader.AddLookupAssembly(dyingAssembly);
			OverrideMergedResource(uiTestResources, defaultLanguage, "ApplicationName", overrideValue);
			Assert.AreEqual(
				overrideValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the collectible override must win while its assembly is registered.");

			// A surviving lookup assembly whose .upri cannot be parsed. The AddLookupAssembly
			// rollback keeps a malformed assembly from lingering through the public path, so
			// inject it at the private list seam — the rebuild guard is defense-in-depth for
			// survivors whose .upri only fails at rebuild time (unreadable/truncated stream).
			var malformedSurvivor = LoadAssemblyWithMalformedUpri(survivorAlc, "Uno.UI.Tests.MalformedUpriSurvivor");
			InjectLookupAssembly(malformedSurvivor);

			// Tear the dying app down. This must NOT throw even though a survivor is malformed.
			_ResourceLoader.ClearAlcAssemblies(dyingAlc);

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(dyingAssembly),
				"The scoped sweep must drop the dying ALC's lookup assembly.");
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"The malformed survivor must be skipped (not fatal): the rebuild must still reach its apply phase so the dying app's override does not outlive its ALC.");
		}
		finally
		{
			// Drop the injected malformed survivor and any leftover non-default registrations.
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			survivorAlc.Unload();
			dyingAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	[TestMethod]
	public void When_AddLookupAssembly_Malformed_Then_Throws_And_Registration_Rolled_Back()
	{
		// AddLookupAssembly registers the assembly BEFORE parsing; without a rollback a malformed
		// .upri leaves the assembly among the lookup assemblies forever, so every later rebuild
		// (culture change, ALC sweep) re-hits the same parse failure. The registration must be
		// rolled back on failure while the exception still surfaces to the caller — as
		// InvalidDataException, the data-format type the per-assembly rebuild guard catches.
		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.malformedRollback", isCollectible: true);
		try
		{
			var malformed = LoadAssemblyWithMalformedUpri(collectibleAlc, "Uno.UI.Tests.MalformedUpriRollback");

			Assert.ThrowsExactly<InvalidDataException>(
				() => _ResourceLoader.AddLookupAssembly(malformed),
				"A malformed .upri must surface as InvalidDataException — the data-format exception the rebuild guard catches.");

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(malformed),
				"A failed registration must be rolled back; otherwise the malformed assembly lingers among the survivors and every later rebuild re-throws.");
		}
		finally
		{
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			collectibleAlc.Unload();
		}
	}

	[TestMethod]
	[Description(
		"A .upri truncated mid key/value pair, registered while the loader context is unchanged (the " +
		"full-reload branch), must leave no registration, no parsed marker and no partially merged " +
		"value observable — not even the loader instance the failed parse would have created.")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23959")]
	public void When_AddLookupAssembly_Truncated_Upri_Then_Throws_And_Partial_Value_Not_Observable()
	{
		// Issue scenario: a .upri with a VALID header that declares two key/value pairs, carries one
		// complete pair, then truncates before the second. ProcessResourceFile used to merge each
		// pair directly into the live loader as it read, so failing on the second pair left the FIRST
		// pair's value observable even after the AddLookupAssembly rollback removed the registration
		// and its parsed markers — an unregistered assembly's partially parsed resource value
		// survived. A failed AddLookupAssembly must be fully transactional.
		const string defaultLanguage = "en";
		const string uiTestResources = "Uno.UI.UnitTests/Resources";
		const string truncatedLoaderName = "Uno.UI.Tests.TruncatedUpri/Resources";
		const string hostValue = "App70-en";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;
		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.truncated", isCollectible: true);
		try
		{
			// Establish the loader context BEFORE the failing registration: with an unchanged
			// context no later GetString call reloads (and incidentally wipes) the loaders, so a
			// leaked partial value would remain observable for the rest of the process lifetime.
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = defaultLanguage;
			_ResourceLoader.DefaultLanguage = defaultLanguage;

			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the host resource resolves before the failing registration.");

			var truncated = LoadAssemblyWithUpri(
				collectibleAlc,
				"Uno.UI.Tests.TruncatedUpri",
				BuildTruncatedUpriPayload(truncatedLoaderName, defaultLanguage));

			Assert.ThrowsExactly<InvalidDataException>(
				() => _ResourceLoader.AddLookupAssembly(truncated),
				"A .upri truncated in the middle of its declared pairs must surface as InvalidDataException naming the file — the contract ProcessResourceFile documents for malformed content.");

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(truncated),
				"A failed registration must be rolled back.");
			Assert.IsFalse(
				_ResourceLoader.ContainsNamedLoader(truncatedLoaderName),
				"The failed parse must not even create the truncated .upri's loader — a created loader proves the parse wrote into the live state. Checked before GetForCurrentView below, which creates it on purpose.");
			Assert.AreEqual(
				string.Empty,
				_ResourceLoader.GetForCurrentView(truncatedLoaderName).GetString("TruncatedKey"),
				"The first pair of a truncated .upri must NOT remain observable after the failed registration — the rollback must cover partially merged values, not just the registration and markers.");
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Surviving registrations must still resolve after the failed registration is rolled back.");
		}
		finally
		{
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			collectibleAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	[TestMethod]
	[Description(
		"The same truncated .upri through AddLookupAssembly's OTHER branch — a changed loader context, " +
		"where only the new assembly is parsed — must leave the live loaders completely untouched: no " +
		"loader instance created and no value merged, pinning the single-assembly transactional parse.")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23959")]
	public void When_AddLookupAssembly_Truncated_Upri_With_Changed_Context_Then_Live_Loaders_Untouched()
	{
		// Same truncated .upri as above, but through AddLookupAssembly's OTHER branch: when the
		// loader context changed since it was last established, the new assembly is parsed on its
		// own (no full reload). That parse used to write each pair straight into the live loader
		// too; it must instead parse into temporaries so a failure leaves the live loaders
		// completely untouched. Observed at the merged-dictionary seam (no GetString) because a
		// later GetString reloads for the new context and would mask the leak.
		const string truncatedLoaderName = "Uno.UI.Tests.TruncatedUpriChangedContext/Resources";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.truncatedChangedContext", isCollectible: true);
		try
		{
			// Establish a French loader context...
			CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
			ApplicationLanguages.PrimaryLanguageOverride = "fr";
			_ResourceLoader.DefaultLanguage = "fr";

			// ...then flip the environment to English WITHOUT the ResourceLoader observing it
			// (neither setter below re-enters the loader), so the next AddLookupAssembly sees a
			// significantly changed context and takes the single-assembly parse branch.
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = "en";

			var truncated = LoadAssemblyWithUpri(
				collectibleAlc,
				"Uno.UI.Tests.TruncatedUpriChangedContext",
				BuildTruncatedUpriPayload(truncatedLoaderName, "en"));

			Assert.ThrowsExactly<InvalidDataException>(
				() => _ResourceLoader.AddLookupAssembly(truncated),
				"A .upri truncated in the middle of its declared pairs must surface as InvalidDataException naming the file — the contract ProcessResourceFile documents for malformed content.");

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(truncated),
				"A failed registration must be rolled back.");
			Assert.IsFalse(
				_ResourceLoader.ContainsNamedLoader(truncatedLoaderName),
				"The failed single-assembly parse must not even CREATE the truncated .upri's loader. Absence (not just emptiness) is what pins this branch: were branch selection to regress to the full reload, its rebuild recovery would empty the loader but the instance would still exist.");
			Assert.IsFalse(
				_ResourceLoader.TryGetMergedResourceForTests(truncatedLoaderName, "en", "TruncatedKey", out var leaked),
				$"The failed single-assembly parse must not leave any value in the live loaders (found '{leaked}').");
		}
		finally
		{
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			collectibleAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	/// <summary>
	/// Builds a <c>.upri</c> payload with a valid header (magic, version 3, loader name, culture)
	/// that declares TWO key/value pairs, carries one complete pair, then truncates — matching
	/// <c>ProcessResourceFile</c>'s binary layout so the parser fails on the second
	/// <see cref="BinaryReader.ReadString"/> AFTER the first pair parsed successfully.
	/// </summary>
	private static byte[] BuildTruncatedUpriPayload(string loaderName, string culture)
	{
		using var payload = new MemoryStream();
		using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
		{
			writer.Write("uno"u8); // magic
			writer.Write(3); // version
			writer.Write(loaderName);
			writer.Write(culture);
			writer.Write(2); // resourceCount: declares two pairs...
			writer.Write("TruncatedKey");
			writer.Write("TruncatedValue");
			// ...but the stream ends before the second pair.
		}

		return payload.ToArray();
	}

	/// <summary>
	/// Emits (in memory, via <see cref="ManagedPEBuilder"/>) a minimal assembly whose single
	/// manifest resource is a malformed <c>.upri</c> (payload does not start with the expected
	/// magic), and loads it into <paramref name="alc"/>. Lets the malformed-resource paths be
	/// exercised with a REAL <see cref="Assembly"/> — no fake seam in the product code.
	/// </summary>
	private static Assembly LoadAssemblyWithMalformedUpri(AssemblyLoadContext alc, string assemblyName)
		=> LoadAssemblyWithUpri(alc, assemblyName, "BAD-upri-payload"u8.ToArray()); // does not start with the "uno" magic

	/// <summary>
	/// Emits (in memory, via <see cref="ManagedPEBuilder"/>) a minimal assembly whose single
	/// manifest resource is a <c>.upri</c> with the given <paramref name="upriPayload"/>, and
	/// loads it into <paramref name="alc"/>. Lets the malformed/truncated-resource paths be
	/// exercised with a REAL <see cref="Assembly"/> — no fake seam in the product code.
	/// </summary>
	private static Assembly LoadAssemblyWithUpri(AssemblyLoadContext alc, string assemblyName, byte[] upriPayload)
	{
		var metadata = new MetadataBuilder();
		metadata.AddAssembly(
			metadata.GetOrAddString(assemblyName),
			new Version(1, 0, 0, 0),
			culture: default,
			publicKey: default,
			flags: default,
			hashAlgorithm: AssemblyHashAlgorithm.None);
		metadata.AddModule(
			generation: 0,
			metadata.GetOrAddString(assemblyName + ".dll"),
			metadata.GetOrAddGuid(Guid.NewGuid()),
			encId: default,
			encBaseId: default);

		// ECMA-335 II.24.2.4: each manifest resource is a 4-byte length prefix followed by the data.
		var resources = new BlobBuilder();
		resources.WriteInt32(upriPayload.Length);
		resources.WriteBytes(upriPayload);

		metadata.AddManifestResource(
			ManifestResourceAttributes.Public,
			metadata.GetOrAddString("Embedded.upri"),
			implementation: default,
			offset: 0);

		var peBuilder = new ManagedPEBuilder(
			PEHeaderBuilder.CreateLibraryHeader(),
			new MetadataRootBuilder(metadata),
			ilStream: new BlobBuilder(),
			managedResources: resources);
		var peBlob = new BlobBuilder();
		peBuilder.Serialize(peBlob);

		using var stream = new MemoryStream(peBlob.ToArray());
		return alc.LoadFromStream(stream);
	}

	/// <summary>
	/// Adds <paramref name="assembly"/> directly to the private
	/// <c>ResourceLoader._lookupAssemblies</c> list, bypassing <c>AddLookupAssembly</c>'s parse
	/// (and its rollback). Simulates a survivor whose .upri only becomes unreadable after
	/// registration — the scenario the per-assembly rebuild guard exists for.
	/// </summary>
	private static void InjectLookupAssembly(Assembly assembly)
	{
		var field = typeof(_ResourceLoader).GetField("_lookupAssemblies", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("ResourceLoader._lookupAssemblies field was not found.");
		((List<Assembly>)field.GetValue(null)!).Add(assembly);
	}

	/// <summary>
	/// Injects a merged resource value at the private per-loader dictionary seam
	/// (<c>ResourceLoader._resources[culture][key]</c>) — the same place
	/// <c>ProcessResourceFile</c> writes — to simulate a collectible .upri overriding an existing
	/// key with a different value. Needed because both ALCs load the same physical assembly, so a
	/// naturally-loaded collectible .upri cannot carry a divergent value.
	/// </summary>
	private static void OverrideMergedResource(string loaderName, string culture, string key, string value)
	{
		var loader = _ResourceLoader.GetForCurrentView(loaderName);
		var resourcesField = typeof(_ResourceLoader).GetField("_resources", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("ResourceLoader._resources field was not found.");
		var resources = (Dictionary<string, Dictionary<string, string>>)resourcesField.GetValue(loader)!;
		if (!resources.TryGetValue(culture, out var map))
		{
			resources[culture] = map = new Dictionary<string, string>();
		}

		map[key] = value;
	}
}
