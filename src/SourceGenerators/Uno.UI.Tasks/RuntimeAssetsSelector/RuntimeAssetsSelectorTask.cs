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
		private const int LatestSupportedDotnetVersion = 10; // **MUST BE** net10.0 aligned (Keep this comment to ease upgrade to later versions of .NET)

		// Even if Uno does not support net7.0 explicitly anymore, dependencies
		// may still be providing net7.0 runtime support files (e.g. SkiaSharp)
		private const int EarliestSupportedDotnetVersion = 7;

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? UnoRuntimeEnabledPackage { get; set; }

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsInput { get; set; }

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? RuntimeCopyLocalItemsInput { get; set; }

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
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsToRemove { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsToAdd { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? RuntimeCopyLocalItemsToRemove { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? RuntimeCopyLocalItemsToAdd { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? DebugSymbols { get; set; }

		public override bool Execute()
		{
			try
			{
				if (UnoRuntimeIdentifier == "reference")
				{
					return true;
				}

				// We have two types of packages
				// 1. Packages that are runtime-enabled (e.g, contains uno-runtime - which is signified by <UnoRuntimeEnabledPackage Include="PackageName" PackageBasePath="..." /> in package props/targets)
				// 2. Packages that are not runtime-enabled.
				//
				// We also have two modes:
				// 1. Single layer mode (non-null UnoRuntimeIdentifier, expected to be either skia or webassembly).
				// 2. Two layer mode (UnoUIRuntimeIdentifier being skia, while UnoWinRTRuntimeIdentifier expected to be webassembly, android, or ios).
				//
				//
				// For runtime-enabled packages:
				//     Single layer mode:
				//         - Don't alter compile file definitions, we still keep reference to be passed to the compiler.
				//         - Adjust RuntimeCopyLocalItems such that reference binaries are removed and binaries from uno-runtime/[skia|wasm] are added.
				//
				//     Two layer mode:
				//         - Adjust RuntimeCopyLocalItems such that WinRT assemblies are added from uno-runtime/webassembly or from lib/netX.0-[android|ios] and other assemblies from uno-runtime/skia.
				//         - For Wasm Skia, don't alter compile file definitions, we still keep reference to be passed to the compiler.
				//         - For Android Skia and iOS Skia, modify compile file definitions such that reference is passed (except for WinRT assemblies, we pass the actual implementation).
				//
				//
				// For non-runtime-enabled packages:
				//     Single layer mode:
				//         - We do nothing
				//     Two layer mode:
				//         - For Wasm Skia, we do nothing.
				//         - For Android Skia, iOS, Mac Catalyst, or tvOS Skia:
				//             - Adjust both RuntimeCopyLocalItems and ResolvedCompileFileDefinitions such that netX.0 binaries are used instead of netX.0-android, -ios, -maccatalyst, or -tvos
				//                 - maybe we should prefer netX.0-desktop over netX.0, if exists.
				//                 - we should only do that for dlls that reference Uno.UI.dll (use Mono.Cecil to detect that)

				var runtimeCopyLocalItemsToAdd = new List<ITaskItem>();
				var runtimeCopyLocalItemsToRemove = new List<ITaskItem>();
				var compileFileDefinitionsToAdd = new List<ITaskItem>();
				var compileFileDefinitionsToRemove = new List<ITaskItem>();
				var pdbFilesToAdd = new List<ITaskItem>();

				var isSingleLayer = !string.IsNullOrWhiteSpace(UnoRuntimeIdentifier);
				if (isSingleLayer && UnoRuntimeIdentifier is not ("skia" or "webassembly"))
				{
					this.Log.LogError($"The value '{UnoRuntimeIdentifier}' not expected for 'UnoRuntimeIdentifier'");
					return false;
				}

				var isTwoLayer = !isSingleLayer && UnoUIRuntimeIdentifier == "skia";
				if (isTwoLayer && !IsSkiaMobileOrWasmRuntimeIdentifier(UnoWinRTRuntimeIdentifier))
				{
					this.Log.LogError($"The combination of UnoUIRuntimeIdentifier '{UnoUIRuntimeIdentifier}' and UnoWinRTRuntimeIdentifier '{UnoWinRTRuntimeIdentifier}' is not expected");
					return false;
				}

				if (!isSingleLayer && !isTwoLayer)
				{
					return true;
				}

				foreach (var package in UnoRuntimeEnabledPackage ?? Array.Empty<ITaskItem>())
				{
					HandleForRuntimeEnabled(package, runtimeCopyLocalItemsToAdd, runtimeCopyLocalItemsToRemove, compileFileDefinitionsToAdd, compileFileDefinitionsToRemove, pdbFilesToAdd, isTwoLayer);
				}

				if (isTwoLayer)
				{
					HandleSkiaMobileForNonRuntimeEnabledPackages(runtimeCopyLocalItemsToAdd, runtimeCopyLocalItemsToRemove, compileFileDefinitionsToAdd, compileFileDefinitionsToRemove, pdbFilesToAdd);
				}

				RuntimeCopyLocalItemsToAdd = runtimeCopyLocalItemsToAdd.ToArray();
				RuntimeCopyLocalItemsToRemove = runtimeCopyLocalItemsToRemove.ToArray();
				ResolvedCompileFileDefinitionsToAdd = compileFileDefinitionsToAdd.ToArray();
				ResolvedCompileFileDefinitionsToRemove = compileFileDefinitionsToRemove.ToArray();
				DebugSymbols = pdbFilesToAdd.ToArray();

				return true;
			}
			catch (Exception e)
			{
				// Require because the task is running out of process
				// and can't marshal non-CLR known exceptions.
				throw new Exception(e.ToString());
			}
		}

		private string? GetUnoRuntimeDirectory(ITaskItem package)
		{
			var packageBasePath = package.GetMetadata("PackageBasePath");
			string runtimeDirectory = Path.Combine(packageBasePath, "uno-runtime");
			if (Directory.Exists(runtimeDirectory))
			{
				return runtimeDirectory;
			}

			runtimeDirectory = Path.Combine(packageBasePath, "..", "uno-runtime");
			if (Directory.Exists(runtimeDirectory))
			{
				return runtimeDirectory;
			}

			return null;
		}

		private string? GetPlatformSpecificDirectoryForRuntimeEnabled(string runtimeDirectory, Version targetFrameworkVersion, bool isTwoLayer)
		{
			string runtimeIdentifier;
			if (isTwoLayer)
			{
				// Two layer mode.
				// We use Skia, except for Uno.dll, Uno.Foundation.dll, and Uno.Dispatching.dll
				// We will adjust for those dlls later.
				if (UnoUIRuntimeIdentifier != "skia")
				{
					throw new Exception($"Unexpected UnoUIRuntimeIdentifier '{UnoUIRuntimeIdentifier}'");
				}

				runtimeIdentifier = UnoUIRuntimeIdentifier;
			}
			else
			{
				// Single layer mode
				runtimeIdentifier = UnoRuntimeIdentifier;
			}

			this.Log.LogMessage($"Searching for '{runtimeIdentifier}' in '{runtimeDirectory}'");

			for (int i = LatestSupportedDotnetVersion; i >= EarliestSupportedDotnetVersion; i--)
			{
				var tfm = $"net{i.ToString(CultureInfo.InvariantCulture)}.0";

				if (targetFrameworkVersion >= new Version(i, 0))
				{
					var directory = Path.Combine(runtimeDirectory, tfm, runtimeIdentifier);
					if (Directory.Exists(directory))
					{
						return directory;
					}
					else
					{
						this.Log.LogMessage($"Directory '{directory}' does not exist.");
					}
				}
			}

			var netstdDirectory = Path.Combine(runtimeDirectory, "netstandard2.0", runtimeIdentifier);
			if (Directory.Exists(netstdDirectory))
			{
				return netstdDirectory;
			}
			else
			{
				this.Log.LogMessage($"Directory '{netstdDirectory}' does not exist.");
			}

			return null;
		}

		private string GetReferenceDirectory(string runtimeDirectory, Version targetFrameworkVersion)
		{
			for (int i = LatestSupportedDotnetVersion; i >= EarliestSupportedDotnetVersion; i--)
			{
				var tfm = $"net{i.ToString(CultureInfo.InvariantCulture)}.0";

				if (targetFrameworkVersion >= new Version(i, 0))
				{
					var directory = Path.Combine(runtimeDirectory, "..", "lib", tfm);
					if (Directory.Exists(directory))
					{
						return directory;
					}
				}
			}

			var netstdDirectory = Path.Combine(runtimeDirectory, "..", "lib", "netstandard2.0");
			if (Directory.Exists(netstdDirectory))
			{
				return netstdDirectory;
			}

			throw new Exception($"Unable to find reference directory from runtime directory '{runtimeDirectory}'");
		}

		private bool IsWinRTAssembly(string fileNameWithoutExtension)
			=> fileNameWithoutExtension.ToLower(CultureInfo.InvariantCulture) is "uno" or "uno.ui.dispatching" or "uno.foundation";

		private string GetWinRTAssembly(string runtimeDirectory, string assembly, Version targetFrameworkVersion)
		{
			// Assembly is on the form:
			// <NuGetPackageRoot>/<PackageName>/<PackageVersion>/uno-runtime/<TargetFramework>/<RuntimeIdentifier>/<AssemblyName>.dll
			assembly = Path.GetFullPath(assembly);
			var unoRuntimeTfmDirectory = Path.GetDirectoryName(Path.GetDirectoryName(assembly));
			if (UnoWinRTRuntimeIdentifier == "webassembly")
			{
				return Path.GetFullPath(Path.Combine(unoRuntimeTfmDirectory, "webassembly", Path.GetFileName(assembly)));
			}

			if (!IsSkiaMobileRuntimeIdentifier(UnoWinRTRuntimeIdentifier))
			{
				throw new Exception($"Unexpected UnoWinRTRuntimeIdentifier '{UnoWinRTRuntimeIdentifier}'");
			}

			var packageRoot = Path.GetDirectoryName(Path.GetDirectoryName(unoRuntimeTfmDirectory));
			var lib = Path.Combine(packageRoot, "lib");

			string? bestTfmMatch = null;
			Version? bestMatchVersion = null;
			foreach (var dir in Directory.GetDirectories(lib))
			{
				var tfm = Path.GetFileName(dir);
				var dashIndex = tfm.IndexOf($"-{UnoWinRTRuntimeIdentifier}", StringComparison.Ordinal);
				if (tfm.StartsWith("net", StringComparison.Ordinal) && dashIndex >= 6 &&
					Version.TryParse(tfm.Substring(3, dashIndex - 3), out var currentVersion) &&
					targetFrameworkVersion >= currentVersion)
				{
					if (bestTfmMatch is null || currentVersion > bestMatchVersion)
					{
						bestTfmMatch = tfm;
						bestMatchVersion = currentVersion;
					}
				}
			}

			if (bestTfmMatch is null)
			{
				throw new Exception($"Cannot get WinRT assembly for '{assembly}'");
			}

			return Path.GetFullPath(Path.Combine(lib, bestTfmMatch, Path.GetFileName(assembly)));
		}

		private void HandleForRuntimeEnabled(
			ITaskItem package,
			List<ITaskItem> runtimeCopyLocalItemsToAdd,
			List<ITaskItem> runtimeCopyLocalItemsToRemove,
			List<ITaskItem> compileFileDefinitionsToAdd,
			List<ITaskItem> compileFileDefinitionsToRemove,
			List<ITaskItem> pdbFilesToAdd,
			bool isTwoLayer)
		{
			var packageIdentity = package.GetMetadata("Identity");
			this.Log.LogMessage($"Processing runtime-enabled package: {packageIdentity}");
			if (GetUnoRuntimeDirectory(package) is not { } runtimeDirectory)
			{
				this.Log.LogMessage($"Cannot find uno-runtime in package '{packageIdentity}'.");
				return;
			}

			if (!Version.TryParse(TargetFrameworkVersion?.Substring(1), out var targetFrameworkVersion))
			{
				targetFrameworkVersion = new(2, 0);
			}

			runtimeCopyLocalItemsToRemove.AddRange(RuntimeCopyLocalItemsInput.Where(item => packageIdentity.Equals(item.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase)));

			var platformDirectory = GetPlatformSpecificDirectoryForRuntimeEnabled(runtimeDirectory, targetFrameworkVersion, isTwoLayer);
			if (platformDirectory is null)
			{
				// This can happen for "legacy convention" (uno-runtime/<runtime-identifier>) which is handled by MSBuild logic in ReplaceUnoRuntime
				this.Log.LogMessage("Cannot find platform-specific directory for runtime-enabled package");
				this.Log.LogMessage($"\tThe uno-runtime directory: {runtimeDirectory}");
				this.Log.LogMessage($"\tThe TFM version: {targetFrameworkVersion}");
				return;
			}

			this.Log.LogMessage($"Found platform-specific directory for runtime-enabled package: {platformDirectory}");

			foreach (var assembly in Directory.EnumerateFiles(platformDirectory, "*.dll"))
			{
				var assemblyFileNameWithoutExtension = Path.GetFileNameWithoutExtension(assembly);
				var adjustedAssembly = assembly;
				var isWinRTAssembly = isTwoLayer && IsWinRTAssembly(assemblyFileNameWithoutExtension);
				if (isWinRTAssembly)
				{
					adjustedAssembly = GetWinRTAssembly(runtimeDirectory, assembly, targetFrameworkVersion);
				}

				this.Log.LogMessage($"Processing assembly: {adjustedAssembly}");

				runtimeCopyLocalItemsToAdd.Add(new TaskItem(
					adjustedAssembly,
					new Dictionary<string, string>
					{
						["NuGetPackageId"] = packageIdentity,
						["PathInPackage"] = GetPathInPackage(adjustedAssembly, runtimeDirectory),
					}));

				var pdbFile = adjustedAssembly.Substring(0, adjustedAssembly.Length - 3) + "pdb";
				if (File.Exists(pdbFile))
				{
					pdbFilesToAdd.Add(new TaskItem(
						pdbFile,
						new Dictionary<string, string>
						{
							["NuGetPackageId"] = packageIdentity,
						}));
				}

				if (isTwoLayer && IsSkiaMobileRuntimeIdentifier(UnoWinRTRuntimeIdentifier))
				{
					var compileTimeAssembly = adjustedAssembly;
					if (!isWinRTAssembly)
					{
						var referenceDirectory = GetReferenceDirectory(runtimeDirectory, targetFrameworkVersion);
						var file = Directory.EnumerateFiles(referenceDirectory, "*.dll")
							.FirstOrDefault(file => assemblyFileNameWithoutExtension.Equals(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase));
						if (file is null)
						{
							throw new Exception($"Cannot find reference assembly for {assembly}");
						}

						compileTimeAssembly = file;
					}

					var existing = ResolvedCompileFileDefinitionsInput.First(item => packageIdentity.Equals(item.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase));
					compileFileDefinitionsToAdd.Add(new TaskItem(
						compileTimeAssembly,
						new Dictionary<string, string>
						{
							["HintPath"] = compileTimeAssembly,
							["NuGetPackageVersion"] = existing.GetMetadata("NuGetPackageVersion"),
							["Private"] = existing.GetMetadata("Private"),
							["ExternallyResolved"] = existing.GetMetadata("ExternallyResolved"),
							["NuGetPackageId"] = packageIdentity,
							["PathInPackage"] = GetPathInPackage(compileTimeAssembly, runtimeDirectory),
							["NuGetSourceType"] = existing.GetMetadata("NuGetSourceType"),
						}));

					var toRemove = ResolvedCompileFileDefinitionsInput
						.FirstOrDefault(
							item => packageIdentity.Equals(item.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase) &&
							Path.GetFileNameWithoutExtension(item.GetMetadata("Identity")) == assemblyFileNameWithoutExtension);

					if (toRemove is not null)
					{
						compileFileDefinitionsToRemove.Add(toRemove);
					}
				}
			}
		}

		private string GetPathInPackage(string assembly, string runtimeDirectory)
		{
			var packageRoot = Path.GetFullPath(Path.Combine(runtimeDirectory, ".."));
			assembly = Path.GetFullPath(assembly);
			if (!assembly.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase))
			{
				throw new Exception($"Cannot get PathInPackage for assembly '{assembly}' and package root '{packageRoot}'");
			}

			var pathInPackage = assembly.Substring(packageRoot.Length);
			return pathInPackage.Replace('\\', '/').TrimStart('/');
		}

		private void HandleSkiaMobileForNonRuntimeEnabledPackages(
			List<ITaskItem> runtimeCopyLocalItemsToAdd,
			List<ITaskItem> runtimeCopyLocalItemsToRemove,
			List<ITaskItem> compileFileDefinitionsToAdd,
			List<ITaskItem> compileFileDefinitionsToRemove,
			List<ITaskItem> pdbFilesToAdd)
		{
			// For Android Skia and iOS Skia, we want to resolve netX.0 instead of netX.0-[android|ios] for non-RuntimeEnabled packages.
			// The idea here is that we loop over ResolvedCompileFileDefinitionsInput, look for dlls from NuGet package cache,
			// and then try to find the right dll.
			if (IsSkiaMobileRuntimeIdentifier(UnoWinRTRuntimeIdentifier))
			{
				var runtimeEnabledPackages = UnoRuntimeEnabledPackage.Select(p => p.GetMetadata("Identity")).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
				var nugetCacheRoot = NuGetPackageRoot.Replace('\\', '/');
				if (!nugetCacheRoot.EndsWith("/", StringComparison.Ordinal))
				{
					nugetCacheRoot += "/";
				}

				foreach (var compileFileDefinition in ResolvedCompileFileDefinitionsInput ?? Array.Empty<ITaskItem>())
				{
					// identityNormalized is expected to be on the form:
					// <NuGetPackageRoot>/<PackageName>/<PackageVersion>/lib/<TargetFramework>/<AssemblyName>.dll
					var identityNormalized = compileFileDefinition.GetMetadata("Identity").Replace('\\', '/');

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
									targetFramework.Contains("-ios") ||
									targetFramework.Contains("-maccatalyst") |
									targetFramework.Contains("-tvos"))
								{
									var packageVersion = split[1];

									// TODO: If netX.0-desktop is present, maybe we should prefer it.
									var adjustedTargetFramework = targetFramework.Substring(0, targetFramework.IndexOf('-'));

									var dllFileName = split[4];
									var adjustedPath = $"{nugetCacheRoot}{packageName}/{packageVersion}/lib/{adjustedTargetFramework}/{dllFileName}";
									if (File.Exists(adjustedPath))
									{
										var originalAssembly = AssemblyDefinition.ReadAssembly(identityNormalized);
										if (!originalAssembly.MainModule.AssemblyReferences.Any(m => m.Name == "Uno.UI"))
										{
											// We only need to retarget packages that are explicitly referencing Uno.UI
											// Other packages that are referencing Uno to access WinRT APIs do not need
											// to be retargeted.
											this.Log.LogMessage($"Skipping {originalAssembly} replacement");
											continue;
										}
										this.Log.LogMessage("Replacing " + packageName + " " + adjustedPath);
										var fullAdjustedPath = Path.GetFullPath(adjustedPath);
										runtimeCopyLocalItemsToAdd.Add(new TaskItem(
											fullAdjustedPath,
											new Dictionary<string, string>
											{
												["NuGetPackageId"] = packageName,
												["PathInPackage"] = $"lib/{adjustedTargetFramework}/{dllFileName}",
											}));

										var pdbFile = fullAdjustedPath.Substring(0, fullAdjustedPath.Length - 3) + "pdb";
										if (File.Exists(pdbFile))
										{
											pdbFilesToAdd.Add(new TaskItem(
												pdbFile,
												new Dictionary<string, string>
												{
													["NuGetPackageId"] = packageName,
												}));
										}

										compileFileDefinitionsToAdd.Add(new TaskItem(
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

										var toRemove = RuntimeCopyLocalItemsInput
											.FirstOrDefault(
												item => packageName.Equals(item.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase) &&
												Path.GetFileName(item.GetMetadata("Identity")) == dllFileName);

										if (toRemove is not null)
										{
											runtimeCopyLocalItemsToRemove.Add(toRemove);
										}

										compileFileDefinitionsToRemove.Add(compileFileDefinition);

									}
								}
							}
						}
					}
				}
			}
		}

		private bool IsSkiaMobileRuntimeIdentifier(string runtimeIdentifier)
			=> runtimeIdentifier is "android" or "ios" or "maccatalyst" or "tvos";

		private bool IsSkiaMobileOrWasmRuntimeIdentifier(string runtimeIdentifier) =>
			IsSkiaMobileRuntimeIdentifier(runtimeIdentifier) || runtimeIdentifier is "webassembly";
	}
}
