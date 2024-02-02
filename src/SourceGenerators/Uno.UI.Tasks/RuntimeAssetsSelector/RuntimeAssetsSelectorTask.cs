#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public string UnoRuntimeIdentifier { get; set; } = "";

		/// <remarks>
		/// Note that this property is not set to [Required] because
		/// with netstandard2.0, it is not set and the default value is used instead.
		/// </remarks>
		public string TargetFrameworkVersion { get; set; } = "";

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? Assemblies { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? DebugSymbols { get; set; }

		public override bool Execute()
		{
			try
			{
				List<ITaskItem> assemblies = new();
				List<ITaskItem> debugSymbols = new();

				foreach (var package in UnoRuntimeEnabledPackage ?? Array.Empty<ITaskItem>())
				{
					var packageBasePath = package.GetMetadata("PackageBasePath");

					if (!Version.TryParse(TargetFrameworkVersion?.Substring(1), out var targetFrameworkVersion))
					{
						targetFrameworkVersion = new(2, 0);
					}

					List<string> searchPaths = new();

					if (targetFrameworkVersion >= new Version(8, 0))
					{
						searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "net8.0", UnoRuntimeIdentifier));
						searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "net8.0", UnoRuntimeIdentifier));
					}

					if (targetFrameworkVersion >= new Version(7, 0))
					{
						searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "net7.0", UnoRuntimeIdentifier));
						searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "net7.0", UnoRuntimeIdentifier));
					}

					if (targetFrameworkVersion >= new Version(2, 0))
					{
						searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "netstandard2.0", UnoRuntimeIdentifier));
						searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "netstandard2.0", UnoRuntimeIdentifier));
					}

					if (searchPaths.FirstOrDefault(Directory.Exists) is { } topMostDirectory)
					{
						foreach (var assembly in Directory.EnumerateFiles(topMostDirectory, "*.dll"))
						{
							assemblies.Add(new TaskItem(
								assembly,
								new Dictionary<string, string>
								{
									["NuGetPackageId"] = package.GetMetadata("Identity"),
									["PathInPackage"] = $"uno-runtime/{Path.GetFileName(Path.GetDirectoryName(topMostDirectory))}/{UnoRuntimeIdentifier}/{Path.GetFileName(assembly)}"
								}));
						}

						foreach (var debugSymbol in Directory.EnumerateFiles(topMostDirectory, "*.pdb"))
						{
							debugSymbols.Add(new TaskItem(
								debugSymbol,
								new Dictionary<string, string>
								{
									["NuGetPackageId"] = package.GetMetadata("Identity")
								}));
						}
					}
				}

				Assemblies = assemblies.ToArray();
				DebugSymbols = debugSymbols.ToArray();

				return true;
			}
			catch (Exception e)
			{
				// Require because the task is running out of process
				// and can't marshal non-CLR known exceptions.
				throw new(e.ToString());
			}
		}
	}
}
