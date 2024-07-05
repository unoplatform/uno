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

		public string UnoRuntimeIdentifier { get; set; } = "";

		public string UnoUIRuntimeIdentifier { get; set; } = "";

		public string UnoWinRTRuntimeIdentifier { get; set; } = "";

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
				if (!string.IsNullOrWhiteSpace(UnoRuntimeIdentifier))
				{
					// Single layer mode, Skia desktop or WebAssembly Browser + DOM.

					var (assemblies, debugSymbols) = EnumerateRuntimeFiles(UnoRuntimeIdentifier);

					Assemblies = assemblies;
					DebugSymbols = debugSymbols;
				}
				else
				{
					// Two layer mode: Skia + WebAssembly.
					string[] winrtAssemblies = ["uno", "uno.ui.dispatching", "uno.foundation"];
					var (winRTAssemblies, winRTDebugSymbols) = EnumerateRuntimeFiles(UnoWinRTRuntimeIdentifier, includeFiles: winrtAssemblies);
					var (uiAssemblies, uiDebugSymbols) = EnumerateRuntimeFiles(UnoUIRuntimeIdentifier, excludeFiles: winrtAssemblies);

					Assemblies = winRTAssemblies.Concat(uiAssemblies).ToArray();
					DebugSymbols = winRTDebugSymbols.Concat(uiDebugSymbols).ToArray();
				}

				return true;
			}
			catch (Exception e)
			{
				// Require because the task is running out of process
				// and can't marshal non-CLR known exceptions.
				throw new Exception(e.ToString());
			}
		}

		private (ITaskItem[] assemblies, ITaskItem[] debugSymbols) EnumerateRuntimeFiles(
			string unoRuntimeIdentifier
			, string[]? includeFiles = null
			, string[]? excludeFiles = null)
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

				if (targetFrameworkVersion >= new Version(9, 0))
				{
					searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "net9.0", unoRuntimeIdentifier));
					searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "net9.0", unoRuntimeIdentifier));
					if (unoRuntimeIdentifier == "android")
					{
						searchPaths.Add(Path.Combine(packageBasePath, "lib", "net9.0-android30.0"));
					}
					else if (unoRuntimeIdentifier == "ios")
					{
						searchPaths.Add(Path.Combine(packageBasePath, "lib", "net9.0-ios17.2"));
					}
				}

				if (targetFrameworkVersion >= new Version(8, 0))
				{
					searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "net8.0", unoRuntimeIdentifier));
					searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "net8.0", unoRuntimeIdentifier));
					searchPaths.Add(packageBasePath);
					if (unoRuntimeIdentifier == "android")
					{
						searchPaths.Add(Path.Combine(packageBasePath, "lib", "net8.0-android30.0"));
					}
					else if (unoRuntimeIdentifier == "ios")
					{
						searchPaths.Add(Path.Combine(packageBasePath, "lib", "net8.0-ios17.0"));
					}
				}

				if (targetFrameworkVersion >= new Version(7, 0))
				{
					// Even if Uno does not support nte7.0 explicitly anymore, dependencies
					// may still be providing net7.0 runtime support files (e.g. SkiaSharp)
					searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "net7.0", UnoRuntimeIdentifier));
					searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "net7.0", UnoRuntimeIdentifier));
				}

				if (targetFrameworkVersion >= new Version(2, 0))
				{
					searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "netstandard2.0", unoRuntimeIdentifier));
					searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "netstandard2.0", unoRuntimeIdentifier));
				}

				if (searchPaths.Where(d => Directory.Exists(d) && Directory.EnumerateFiles(d, "*.dll").Any()).FirstOrDefault() is { } topMostDirectory)
				{
					var assemblyFiles = Directory.EnumerateFiles(topMostDirectory, "*.dll");

					assemblyFiles = excludeFiles is not null
						? assemblyFiles.Where(f => !excludeFiles.Contains(Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
						: assemblyFiles;

					assemblyFiles = includeFiles is not null
						? assemblyFiles.Where(f => includeFiles.Contains(Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
						: assemblyFiles;

					foreach (var assembly in assemblyFiles)
					{
						assemblies.Add(new TaskItem(
							assembly,
							new Dictionary<string, string>
							{
								["NuGetPackageId"] = package.GetMetadata("Identity"),
								["PathInPackage"] = $"uno-runtime/{Path.GetFileName(Path.GetDirectoryName(topMostDirectory))}/{unoRuntimeIdentifier}/{Path.GetFileName(assembly)}"
							}));
					}

					var symbolFiles = Directory.EnumerateFiles(topMostDirectory, "*.pdb");

					symbolFiles = excludeFiles is not null
						? symbolFiles.Where(f => !excludeFiles.Contains(Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
						: symbolFiles;

					symbolFiles = includeFiles is not null
						? symbolFiles.Where(f => includeFiles.Contains(Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
						: symbolFiles;

					foreach (var debugSymbol in symbolFiles)
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

			return (assemblies.ToArray(), debugSymbols.ToArray());
		}
	}
}
