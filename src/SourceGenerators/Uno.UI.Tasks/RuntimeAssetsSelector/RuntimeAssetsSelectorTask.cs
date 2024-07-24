#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Transactions;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Uno.UI.Tasks.RuntimeAssetsSelector
{
	/// <summary>
	/// A task used to merge linker definition files and embed the result in an assembly
	/// </summary>
	public class RuntimeAssetsSelectorTask_v0 : Microsoft.Build.Utilities.Task
	{
		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? UnoRuntimeEnabledPackage { get; set; }

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsInput { get; set; }

		[Required]
		public string UnoRuntimeIdentifier { get; set; } = "";

		/// <remarks>
		/// Note that this property is not set to [Required] because
		/// with netstandard2.0, it is not set and the default value is used instead.
		/// </remarks>
		public string TargetFrameworkVersion { get; set; } = "";

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? Assemblies { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsOutput { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? DebugSymbols { get; set; }

		public override bool Execute()
		{
			try
			{
				List<ITaskItem> assemblies = new();
				List<ITaskItem> resolvedCompileFileDefinitions = new();
				List<ITaskItem> debugSymbols = new();

				foreach (var package in UnoRuntimeEnabledPackage ?? Array.Empty<ITaskItem>())
				{
					var packageBasePath = package.GetMetadata("PackageBasePath");

					if (!Version.TryParse(TargetFrameworkVersion?.Substring(1), out var targetFrameworkVersion))
					{
						targetFrameworkVersion = new(2, 0);
					}

					List<string> searchPaths = new();

					// Even if Uno does not support nte7.0 explicitly anymore, dependencies
					// may still be providing net7.0 runtime support files (e.g. SkiaSharp)
					for (int i = 9; i >= 7; i--)
					{
						var versionAsString = i.ToString(CultureInfo.InvariantCulture);
						if (targetFrameworkVersion >= new Version(i, 0))
						{
							searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", $"net{versionAsString}.0", UnoRuntimeIdentifier));
							searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", $"net{versionAsString}.0", UnoRuntimeIdentifier));
						}
					}

					searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "netstandard2.0", UnoRuntimeIdentifier));
					searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "netstandard2.0", UnoRuntimeIdentifier));

					if (searchPaths.FirstOrDefault(Directory.Exists) is { } topMostDirectory)
					{
						var packageIdentity = package.GetMetadata("Identity");
						var pathInPackagePrefix = topMostDirectory.Substring(topMostDirectory.LastIndexOf("uno-runtime", StringComparison.Ordinal)).Replace('\\', '/');
						foreach (var assembly in Directory.EnumerateFiles(topMostDirectory, "*.dll"))
						{
							assemblies.Add(new TaskItem(
								assembly,
								new Dictionary<string, string>
								{
									["NuGetPackageId"] = packageIdentity,
									["PathInPackage"] = $"{pathInPackagePrefix}/{Path.GetFileName(assembly)}",
								}));

							var existing = ResolvedCompileFileDefinitionsInput.First(item => item.GetMetadata("NuGetPackageId") == packageIdentity);

							resolvedCompileFileDefinitions.Add(new TaskItem(
								assembly,
								new Dictionary<string, string>
								{
									["HintPath"] = assembly,
									["NuGetPackageVersion"] = existing.GetMetadata("NuGetPackageVersion"),
									["Private"] = existing.GetMetadata("Private"),
									["ExternallyResolved"] = existing.GetMetadata("ExternallyResolved"),
									["NuGetPackageId"] = packageIdentity,
									["PathInPackage"] = $"{pathInPackagePrefix}/{Path.GetFileName(assembly)}",
									["NuGetSourceType"] = existing.GetMetadata("NuGetSourceType"),
								}));
						}

						foreach (var debugSymbol in Directory.EnumerateFiles(topMostDirectory, "*.pdb"))
						{
							debugSymbols.Add(new TaskItem(
								debugSymbol,
								new Dictionary<string, string>
								{
									["NuGetPackageId"] = packageIdentity,
								}));
						}
					}
				}

				Assemblies = assemblies.ToArray();
				DebugSymbols = debugSymbols.ToArray();
				ResolvedCompileFileDefinitionsOutput = resolvedCompileFileDefinitions.ToArray();
				return true;
			}
			catch (Exception e)
			{
				// Require because the task is running out of process
				// and can't marshal non-CLR known exceptions.
				throw new Exception(e.ToString());
			}
		}
	}
}
