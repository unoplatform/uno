using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tests.Utils;

/// <summary>
/// Tests for <see cref="RoslynExtensions.TryGetTargetFramework"/> and the internal
/// <c>TryParseRefPackPath</c> helper.
/// </summary>
[TestClass]
public sealed class Given_TryGetTargetFramework
{
	// ────────────────────────────────────────────────────────────────────────
	// TryParseRefPackPath — pure path parser, no Roslyn workspace needed
	// ────────────────────────────────────────────────────────────────────────

	[TestMethod]
	public void TryParseRefPackPath_NetCoreAppRef_BasePackNoPlatform()
	{
		var path = ToOsPath("/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.4/ref/net10.0/System.Runtime.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.IsNull(platform);
		Assert.IsNull(platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_NugetCachedNetCoreAppRef_LowercasedSegments()
	{
		// NuGet cache typically lowercases pack-name segments.
		var path = ToOsPath("/home/user/.nuget/packages/microsoft.netcore.app.ref/10.0.4/ref/net10.0/System.Runtime.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.IsNull(platform);
		Assert.IsNull(platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_AspNetCoreAppRef_BasePackRecognised()
	{
		var path = ToOsPath("/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.4/ref/net10.0/Microsoft.AspNetCore.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out _);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.IsNull(platform);
	}

	[TestMethod]
	public void TryParseRefPackPath_WindowsDesktopAppRef_BasePackRecognised()
	{
		var path = ToOsPath("/usr/share/dotnet/packs/Microsoft.WindowsDesktop.App.Ref/10.0.4/ref/net10.0/PresentationCore.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out _);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.IsNull(platform);
	}

	[TestMethod]
	public void TryParseRefPackPath_IosRefPack_ParsesPlatformAndVersion()
	{
		var path = ToOsPath(@"C:\Program Files\dotnet\packs\Microsoft.iOS.Ref.net10.0_26.0\26.0.11017\ref\net10.0\Microsoft.iOS.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("ios", platform);
		Assert.AreEqual("26.0", platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_TvOsRefPack_ParsesPlatformAndVersion()
	{
		var path = ToOsPath(@"C:\Program Files\dotnet\packs\Microsoft.tvOS.Ref.net10.0_18.5\18.5.42\ref\net10.0\Microsoft.tvOS.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("tvos", platform);
		Assert.AreEqual("18.5", platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_MacCatalystRefPack_ParsesPlatformAndVersion()
	{
		var path = ToOsPath(@"C:\Program Files\dotnet\packs\Microsoft.MacCatalyst.Ref.net10.0_18.5\18.5.42\ref\net10.0\Microsoft.MacCatalyst.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("maccatalyst", platform);
		Assert.AreEqual("18.5", platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_AndroidRefPack_ParsesPlatformAndVersion()
	{
		var path = ToOsPath(@"C:\Program Files\dotnet\packs\Microsoft.Android.Ref.net10.0_36.0\36.0.42\ref\net10.0\Microsoft.Android.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("android", platform);
		Assert.AreEqual("36.0", platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_MacOsRefPack_ParsesPlatformAndVersion()
	{
		var path = ToOsPath(@"C:\Program Files\dotnet\packs\Microsoft.macOS.Ref.net10.0_15.0\15.0.42\ref\net10.0\Microsoft.macOS.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("macos", platform);
		Assert.AreEqual("15.0", platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_WindowsSdkRefPack_ParsesPlatform()
	{
		var path = ToOsPath("/home/user/.nuget/packages/Microsoft.Windows.SDK.NET.Ref/10.0.19041.68/ref/net10.0/Microsoft.Windows.SDK.NET.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("windows", platform);
		Assert.IsNull(platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_PlatformPackWithoutVersionSuffix_ReturnsNullPlatformVersion()
	{
		// Older pack-name format — no `_<platform-version>` suffix.
		var path = ToOsPath("/usr/share/dotnet/packs/Microsoft.iOS.Ref/26.0.11017/ref/net10.0/Microsoft.iOS.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out var platformVersion);

		Assert.IsTrue(ok);
		Assert.AreEqual("net10.0", fw);
		Assert.AreEqual("ios", platform);
		Assert.IsNull(platformVersion);
	}

	[TestMethod]
	public void TryParseRefPackPath_NonRefPackPath_ReturnsFalse()
	{
		// Generic NuGet package path, not a ref pack.
		var path = ToOsPath("/home/user/.nuget/packages/humanizer/2.14.1/lib/net8.0/Humanizer.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out var platform, out _);

		Assert.IsFalse(ok);
		Assert.IsNull(fw);
		Assert.IsNull(platform);
	}

	[TestMethod]
	public void TryParseRefPackPath_PathTooShort_ReturnsFalse()
	{
		var path = ToOsPath("/short/path.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out _, out _);

		Assert.IsFalse(ok);
		Assert.IsNull(fw);
	}

	[TestMethod]
	public void TryParseRefPackPath_NotRefSegment_ReturnsFalse()
	{
		// The 3rd-from-last segment is not "ref" — for example "lib".
		var path = ToOsPath("/home/user/.nuget/packages/microsoft.netcore.app.ref/10.0.4/lib/net10.0/System.Runtime.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out _, out _);

		Assert.IsFalse(ok);
		Assert.IsNull(fw);
	}

	[TestMethod]
	public void TryParseRefPackPath_TfmSegmentNotNetXY_ReturnsFalse()
	{
		// The TFM segment doesn't start with "net<digit>" — netstandard2.0 falls outside our accepted shape.
		var path = ToOsPath("/home/user/.nuget/packages/some.refpack/1.0.0/ref/netstandard2.0/Some.dll");

		var ok = RoslynExtensions.TryParseRefPackPath(path, out var fw, out _, out _);

		Assert.IsFalse(ok);
		Assert.IsNull(fw);
	}

	// ────────────────────────────────────────────────────────────────────────
	// TryGetTargetFramework — full extension on Project
	// ────────────────────────────────────────────────────────────────────────

	[TestMethod]
	public void TryGetTargetFramework_IosRefPackAlone_ResolvesIos26()
	{
		// Source #1 alone — iOS ref pack present, no preprocessor symbols needed.
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.iOS.Ref.net10.0_26.0", "26.0.11017", "net10.0", "Microsoft.iOS.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-ios26.0", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_AndroidRefPack_ResolvesAndroid36()
	{
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.Android.Ref.net10.0_36.0", "36.0.42", "net10.0", "Microsoft.Android.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-android36.0", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_MacCatalystRefPack_ResolvesMacCatalyst()
	{
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.MacCatalyst.Ref.net10.0_18.5", "18.5.42", "net10.0", "Microsoft.MacCatalyst.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-maccatalyst18.5", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_TvosRefPack_ResolvesTvos()
	{
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.tvOS.Ref.net10.0_18.5", "18.5.42", "net10.0", "Microsoft.tvOS.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-tvos18.5", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_RefPackWins_OverPreprocessorSymbol()
	{
		// Source #1 is authoritative. iOS ref pack present + __WASM__ symbol — iOS wins.
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.NETCore.App.Ref", "10.0.4", "net10.0", "System.Runtime.dll"),
				FakeRefPackPath("Microsoft.iOS.Ref.net10.0_26.0", "26.0.11017", "net10.0", "Microsoft.iOS.dll"),
			],
			preprocessorSymbols: ["__WASM__"]);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-ios26.0", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_NetCoreAppRefOnly_WasmSymbol_ResolvesBrowserWasm()
	{
		// Source #2 tiebreaker: only base ref pack, __WASM__ in symbols.
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.NETCore.App.Ref", "10.0.4", "net10.0", "System.Runtime.dll"),
			],
			preprocessorSymbols: ["__WASM__"]);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-browserwasm", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_NetCoreAppRefOnly_DesktopSymbol_ResolvesDesktop()
	{
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.NETCore.App.Ref", "10.0.4", "net10.0", "System.Runtime.dll"),
			],
			preprocessorSymbols: ["__DESKTOP__"]);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-desktop", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_NetCoreAppRefOnly_NoSymbol_ResolvesPlainTfm()
	{
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.NETCore.App.Ref", "10.0.4", "net10.0", "System.Runtime.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_AspNetCoreAppRef_TriggersTiebreaker()
	{
		// AspNetCore.App.Ref is also a base pack — should fall through to source #2.
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.AspNetCore.App.Ref", "10.0.4", "net10.0", "Microsoft.AspNetCore.dll"),
			],
			preprocessorSymbols: ["__DESKTOP__"]);

		Assert.IsTrue(project.TryGetTargetFramework(out var tfm));
		Assert.AreEqual("net10.0-desktop", tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_NoRefPack_ReturnsFalse()
	{
		var project = CreateProject(refPaths: [], preprocessorSymbols: []);

		Assert.IsFalse(project.TryGetTargetFramework(out var tfm));
		Assert.IsNull(tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_OnlyUnrelatedNugetReferences_ReturnsFalse()
	{
		// Project has metadata references but none are recognised ref packs.
		var project = CreateProject(
			refPaths:
			[
				ToOsPath("/home/user/.nuget/packages/humanizer/2.14.1/lib/net8.0/Humanizer.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsFalse(project.TryGetTargetFramework(out var tfm));
		Assert.IsNull(tfm);
	}

	[TestMethod]
	public void TryGetTargetFramework_DivergingFrameworkVersions_ReturnsFalse()
	{
		// Two ref-pack references with different framework-version segments → invariant violation.
		var project = CreateProject(
			refPaths:
			[
				FakeRefPackPath("Microsoft.NETCore.App.Ref", "10.0.4", "net10.0", "System.Runtime.dll"),
				FakeRefPackPath("Microsoft.NETCore.App.Ref", "9.0.0", "net9.0", "System.Runtime.dll"),
			],
			preprocessorSymbols: []);

		Assert.IsFalse(project.TryGetTargetFramework(out var tfm));
		Assert.IsNull(tfm);
	}

	// ────────────────────────────────────────────────────────────────────────
	// Test fixture — fabricates Roslyn Project instances with controlled refs
	// ────────────────────────────────────────────────────────────────────────

	internal static Project CreateProject(IReadOnlyList<string> refPaths, IReadOnlyList<string> preprocessorSymbols)
	{
		var workspace = new AdhocWorkspace();
		var projectId = ProjectId.CreateNewId();

		var metadataReferences = refPaths.Select(StubMetadataReference.Create).ToImmutableArray<MetadataReference>();

		var parseOptions = preprocessorSymbols.Count > 0
			? new CSharpParseOptions().WithPreprocessorSymbols(preprocessorSymbols)
			: new CSharpParseOptions();

		var projectInfo = ProjectInfo.Create(
			projectId,
			VersionStamp.Default,
			name: "Test",
			assemblyName: "Test",
			language: LanguageNames.CSharp,
			parseOptions: parseOptions,
			metadataReferences: metadataReferences);

		var solution = workspace.AddProject(projectInfo);
		return solution;
	}

	/// <summary>
	/// Builds a path that mimics the SDK-installed ref-pack layout
	/// (<c>&lt;dotnet-root&gt;/packs/&lt;PackName&gt;/&lt;PackVersion&gt;/ref/&lt;tfm&gt;/&lt;asm&gt;.dll</c>),
	/// using OS-appropriate separators so the test is portable across platforms.
	/// </summary>
	internal static string FakeRefPackPath(string packName, string packVersion, string tfm, string assemblyFile)
		=> Path.Combine("/", "fake", "dotnet", "packs", packName, packVersion, "ref", tfm, assemblyFile);

	internal static string ToOsPath(string path)
		=> path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

	/// <summary>
	/// Roslyn does not allow constructing a <see cref="PortableExecutableReference"/> without a
	/// readable PE image on disk. For unit tests that exercise path-only logic we subclass
	/// <see cref="PortableExecutableReference"/> with a stub that exposes a controllable
	/// <c>FilePath</c> and a no-op metadata getter — the consumer
	/// (<see cref="RoslynExtensions.TryGetTargetFramework"/>) only inspects <c>FilePath</c>.
	/// </summary>
	internal sealed class StubMetadataReference : PortableExecutableReference
	{
		private StubMetadataReference(string filePath)
			: base(MetadataReferenceProperties.Assembly, filePath)
		{
		}

		public static StubMetadataReference Create(string filePath) => new(filePath);

		protected override DocumentationProvider CreateDocumentationProvider() => DocumentationProvider.Default;

		protected override Metadata GetMetadataImpl()
			=> throw new NotSupportedException("Stub reference: metadata not available.");

		protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
			=> new StubMetadataReference(FilePath!);
	}
}
