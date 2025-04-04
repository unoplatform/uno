using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.Tests.Verifiers;

internal static partial class UnoAssemblyHelper
{
	public static PortableExecutableReference[] LoadAssemblies() =>
		LoadAssemblies(GetBinDirectory(
			[
				// On CI the test assemblies set must be first, as it contains all dependent assemblies
				"Uno.UI.Tests",
				"Uno.UI.Skia",
				"Uno.UI.Reference",
			],
			[TFMPrevious, TFMCurrent]
		));

	public static PortableExecutableReference[] LoadAndroidAssemblies() =>
		LoadAssemblies(GetBinDirectory(
			["Uno.UI.netcoremobile"],
			[$"{TFMPrevious}-android", $"{TFMCurrent}-android"]
		));

	private static string GetBinDirectory(string[] targets, string[] tfms)
	{
		var tfmSubPaths =
		(
			from tfm in tfms
			from target in targets
			select Path.Combine(target, CurrentConfiguration, tfm)
		).ToArray();

		var unoBasePath = Path.Combine(
			Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
			"..",
			"..",
			"..",
			"..",
			"..",
			"Uno.UI",
			"bin"
		);

		var directory = tfmSubPaths
			.Select(x => Path.Combine(unoBasePath, x))
			.FirstOrDefault(x => File.Exists(Path.Combine(x, "Uno.UI.dll")));
		if (directory is null)
		{
			throw new InvalidOperationException(string.Join("\n", (string[])[
				"Unable to find Uno.UI.dll in the expected locations.",
#if DEBUG
				// on ci, they are ensured by the ci script
				"note: If you are getting this error locally, make sure to build the Uno.UI project once for any of the target listed below",
#endif
				$"unoBasePath: {new Uri(unoBasePath).LocalPath}",
				$"tfmSubPaths:",
				..tfmSubPaths.Select(x => $"  - {x}"),
			]));
		}

		return directory;
	}

	private static PortableExecutableReference[] LoadAssemblies(string binDirectory) =>
		Directory.GetFiles(binDirectory, "*.dll")
			.Select(x => MetadataReference.CreateFromFile(x))
			.ToArray();
}

partial class UnoAssemblyHelper
{
	private const string CurrentConfiguration =
#if DEBUG
		"Debug";
#else
		"Release";
#endif
	private const string TFMPrevious = "net8.0";
	private const string TFMCurrent = "net9.0";
}
