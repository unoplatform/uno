#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		public string NuGetPackageRoot { get; set; } = "";

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
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsOutput { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? DebugSymbols { get; set; }

		public override bool Execute()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(UnoRuntimeIdentifier))
				{
					// Single layer mode, Skia desktop or WebAssembly Browser + DOM.

					var (assemblies, _, debugSymbols) = EnumerateRuntimeFiles(UnoRuntimeIdentifier);

					Assemblies = assemblies.ToArray();
					DebugSymbols = debugSymbols.ToArray();
				}
				else
				{
					// Two layer mode: Skia + WebAssembly.
					string[] winrtAssemblies = ["uno", "uno.ui.dispatching", "uno.foundation"];
					var (winRTAssemblies, winRTResolvedCompileFileDefinitions, winRTDebugSymbols) = EnumerateRuntimeFiles(UnoWinRTRuntimeIdentifier, includeFiles: winrtAssemblies);
					var (uiAssemblies, uiResolvedCompileFileDefinitions, uiDebugSymbols) = EnumerateRuntimeFiles(UnoUIRuntimeIdentifier, excludeFiles: winrtAssemblies);

					Assemblies = winRTAssemblies.Concat(uiAssemblies).ToArray();
					DebugSymbols = winRTDebugSymbols.Concat(uiDebugSymbols).ToArray();
					ResolvedCompileFileDefinitionsOutput = winRTResolvedCompileFileDefinitions.Concat(uiResolvedCompileFileDefinitions).ToArray();
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

		private string? GetTargetFrameworkSearchPathFromLibDirectory(string lib, string targetFramework)
		{
			if (Directory.Exists(lib))
			{
				foreach (var dir in new DirectoryInfo(lib).GetDirectories())
				{
					if (dir.Name.StartsWith(targetFramework, StringComparison.Ordinal))
					{
						return dir.FullName;
					}
				}
			}

			return null;
		}

		private string? GetTargetFrameworkSearchPath(string packageBasePath, string targetFramework)
			=> GetTargetFrameworkSearchPathFromLibDirectory(Path.Combine(packageBasePath, "lib"), targetFramework)
				?? GetTargetFrameworkSearchPathFromLibDirectory(Path.Combine(packageBasePath, "..", "lib"), targetFramework);

		private void CollectReferencePaths(List<string> referencePaths, string tfm, string packageBasePath)
		{
			var lib1 = Path.Combine(packageBasePath, "lib", tfm);
			if (Directory.Exists(lib1))
			{
				referencePaths.AddRange(Directory.GetFiles(lib1, "*.dll"));
			}

			var lib2 = Path.Combine(packageBasePath, "..", "lib", tfm);
			if (Directory.Exists(lib2))
			{
				referencePaths.AddRange(Directory.GetFiles(lib2, "*.dll"));
			}
		}


		private (List<ITaskItem> assemblies, List<ITaskItem> resolvedCompileFileDefinitions, List<ITaskItem> debugSymbols) EnumerateRuntimeFiles(
			string unoRuntimeIdentifier
			, string[]? includeFiles = null
			, string[]? excludeFiles = null)
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
				List<string> referencePaths = new();

				// Even if Uno does not support net7.0 explicitly anymore, dependencies
				// may still be providing net7.0 runtime support files (e.g. SkiaSharp)
				for (int i = 9; i >= 7; i--)
				{
					var tfm = $"net{i.ToString(CultureInfo.InvariantCulture)}.0";

					if (targetFrameworkVersion >= new Version(i, 0))
					{
						searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", tfm, unoRuntimeIdentifier));
						searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", tfm, unoRuntimeIdentifier));
						if (unoRuntimeIdentifier is "android" or "ios" &&
							GetTargetFrameworkSearchPath(packageBasePath, $"{tfm}-{unoRuntimeIdentifier}") is { } tfmSearchPath)
						{
							searchPaths.Add(tfmSearchPath);
						}

						CollectReferencePaths(referencePaths, tfm, packageBasePath);
					}
				}

				searchPaths.Add(Path.Combine(packageBasePath, "uno-runtime", "netstandard2.0", unoRuntimeIdentifier));
				searchPaths.Add(Path.Combine(packageBasePath, "..", "uno-runtime", "netstandard2.0", unoRuntimeIdentifier));
				CollectReferencePaths(referencePaths, "netstandard2.0", packageBasePath);

				if (searchPaths.Where(d => Directory.Exists(d) && Directory.EnumerateFiles(d, "*.dll").Any()).FirstOrDefault() is { } topMostDirectory)
				{
					var packageIdentity = package.GetMetadata("Identity");
					var startingIndex = Math.Max(topMostDirectory.LastIndexOf("uno-runtime", StringComparison.Ordinal), topMostDirectory.LastIndexOf("lib", StringComparison.Ordinal));
					var pathInPackagePrefix = topMostDirectory.Substring(startingIndex).Replace('\\', '/');
					var assemblyFiles = Directory.EnumerateFiles(topMostDirectory, "*.dll");

					assemblyFiles = excludeFiles is not null
						? assemblyFiles.Where(f => !excludeFiles.Contains(Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
						: assemblyFiles;

					assemblyFiles = includeFiles is not null
						? assemblyFiles.Where(f => includeFiles.Contains(Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase))
						: assemblyFiles;

					foreach (var assembly in assemblyFiles)
					{
						var dllFileName = Path.GetFileName(assembly);
						assemblies.Add(new TaskItem(
							assembly,
							new Dictionary<string, string>
							{
								["NuGetPackageId"] = packageIdentity,
								["PathInPackage"] = $"{pathInPackagePrefix}/{dllFileName}"
							}));

						if (UnoUIRuntimeIdentifier == "skia")
						{
							string compileTimeDllFilePath;
							if (unoRuntimeIdentifier == UnoWinRTRuntimeIdentifier && UnoWinRTRuntimeIdentifier is "android" or "ios")
							{
								// In the context of Android Skia and iOS Skia, we want to pass the actual implementation dll to the compiler.
								// For example, Android Skia apps need to call Uno.Helpers.DrawableHelper class, so it has to be accessible at compile-time
								compileTimeDllFilePath = assembly;
							}
							else
							{
								compileTimeDllFilePath = referencePaths.First(p => Path.GetFileName(p) == dllFileName);
							}

							var normalizedCompileTimeDllFilePath = compileTimeDllFilePath.Replace('\\', '/');
							var existing = ResolvedCompileFileDefinitionsInput.First(item => item.GetMetadata("NuGetPackageId") == packageIdentity);
							resolvedCompileFileDefinitions.Add(new TaskItem(
								assembly,
								new Dictionary<string, string>
								{
									["HintPath"] = compileTimeDllFilePath,
									["NuGetPackageVersion"] = existing.GetMetadata("NuGetPackageVersion"),
									["Private"] = existing.GetMetadata("Private"),
									["ExternallyResolved"] = existing.GetMetadata("ExternallyResolved"),
									["NuGetPackageId"] = packageIdentity,
									["PathInPackage"] = normalizedCompileTimeDllFilePath.Substring(normalizedCompileTimeDllFilePath.IndexOf("lib/", StringComparison.Ordinal)),
									["NuGetSourceType"] = existing.GetMetadata("NuGetSourceType"),
								}));
						}
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
								["NuGetPackageId"] = packageIdentity
							}));
					}
				}
			}

			// For Android Skia and iOS Skia, we want to resolve netX.0 instead of netX.0-[android|ios] for non-RuntimeEnabled packages.
			// The idea here is that we loop over ResolvedCompileFileDefinitionsInput, look for dlls from NuGet package cache,
			// and then try to find the right dll.
			if (UnoUIRuntimeIdentifier == "skia" && UnoWinRTRuntimeIdentifier is "android" or "ios")
			{
				var nugetCacheRoot = NuGetPackageRoot.Replace('\\', '/');
				if (!nugetCacheRoot.EndsWith("/", StringComparison.Ordinal))
				{
					nugetCacheRoot += "/";
				}

				var runtimeEnabledPackages = UnoRuntimeEnabledPackage.Select(p => p.GetMetadata("Identity")).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
				if (ResolvedCompileFileDefinitionsInput is not null)
				{
					foreach (var compileFileDefinition in ResolvedCompileFileDefinitionsInput)
					{
						// identityNormalized is expected to be on the form:
						// <NuGetPackageRoot>/<PackageName>/<PackageVersion>/lib/<TargetFramework>/<AssemblyName>.dll
						var identityNormalized = compileFileDefinition.GetMetadata("Identity").Replace('\\', '/');

						// OrdinalIgnoreCase is not correct for a case-sensitive OS (e.g, Linux), but not a big deal.
						if (identityNormalized.StartsWith(nugetCacheRoot, StringComparison.Ordinal))
						{
							var relativePath = identityNormalized.Substring(nugetCacheRoot.Length);
							var split = relativePath.Split('/');
							if (split.Length == 5 && split[2] == "lib")
							{
								var packageName = split[0];
								if (!runtimeEnabledPackages.Contains(packageName))
								{
									var targetFramework = split[3];
									if (targetFramework.Contains("-android") ||
										targetFramework.Contains("-ios"))
									{
										var packageVersion = split[1];
										var adjustedTargetFramework = targetFramework.Substring(0, targetFramework.IndexOf('-'));
										var dllFileName = split[4];
										var adjustedPath = $"{nugetCacheRoot}{packageName}/{packageVersion}/lib/{adjustedTargetFramework}/{dllFileName}";
										if (File.Exists(adjustedPath))
										{
											assemblies.Add(new TaskItem(
												Path.GetFullPath(adjustedPath),
												new Dictionary<string, string>
												{
													["NuGetPackageId"] = packageName,
													["PathInPackage"] = $"lib/{adjustedTargetFramework}/{dllFileName}",
												}));

											resolvedCompileFileDefinitions.Add(new TaskItem(
												Path.GetFullPath(adjustedPath),
												new Dictionary<string, string>
												{
													["HintPath"] = Path.GetFullPath(adjustedPath),
													["NuGetPackageVersion"] = packageVersion,
													["Private"] = compileFileDefinition.GetMetadata("Private"),
													["ExternallyResolved"] = compileFileDefinition.GetMetadata("ExternallyResolved"),
													["NuGetPackageId"] = packageName,
													["PathInPackage"] = $"lib/{adjustedTargetFramework}/{dllFileName}",
													["NuGetSourceType"] = compileFileDefinition.GetMetadata("NuGetSourceType"),
												}));
										}
									}
								}
							}
						}
					}
				}
			}

			return (assemblies, resolvedCompileFileDefinitions, debugSymbols);
		}
	}
}
