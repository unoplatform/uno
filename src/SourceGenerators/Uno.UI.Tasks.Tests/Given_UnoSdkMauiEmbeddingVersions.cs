#nullable enable

using System.IO.Compression;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_UnoSdkMauiEmbeddingVersions
{
	[TestMethod]
	[DataRow("net10.0-desktop", false)]
	[DataRow("net10.0-browserwasm", false)]
	[DataRow("net10.0-desktop", true)]
	[DataRow("net10.0-browserwasm", true)]
	public void When_Library_Requires_A_Newer_Maui_Patch_Then_Restore_Uses_It(string targetFramework, bool centralVersions)
		=> Restore(targetFramework, centralVersions, libraryVersion: "10.0.110", expectedVersion: "10.0.110");

	[TestMethod]
	[DataRow("10.0.101")]
	[DataRow("10.0.110")]
	public void When_No_Library_Raises_Maui_Then_Embedding_Supplies_The_Minimum(string embeddingMinimum)
		=> Restore("net10.0-desktop", centralVersions: false, expectedVersion: embeddingMinimum, embeddingMinimum: embeddingMinimum);

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_MauiVersion_Is_Explicit_Then_Restore_Honors_It(bool lateOverride)
		=> Restore("net10.0-desktop", centralVersions: true, mauiVersion: "10.0.110", lateOverride: lateOverride, expectedVersion: "10.0.110");

	[TestMethod]
	public void When_An_Explicit_Pin_Conflicts_With_The_Library_Then_Downgrade_Remains_An_Error()
		=> Restore("net10.0-desktop", centralVersions: true, mauiVersion: "10.0.101", libraryVersion: "10.0.110", expectedVersion: "10.0.101", expectedDowngrade: true);

	[TestMethod]
	[DataRow("MauiNativeEmbedding")]
	[DataRow("MauiEmbedding;MauiNativeEmbedding")]
	public void When_Native_Embedding_Is_Selected_Then_Its_Controls_Reference_Is_Retained(string features)
	{
		WithDirectory(directory =>
		{
			var selection = Evaluate(directory, "net10.0-desktop", "10.0.110", lateOverride: false, features);
			selection.Controls.Should().BeTrue();
			selection.MauiVersion.Should().Be("10.0.110");
		});
	}

	private static void Restore(string targetFramework, bool centralVersions, string expectedVersion, string? libraryVersion = null,
		string? mauiVersion = null, bool lateOverride = false, string embeddingMinimum = "10.0.101", bool expectedDowngrade = false)
	{
		WithDirectory(directory =>
		{
			var selection = Evaluate(directory, targetFramework, mauiVersion, lateOverride);
			var feed = Directory.CreateDirectory(Path.Join(directory, "feed")).FullName;
			WritePackage(feed, "Microsoft.Maui.Controls", "10.0.101");
			WritePackage(feed, "Microsoft.Maui.Controls", "10.0.110");
			WritePackage(feed, "Uno.Maui.Embedding", "1.0.0", embeddingMinimum);
			new XDocument(new XElement("configuration",
				new XElement("packageSources", new XElement("clear"), new XElement("add", new XAttribute("key", "fixture"), new XAttribute("value", feed))),
				new XElement("fallbackPackageFolders", new XElement("clear"))))
				.Save(Path.Join(directory, "NuGet.Config"));

			var framework = new FrameworkName(AppContext.TargetFrameworkName!).Version;
			var tfm = $"net{framework.Major}.{framework.Minor}";
			var app = Directory.CreateDirectory(Path.Join(directory, "App")).FullName;
			var references = new XElement("ItemGroup", Reference("Uno.Maui.Embedding", "1.0.0", centralVersions));
			if (selection.Controls)
			{
				selection.MauiVersion.Should().NotBeNullOrEmpty("a direct Controls reference needs an explicit version");
				references.Add(Reference("Microsoft.Maui.Controls", selection.MauiVersion, centralVersions));
			}
			if (libraryVersion is not null)
			{
				var library = Directory.CreateDirectory(Path.Join(directory, "Library")).FullName;
				WriteProject(library, tfm, centralVersions, new XElement("ItemGroup", Reference("Microsoft.Maui.Controls", libraryVersion, centralVersions)));
				WriteCentralVersions(library, ("Microsoft.Maui.Controls", libraryVersion));
				references.Add(new XElement("ProjectReference", new XAttribute("Include", Path.Join(library, "Library.csproj"))));
			}

			WriteProject(app, tfm, centralVersions, references);
			WriteCentralVersions(app, ("Uno.Maui.Embedding", "1.0.0"), ("Microsoft.Maui.Controls", selection.MauiVersion));
			var result = DotnetMSBuild.Run(RepositoryPaths.Root, [Path.Join(app, "App.csproj"), "-t:Restore", "-p:RestoreForce=true"]);
			if (expectedDowngrade)
			{
				result.ExitCode.Should().NotBe(0);
				result.Output.Should().Contain("NU1605");
				return;
			}

			result.ExitCode.Should().Be(0, result.Output);
			using var assets = JsonDocument.Parse(File.ReadAllText(Path.Join(app, "obj", "project.assets.json")));
			var libraries = assets.RootElement.GetProperty("libraries");
			libraries.TryGetProperty($"Microsoft.Maui.Controls/{expectedVersion}", out _).Should().BeTrue();
			if (libraryVersion is not null)
			{
				libraries.TryGetProperty("Library/1.0.0", out _).Should().BeTrue("the project reference must participate in restore");
			}
		});
	}

	private static (bool Controls, string MauiVersion) Evaluate(string directory, string targetFramework, string? mauiVersion,
		bool lateOverride, string features = "MauiEmbedding")
	{
		XElement Version() => new("PropertyGroup", new XElement("MauiVersion", mauiVersion));
		var project = new XElement("Project",
			new XElement("PropertyGroup", new XElement("TargetFramework", targetFramework), new XElement("OutputType", "Exe"),
				new XElement("UnoSingleProject", "true"), new XElement("UnoFeatures", features)));
		if (!lateOverride && mauiVersion is not null)
		{
			project.Add(Version());
		}
		project.Add(new XElement("Import", new XAttribute("Project", RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets"))));
		if (lateOverride && mauiVersion is not null)
		{
			project.Add(Version());
		}
		// Run the real selection hooks without loading the packaged SDK task assembly.
		project.Add(new XElement("Target", new XAttribute("Name", "UnoImplicitPackages")));
		var path = Path.Join(directory, "Selection.proj");
		new XDocument(project).Save(path);
		var result = DotnetMSBuild.Run(RepositoryPaths.Root,
			[path, "-t:UnoImplicitPackages", "-getProperty:MauiVersion", "-getItem:_UnoProjectSystemPackageReference"]);
		result.ExitCode.Should().Be(0, result.Output);
		using var json = JsonDocument.Parse(result.StandardOutput);
		var controls = json.RootElement.GetProperty("Items").GetProperty("_UnoProjectSystemPackageReference").EnumerateArray()
			.Any(item => item.GetProperty("Identity").GetString() == "Microsoft.Maui.Controls");
		return (controls, json.RootElement.GetProperty("Properties").GetProperty("MauiVersion").GetString()!);
	}

	private static XElement Reference(string id, string? version, bool centralVersions)
	{
		var reference = new XElement("PackageReference", new XAttribute("Include", id));
		if (!centralVersions)
		{
			reference.Add(new XAttribute("Version", version!));
		}
		return reference;
	}

	private static void WriteProject(string directory, string targetFramework, bool centralVersions, XElement references)
		=> new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
			new XElement("PropertyGroup", new XElement("TargetFramework", targetFramework),
				new XElement("ManagePackageVersionsCentrally", centralVersions), new XElement("NuGetAudit", "false"),
				new XElement("RestorePackagesPath", Path.Join(directory, "packages")), new XElement("WarningsAsErrors", "NU1605")), references))
			.Save(Path.Join(directory, Path.GetFileName(directory) + ".csproj"));

	private static void WriteCentralVersions(string directory, params (string Id, string? Version)[] packages)
		=> new XDocument(new XElement("Project", new XElement("ItemGroup", packages.Where(package => !string.IsNullOrEmpty(package.Version))
			.Select(package => new XElement("PackageVersion", new XAttribute("Include", package.Id), new XAttribute("Version", package.Version!))))))
			.Save(Path.Join(directory, "Directory.Packages.props"));

	private static void WritePackage(string feed, string id, string version, string? controlsMinimum = null)
	{
		using var archive = ZipFile.Open(Path.Join(feed, $"{id}.{version}.nupkg"), ZipArchiveMode.Create);
		var metadata = new XElement("metadata", new XElement("id", id), new XElement("version", version),
			new XElement("authors", "Uno SDK test fixture"), new XElement("description", "Offline dependency resolution fixture"));
		if (controlsMinimum is not null)
		{
			metadata.Add(new XElement("dependencies", new XElement("dependency", new XAttribute("id", "Microsoft.Maui.Controls"),
				new XAttribute("version", $"[{controlsMinimum},)"))));
		}
		using (var stream = archive.CreateEntry(id + ".nuspec").Open())
		{
			new XDocument(new XElement("package", metadata)).Save(stream);
		}
		archive.CreateEntry("lib/netstandard2.0/_._");
	}

	private static void WithDirectory(Action<string> test)
	{
		var directory = Directory.CreateTempSubdirectory("uno-sdk-maui-versions-").FullName;
		try
		{
			test(directory);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}
