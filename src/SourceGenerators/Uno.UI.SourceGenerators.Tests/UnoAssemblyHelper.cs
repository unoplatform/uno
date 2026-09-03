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
					"Uno.UI",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.WinRT",
				"Uno.WinRT.dll",
				[
					"Uno.WinRT.Skia",
					"Uno.WinRT.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.Foundation",
				"Uno.Foundation.dll",
				[
					"Uno.Foundation.Skia",
					"Uno.Foundation.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.UI.Composition",
				"Uno.UI.Composition.dll",
				[
					"Uno.UI.Composition",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Uno.UI.Dispatching",
				"Uno.UI.Dispatching.dll",
				[
					"Uno.UI.Dispatching.Skia",
					"Uno.UI.Dispatching.Reference",
				],
				[TFMPrevious, TFMCurrent]
			)),
		];

	/// <summary>
	/// Replacement for the Uno.UI.Toolkit assembly shipped by pre-7.0 Uno.WinUI packages, whose
	/// types now live in the Uno.UI.* namespaces. Loaded only where that package assembly was
	/// referenced, so tests that never saw the Toolkit keep their existing reference set.
	/// </summary>
	public static PortableExecutableReference[] LoadExtrasAssemblies() =>
		LoadAssemblies(GetBinDirectory(
			"Uno.UI.Extras",
			"Uno.UI.Extras.dll",
			[
				"Uno.UI.Extras.Skia",
				"Uno.UI.Extras.Reference",
			],
			[TFMPrevious, TFMCurrent]
		));

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
			from target in targets
			from tfm in tfms
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
	private const string TFMPrevious = "net10.0";
	private const string TFMCurrent = "net11.0";
}
