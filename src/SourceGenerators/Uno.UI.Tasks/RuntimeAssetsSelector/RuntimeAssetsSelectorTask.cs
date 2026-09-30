#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.UI.Tasks.RuntimeAssetsSelector
{
	/// <summary>
	/// A task used to merge linker definition files and embed the result in an assembly
	/// </summary>
	public class RuntimeAssetsSelectorTask_v0 : Microsoft.Build.Utilities.Task
	{
		private const int LatestSupportedDotnetVersion = 11; // **MUST BE** net11.0 aligned (Keep this comment to ease upgrade to later versions of .NET)

		// Even if Uno does not support net7.0 explicitly anymore, dependencies
		// may still be providing net7.0 runtime support files (e.g. SkiaSharp)
		private const int EarliestSupportedDotnetVersion = 7;

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? UnoRuntimeEnabledPackage { get; set; }

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsInput { get; set; }

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? RuntimeCopyLocalItemsInput { get; set; }

		/// <summary>
		/// The platform of the target framework being built, e.g. "android", or empty for a plain netX.0 head.
		/// </summary>
		/// <remarks>
		/// Deliberately not [Required]: MSBuild treats an empty string as unsupplied, and empty is the real
		/// value for a headless head on a plain netX.0 target framework.
		/// </remarks>
		public string TargetPlatformIdentifier { get; set; } = "";

		/// <summary>
		/// The <c>UnoRuntimeVariant</c> of a cross-runtime library build (no runtime host): "generic" or "wasm".
		/// A library has no per-platform head to derive it from, so it names its runtime folder directly.
		/// Empty for an application head, which derives it from <see cref="TargetPlatformIdentifier"/>.
		/// </summary>
		public string LibraryRuntimeVariant { get; set; } = "";

		/// <summary>
		/// The <c>UnoRuntimeVariant</c> values, lowercased. "generic" holds the build every target framework drawn
		/// by Uno shares, and "wasm" the browser's WinRT implementation. A folder miss is not an error here, so
		/// ReplaceUnoRuntime reports it as UNOB0023.
		/// </summary>
		private const string GenericRuntimeFolder = "generic";

		private const string WasmRuntimeFolder = "wasm";

		/// <summary>
		/// Where a package's WinRT assemblies come from. Everything else always comes from the runtime folder.
		/// </summary>
		private enum WinRTSource
		{
			/// <summary>Desktop and headless heads: the generic folder carries the implementation.</summary>
			GenericFolder,

			/// <summary>Browser heads: the wasm folder next to the generic one.</summary>
			WasmFolder,

			/// <summary>Mobile heads: the package's own lib/netX.0-&lt;platform&gt; asset.</summary>
			PlatformLib,
		}

		/// <remarks>
		/// Note that this property is not set to [Required] because
		/// with netstandard2.0, it is not set and the default value is used instead.
		/// </remarks>
		public string TargetFrameworkVersion { get; set; } = "";

		/// <summary>
		/// Whether an asset a runtime-enabled package should provide but does not is a UNOB0023 error. False
		/// during design-time builds, where a partially restored package is expected, and when UNOB0023 is disabled.
		/// </summary>
		public bool ReportUnresolvedAssets { get; set; } = true;

		private const string UnresolvedAssetCode = "UNOB0023";
		private const string UnresolvedAssetHelpLink = "https://aka.platform.uno/UNOB0023";

		private sealed class UnresolvedAssetException(string message) : Exception(message);

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
				// We have two types of packages
				// 1. Packages that are runtime-enabled (e.g, contains uno-runtime - which is signified by <UnoRuntimeEnabledPackage Include="PackageName" PackageBasePath="..." /> in package props/targets)
				// 2. Packages that are not runtime-enabled.
				//
				// For runtime-enabled packages the assemblies always come from the runtime folder, except
				// the WinRT ones (see IsWinRTAssembly), which follow the head's platform:
				//     - desktop and headless: the generic folder holds the implementation already.
				//     - browser: the sibling wasm folder. Compile references are left alone, since the
				//       platform-neutral surface is the one to bind against.
				//     - android, iOS and tvOS: the package's own lib/netX.0-<platform> asset, and compile
				//       references are rewritten so a WinRT call binds the platform implementation.
				//
				// For non-runtime-enabled packages we do nothing: NuGet's own target framework selection stands,
				// so a multi-targeted library keeps its netX.0-[android|ios|tvos] asset.

				var runtimeCopyLocalItemsToAdd = new List<ITaskItem>();
				var runtimeCopyLocalItemsToRemove = new List<ITaskItem>();
				var compileFileDefinitionsToAdd = new List<ITaskItem>();
				var compileFileDefinitionsToRemove = new List<ITaskItem>();
				var pdbFilesToAdd = new List<ITaskItem>();

				var platform = TargetPlatformIdentifier.ToLower(CultureInfo.InvariantCulture);
				WinRTSource winRTSource;
				string runtimeFolder = GenericRuntimeFolder;

				if (!string.IsNullOrEmpty(LibraryRuntimeVariant))
				{
					// Library-authoring contract: there's no per-platform head here, so the variant names
					// its runtime folder directly instead of being derived from TargetPlatformIdentifier.
					switch (LibraryRuntimeVariant.ToLower(CultureInfo.InvariantCulture))
					{
						case WasmRuntimeFolder:
							winRTSource = WinRTSource.WasmFolder;
							runtimeFolder = WasmRuntimeFolder;
							break;

						case GenericRuntimeFolder:
							winRTSource = WinRTSource.GenericFolder;
							break;

						default:
							LogUnresolved($"UnoRuntimeVariant '{LibraryRuntimeVariant}' is not supported for a cross-runtime library. Use Generic, Wasm or Reference.");
							return false;
					}
				}
				else
				{
					switch (platform)
					{
						case "":
						case "desktop":
							winRTSource = WinRTSource.GenericFolder;
							break;

						case "browserwasm":
							winRTSource = WinRTSource.WasmFolder;
							break;

						case "android":
						case "ios":
						case "tvos":
							winRTSource = WinRTSource.PlatformLib;
							break;

						default:
							// Without this error an unknown platform would silently leave every runtime-enabled
							// package on its reference facade, in a green build that only fails once it runs.
							LogUnresolved(
								$"The target platform '{TargetPlatformIdentifier}' has no Uno Platform runtime assets. " +
								"A head with a Uno Platform runtime host must target desktop, browserwasm, android, ios, tvos, or a plain netX.0 framework.");
							return false;
					}
				}

				foreach (var package in UnoRuntimeEnabledPackage ?? Array.Empty<ITaskItem>())
				{
					HandleForRuntimeEnabled(package, runtimeCopyLocalItemsToAdd, runtimeCopyLocalItemsToRemove, compileFileDefinitionsToAdd, compileFileDefinitionsToRemove, pdbFilesToAdd, winRTSource, platform, runtimeFolder);
				}

				RuntimeCopyLocalItemsToAdd = runtimeCopyLocalItemsToAdd.ToArray();
				RuntimeCopyLocalItemsToRemove = runtimeCopyLocalItemsToRemove.ToArray();
				ResolvedCompileFileDefinitionsToAdd = compileFileDefinitionsToAdd.ToArray();
				ResolvedCompileFileDefinitionsToRemove = compileFileDefinitionsToRemove.ToArray();
				DebugSymbols = pdbFilesToAdd.ToArray();

				return true;
			}
			catch (UnresolvedAssetException e) when (!ReportUnresolvedAssets)
			{
				this.Log.LogMessage(MessageImportance.Normal, e.Message);
				return true;
			}
			catch (UnresolvedAssetException e)
			{
				LogUnresolved(e.Message);
				return false;
			}
			catch (Exception e)
			{
				// Require because the task is running out of process
				// and can't marshal non-CLR known exceptions.
				throw new Exception(e.ToString());
			}
		}

		private void LogUnresolved(string message)
			=> this.Log.LogError(null, UnresolvedAssetCode, null, UnresolvedAssetHelpLink, null, 0, 0, 0, 0, $"{message} See {UnresolvedAssetHelpLink}");

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

		/// <remarks>
		/// The runtime folder's file listing is the authoritative asset list: an assembly is deployed because it
		/// appears here, and only then is it redirected per <see cref="WinRTSource"/>.
		/// </remarks>
		private string? GetPlatformSpecificDirectoryForRuntimeEnabled(string runtimeDirectory, Version targetFrameworkVersion, string runtimeFolder)
		{
			this.Log.LogMessage($"Searching for '{runtimeFolder}' in '{runtimeDirectory}'");

			for (int i = LatestSupportedDotnetVersion; i >= EarliestSupportedDotnetVersion; i--)
			{
				var tfm = $"net{i.ToString(CultureInfo.InvariantCulture)}.0";

				if (targetFrameworkVersion >= new Version(i, 0))
				{
					var directory = Path.Combine(runtimeDirectory, tfm, runtimeFolder);
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

			return null;
		}

		private string GetReferenceDirectory(string packageIdentity, string runtimeDirectory, Version targetFrameworkVersion)
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

			throw new UnresolvedAssetException(
				$"The runtime-enabled package '{packageIdentity}' has no lib/netX.0 reference folder next to '{Path.GetFullPath(runtimeDirectory)}'.");
		}

		// Uno.UI.MSAL only depends on the WinRT layer (Uno.UWP), so it must follow the
		// WinRT layer selection to keep its platform-specific helpers (https://github.com/unoplatform/uno/issues/20601).
		private bool IsWinRTAssembly(string fileNameWithoutExtension)
			=> fileNameWithoutExtension.ToLower(CultureInfo.InvariantCulture) is "uno.winrt" or "uno.ui.dispatching" or "uno.foundation" or "uno.ui.msal";

		private string GetWinRTAssembly(string packageIdentity, string assembly, Version targetFrameworkVersion, WinRTSource winRTSource, string platform)
		{
			// Assembly is on the form:
			// <NuGetPackageRoot>/<PackageName>/<PackageVersion>/uno-runtime/<TargetFramework>/<RuntimeFolder>/<AssemblyName>.dll
			assembly = Path.GetFullPath(assembly);
			var unoRuntimeTfmDirectory = Path.GetDirectoryName(Path.GetDirectoryName(assembly));
			if (winRTSource == WinRTSource.WasmFolder)
			{
				var wasmAsset = Path.GetFullPath(Path.Combine(unoRuntimeTfmDirectory, WasmRuntimeFolder, Path.GetFileName(assembly)));
				if (!File.Exists(wasmAsset))
				{
					throw new UnresolvedAssetException(
						$"The runtime-enabled package '{packageIdentity}' has no browser implementation of '{Path.GetFileName(assembly)}': '{wasmAsset}' does not exist.");
				}

				return wasmAsset;
			}

			var packageRoot = Path.GetDirectoryName(Path.GetDirectoryName(unoRuntimeTfmDirectory));
			var lib = Path.Combine(packageRoot, "lib");
			if (!Directory.Exists(lib))
			{
				throw new UnresolvedAssetException(
					$"The runtime-enabled package '{packageIdentity}' has no '{platform}' implementation of '{Path.GetFileName(assembly)}': '{lib}' does not exist.");
			}

			string? bestTfmMatch = null;
			Version? bestMatchVersion = null;
			foreach (var dir in Directory.GetDirectories(lib))
			{
				var tfm = Path.GetFileName(dir);
				var dashIndex = tfm.IndexOf($"-{platform}", StringComparison.Ordinal);
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
				throw new UnresolvedAssetException(
					$"The runtime-enabled package '{packageIdentity}' has no '{platform}' implementation of '{Path.GetFileName(assembly)}': no lib/netX.0-{platform} folder in '{lib}' matches net{targetFrameworkVersion}.");
			}

			var winRTAssembly = Path.GetFullPath(Path.Combine(lib, bestTfmMatch, Path.GetFileName(assembly)));
			if (!File.Exists(winRTAssembly))
			{
				throw new UnresolvedAssetException(
					$"The runtime-enabled package '{packageIdentity}' has no '{platform}' implementation of '{Path.GetFileName(assembly)}': '{winRTAssembly}' does not exist.");
			}

			return winRTAssembly;
		}

		private void HandleForRuntimeEnabled(
			ITaskItem package,
			List<ITaskItem> runtimeCopyLocalItemsToAdd,
			List<ITaskItem> runtimeCopyLocalItemsToRemove,
			List<ITaskItem> compileFileDefinitionsToAdd,
			List<ITaskItem> compileFileDefinitionsToRemove,
			List<ITaskItem> pdbFilesToAdd,
			WinRTSource winRTSource,
			string platform,
			string runtimeFolder)
		{
			var packageIdentity = package.GetMetadata("Identity");
			this.Log.LogMessage($"Processing runtime-enabled package: {packageIdentity}");
			if (GetUnoRuntimeDirectory(package) is not { } runtimeDirectory)
			{
				var packageBasePath = package.GetMetadata("PackageBasePath");
				this.Log.LogMessage(
					$"Cannot find uno-runtime in package '{packageIdentity}': neither '{Path.GetFullPath(Path.Combine(packageBasePath, "uno-runtime"))}' " +
					$"nor '{Path.GetFullPath(Path.Combine(packageBasePath, "..", "uno-runtime"))}' exists.");
				return;
			}

			if (!Version.TryParse(TargetFrameworkVersion?.Substring(1), out var targetFrameworkVersion))
			{
				targetFrameworkVersion = new(2, 0);
			}

			runtimeCopyLocalItemsToRemove.AddRange(RuntimeCopyLocalItemsInput.Where(item => packageIdentity.Equals(item.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase)));

			var platformDirectory = GetPlatformSpecificDirectoryForRuntimeEnabled(runtimeDirectory, targetFrameworkVersion, runtimeFolder);
			if (platformDirectory is null)
			{
				this.Log.LogMessage("Cannot find platform-specific directory for runtime-enabled package");
				this.Log.LogMessage($"\tThe uno-runtime directory: {runtimeDirectory}");
				this.Log.LogMessage($"\tThe TFM version: {targetFrameworkVersion}");
				return;
			}

			this.Log.LogMessage($"Found platform-specific directory for runtime-enabled package: {platformDirectory}");

			// Mobile only, and the same for every assembly of the package, so resolved once.
			string? referenceDirectory = null;
			Dictionary<string, string>? referenceAssemblies = null;
			List<ITaskItem>? packageCompileItems = null;

			foreach (var assembly in Directory.EnumerateFiles(platformDirectory, "*.dll"))
			{
				var assemblyFileNameWithoutExtension = Path.GetFileNameWithoutExtension(assembly);
				var adjustedAssembly = assembly;
				var isWinRTAssembly = winRTSource != WinRTSource.GenericFolder && IsWinRTAssembly(assemblyFileNameWithoutExtension);
				if (isWinRTAssembly)
				{
					adjustedAssembly = GetWinRTAssembly(packageIdentity, assembly, targetFrameworkVersion, winRTSource, platform);
					this.Log.LogMessage($"Assembly '{assemblyFileNameWithoutExtension}' follows the WinRT layer: replacing '{assembly}' with '{adjustedAssembly}'");
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

				if (winRTSource == WinRTSource.PlatformLib)
				{
					var compileTimeAssembly = adjustedAssembly;
					if (!isWinRTAssembly)
					{
						referenceDirectory ??= GetReferenceDirectory(packageIdentity, runtimeDirectory, targetFrameworkVersion);
						referenceAssemblies ??= IndexByName(Directory.EnumerateFiles(referenceDirectory, "*.dll"));
						if (!referenceAssemblies.TryGetValue(assemblyFileNameWithoutExtension, out var file))
						{
							throw new UnresolvedAssetException(
								$"The runtime-enabled package '{packageIdentity}' has no reference assembly for '{Path.GetFileName(assembly)}' in '{Path.GetFullPath(referenceDirectory)}'.");
						}

						compileTimeAssembly = file;
					}

					packageCompileItems ??= ResolvedCompileFileDefinitionsInput
						.Where(item => packageIdentity.Equals(item.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase))
						.ToList();

					var existing = packageCompileItems.First();
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

					var toRemove = packageCompileItems
						.FirstOrDefault(item => Path.GetFileNameWithoutExtension(item.GetMetadata("Identity")) == assemblyFileNameWithoutExtension);

					if (toRemove is not null)
					{
						compileFileDefinitionsToRemove.Add(toRemove);
					}
				}
			}
		}

		private static Dictionary<string, string> IndexByName(IEnumerable<string> files)
		{
			var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var file in files)
			{
				var name = Path.GetFileNameWithoutExtension(file);
				if (!index.ContainsKey(name))
				{
					index[name] = file;
				}
			}

			return index;
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
	}
}
