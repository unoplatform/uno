using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class ManifestAddInResolverTests
{
	private static ILogger<ManifestAddInResolver> _logger = null!;
	private string _tempDir = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext _)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.AddDebug();
			builder.SetMinimumLevel(LogLevel.Trace);
		});
		_logger = loggerFactory.CreateLogger<ManifestAddInResolver>();
	}

	[TestInitialize]
	public void TestInitialize()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "ManifestAddInResolverTests_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, recursive: true);
			}
		}
		catch
		{
			// Best effort cleanup
		}
	}

	[TestMethod]
	[Description("No devserver-addin.json on disk → null (fall through to .targets)")]
	public void WhenNoManifestFile_ReturnsNull()
	{
		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().BeNull();
	}

	[TestMethod]
	[Description("Valid v1 manifest with one add-in resolves the DLL path, package name, version, and source")]
	public void WhenValidManifestV1_ResolvesAddIn()
	{
		var dllPath = CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().HaveCount(1);
		result.AddIns[0].EntryPointDll.Should().Be(dllPath);
		result.AddIns[0].PackageName.Should().Be("Uno.Test");
		result.AddIns[0].PackageVersion.Should().Be("1.0.0");
		result.AddIns[0].DiscoverySource.Should().Be("manifest");
	}

	[TestMethod]
	[Description("Manifest version 2 is unknown → null (fall through for forward compat)")]
	public void WhenManifestVersionGreaterThan1_ReturnsNull()
	{
		WriteManifest("""
		{
			"version": 2,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().BeNull();
	}

	[TestMethod]
	[Description("Extra JSON fields ($schema, futureField) are silently ignored for forward compatibility")]
	public void WhenUnknownFields_IgnoresAndResolves()
	{
		var dllPath = CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"$schema": "https://schemas.platform.uno/devserver/addin-manifest-v1.json",
			"version": 1,
			"futureField": true,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll", "futureData": 42 }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().HaveCount(1);
	}

	[TestMethod]
	[Description("Add-in entry without entryPoint is skipped, other entries still resolve")]
	public void WhenEntryPointMissing_SkipsWithWarning()
	{
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "minHostVersion": "6.0.0" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("entryPoint references a DLL that does not exist on disk → skipped")]
	public void WhenEntryPointDllNotOnDisk_SkipsWithWarning()
	{
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/NotThere.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Add-in requires host >= 99.0 but current is 6.0 → skipped")]
	public void WhenMinHostVersionNotSatisfied_SkipsWithWarning()
	{
		CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll", "minHostVersion": "99.0.0" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger, hostVersion: "6.0.0");

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("P2 regression: when hostVersion is null (default DI), add-ins with minHostVersion should still be filtered")]
	public void WhenHostVersionNull_IncompatibleAddInShouldStillBeFiltered()
	{
		CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll", "minHostVersion": "99.0.0" }
			]
		}
		""");

		// Bug P2: when DI registers ManifestAddInResolver without hostVersion,
		// the minHostVersion check is bypassed entirely.
		// Safe default: null hostVersion should mean "unknown/oldest" → filter gated add-ins.
		var resolver = new ManifestAddInResolver(_logger); // no hostVersion

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty("add-in with minHostVersion should be filtered even when hostVersion is unknown");
	}

	[TestMethod]
	[Description("No minHostVersion in manifest entry → no version gate, add-in resolves")]
	public void WhenMinHostVersionOmitted_Resolves()
	{
		var dllPath = CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger, hostVersion: "6.0.0");

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().HaveCount(1);
	}

	[TestMethod]
	[Description("Broken JSON → empty result (stops chain for this package, no fall through)")]
	public void WhenMalformedJson_ReturnsEmptyWithWarning()
	{
		WriteManifest("{ this is not valid json }}}");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Zero-byte manifest file → empty result (parse error stops chain)")]
	public void WhenEmptyFile_ReturnsEmptyWithWarning()
	{
		WriteManifest("");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Binary garbage in manifest file → empty result (parse error stops chain)")]
	public void WhenBinaryGarbage_ReturnsEmptyWithWarning()
	{
		File.WriteAllBytes(Path.Combine(_tempDir, "devserver-addin.json"), [0xFF, 0xFE, 0x00, 0x01, 0x80]);

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Valid JSON but wrong shape (array instead of object) → empty result")]
	public void WhenJsonArrayInsteadOfObject_ReturnsEmptyWithWarning()
	{
		WriteManifest("[1, 2, 3]");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Missing 'version' field defaults to 0 (≤ 1), so manifest is consumed, not fallen through")]
	public void WhenVersionMissing_TreatsAsVersion0_DoesNotFallThrough()
	{
		CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		// version defaults to 0 (not > 1), so does not fall through
		result.Should().NotBeNull();
		result!.AddIns.Should().HaveCount(1);
	}

	[TestMethod]
	[Description("Far-future version (42) → null (fall through to .targets for forward compat)")]
	public void WhenVersionIs42_ReturnsNull()
	{
		WriteManifest("""
		{
			"version": 42,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().BeNull("version > 1 should fall through to .targets discovery");
	}

	[TestMethod]
	[Description("Manifest with two add-in entries resolves both")]
	public void WhenMultipleAddIns_ResolvesAll()
	{
		var dllA = CreateDll("tools/devserver/A.dll");
		var dllB = CreateDll("tools/devserver/B.dll");
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/A.dll" },
				{ "entryPoint": "tools/devserver/B.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().HaveCount(2);
		result.AddIns.Select(a => a.EntryPointDll).Should().BeEquivalentTo([dllA, dllB]);
	}

	[TestMethod]
	[Description("Forward slashes in entryPoint are normalized to OS-native separators")]
	public void WhenForwardSlashPaths_NormalizesForCurrentOs()
	{
		var dllPath = CreateDll("tools/devserver/Foo.dll");
		WriteManifest("""
		{
			"version": 1,
			"addins": [
				{ "entryPoint": "tools/devserver/Foo.dll" }
			]
		}
		""");

		var resolver = new ManifestAddInResolver(_logger);

		var result = resolver.TryResolveFromManifest(_tempDir, "Uno.Test", "1.0.0");

		result.Should().NotBeNull();
		result!.AddIns.Should().HaveCount(1);
		// Path should use OS-native separators
		result.AddIns[0].EntryPointDll.Should().Be(dllPath);
		result.AddIns[0].EntryPointDll.Should().NotContain("/");
	}

	#region Helpers

	private void WriteManifest(string content)
	{
		File.WriteAllText(Path.Combine(_tempDir, "devserver-addin.json"), content);
	}

	private string CreateDll(string relativePath)
	{
		var fullPath = Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
		File.WriteAllBytes(fullPath, [0]);
		return Path.GetFullPath(fullPath);
	}

	#endregion
}
