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
		[
			..LoadAssemblies(GetBinDirectory(
				"Uno.UI",
				"Uno.UI.dll",
				[
					// On CI the test assemblies set must be first, as it contains all dependent assemblies
					"Uno.UI.Tests",
					"Uno.UI.Skia",
					"Uno.UI.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.UWP",
				"Uno.dll",
				[
					// On CI the test assemblies set must be first, as it contains all dependent assemblies
					"Uno.Tests",
					"Uno.Skia",
					"Uno.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.Foundation",
				"Uno.Foundation.dll",
				[
					// On CI the test assemblies set must be first, as it contains all dependent assemblies
					"Uno.Foundation.Tests",
					"Uno.Foundation.Skia",
					"Uno.Foundation.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.UI.Composition",
				"Uno.UI.Composition.dll",
				[
					// On CI the test assemblies set must be first, as it contains all dependent assemblies
					"Uno.UI.Composition.Tests",
					"Uno.UI.Composition.Skia",
					"Uno.UI.Composition.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
		];

	public static PortableExecutableReference[] LoadAndroidAssemblies() =>
		LoadAssemblies(GetBinDirectory(
			"Uno.UI",
			"Uno.UI.dll",
			["Uno.UI.netcoremobile"],
			[$"{TFMPrevious}-android", $"{TFMCurrent}-android"]
		));

	private static string GetBinDirectory(string baseName, string assemblyName, string[] targets, string[] tfms)
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
			baseName,
			"bin"
		);

		var directory = tfmSubPaths
			.Select(x => Path.Combine(unoBasePath, x))
			.FirstOrDefault(x => File.Exists(Path.Combine(x, assemblyName)));
		if (directory is null)
		{
			throw new InvalidOperationException(string.Join("\n", (string[])[
				$"Unable to find {assemblyName} in the expected locations.",
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
