using System.Xml.Linq;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The packed uno-runtime folders are the UnoRuntimeVariant values lowercased. The nuspecs spell them out by hand,
/// and a mismatch only surfaces as UNOB0023 in a consumer's build, so it is pinned here instead.
/// </summary>
[TestClass]
public class Given_UnoRuntimeLayout
{
	private static readonly string[] Variants = ["generic", "wasm"];

	public static IEnumerable<object[]> RuntimeEnabledNuspecs =>
		Directory.EnumerateFiles(RepositoryPaths.Get("build", "nuget"), "*.nuspec")
			.Where(path => RuntimeTargets(path).Any())
			.Select(path => new object[] { Path.GetFileName(path) });

	[TestMethod]
	public void When_Enumerating_Nuspecs_Then_Uno_WinRT_And_Uno_Foundation_Ship_Runtime_Folders()
	{
		RuntimeEnabledNuspecs.Select(row => (string)row[0])
			.Should().Contain(["Uno.WinRT.nuspec", "Uno.Foundation.nuspec"]);
	}

	[TestMethod]
	[DynamicData(nameof(RuntimeEnabledNuspecs))]
	public void When_Packing_Then_Every_Runtime_Folder_Is_A_Variant(string nuspec)
	{
		foreach (var (_, variant) in RuntimeTargets(RepositoryPaths.Get("build", "nuget", nuspec)))
		{
			Variants.Should().Contain(variant, $"{nuspec} packs to uno-runtime/<tfm>/{variant}");
		}
	}

	[TestMethod]
	[DynamicData(nameof(RuntimeEnabledNuspecs))]
	public void When_Packing_Then_Wasm_Ships_The_Same_Files_As_Generic(string nuspec)
	{
		// The generic listing is the authoritative asset list (spec 059 §8): a file only in wasm is never deployed.
		var files = XDocument.Load(RepositoryPaths.Get("build", "nuget", nuspec))
			.Descendants().Where(e => e.Name.LocalName == "file")
			.Select(e => (Source: (string?)e.Attribute("src") ?? "", Target: Split((string?)e.Attribute("target") ?? "")))
			.Where(f => f.Target is not null)
			.Select(f => (f.Source, Target: f.Target!.Value))
			.GroupBy(f => (f.Target.Tfm, f.Target.Variant), f => Path.GetFileName(f.Source.Replace('\\', '/')))
			.ToDictionary(g => g.Key, g => g.OrderBy(n => n, StringComparer.Ordinal).ToArray());

		foreach (var ((tfm, variant), names) in files.Where(f => f.Key.Variant == "wasm"))
		{
			files.Should().ContainKey((tfm, "generic"));
			files[(tfm, "generic")].Should().Equal(names, $"{nuspec} must ship the same files for {tfm} generic and wasm");
		}
	}

	private static IEnumerable<(string Tfm, string Variant)> RuntimeTargets(string nuspec)
		=> XDocument.Load(nuspec)
			.Descendants().Where(e => e.Name.LocalName == "file")
			.Select(e => Split((string?)e.Attribute("target") ?? ""))
			.Where(t => t is not null)
			.Select(t => t!.Value);

	private static (string Tfm, string Variant)? Split(string target)
	{
		var parts = target.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		return parts is ["uno-runtime", var tfm, var variant, ..] ? (tfm, variant) : null;
	}
}
