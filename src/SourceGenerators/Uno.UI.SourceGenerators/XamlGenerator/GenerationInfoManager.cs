#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Equality;
using Uno.Extensions;
using Uno.Roslyn;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class GenerationRunInfoManager
	{
		private List<GenerationRunInfo> _runs = new List<GenerationRunInfo>();
		private Dictionary<(string projectFile, string targetFramework), DateTimeOffset> _previousWriteTime = new();

		/// <summary>
		/// Definition of a hash information based on a project and target framework. This is needed
		/// since generators can be instantiated only once and kept active in VBCSCompiler.
		/// </summary>
		private record class HashInfo(string ProjectFile, string TargetFramework, int Hash);

		/// <summary>
		/// A list of known hashes for the current process to avoid removing previously
		/// generated hashes and break Roslyn's metadata generator with inconsistent missing
		/// methods.
		/// </summary>
		private static ConcurrentDictionary<HashInfo, object?> _knownAdditionalFilesHashes = new();

		internal GenerationRunInfoManager()
		{
			foreach (var hash in _knownAdditionalFilesHashes.ToArray())
			{
				_runs.Add(new(this, hash.Key.ProjectFile, hash.Key.TargetFramework, hash.Key.Hash));
			}
		}

		public IEnumerable<GenerationRunInfo> GetAllRuns(GenerationRunInfo currentRun)
			=> _runs
				.Where(r => r.TargetFramework == currentRun.TargetFramework && r.ProjectFile == currentRun.ProjectFile)
				.AsEnumerable();

		internal GenerationRunInfo CreateRun(GeneratorExecutionContext context)
		{
			ReadProjectConfiguration(
				context,
				out var useXamlReaderHotReload,
				out var useHotReload);

			var hash = context
				.AdditionalFiles
				.Aggregate(0, (hash, f) => ByteSequenceComparer.GetHashCode(f.GetText()?.GetChecksum() ?? ImmutableArray<byte>.Empty) ^ hash);

			// Only create a new run when the previous run additional files are different
			// This ensures that each run produces the same output for a given input.
			if (
				!useXamlReaderHotReload
				&& useHotReload
				&& _runs.FirstOrDefault(r => r.AdditionalFilesHash == hash) is { } run)
			{
				return run;
			}
			else
			{
				var projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
				var targetFramework = context.GetMSBuildPropertyValue("TargetFramework");

				var runInfo = new GenerationRunInfo(this, projectFullPath, targetFramework, hash);

				_runs.Add(runInfo);

				_knownAdditionalFilesHashes.TryAdd(new(projectFullPath, targetFramework, hash), null);

				return runInfo;
			}
		}

		private static void ReadProjectConfiguration(GeneratorExecutionContext context, out bool useXamlReaderHotReload, out bool useHotReload)
		{
			bool.TryParse(context.GetMSBuildPropertyValue("UnoUseXamlReaderHotReload"), out useXamlReaderHotReload);

			var configuration = context.GetMSBuildPropertyValue("Configuration")
				?? throw new InvalidOperationException("The configuration property must be provided");

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoForceHotReloadCodeGen"), out var forceHotReloadCodeGen))
			{
				useHotReload = forceHotReloadCodeGen;
			}
			else
			{
				useHotReload = string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase);
			}
		}

		internal void Update(GeneratorExecutionContext context)
		{
			var intermediateOutputPath = context.GetMSBuildPropertyValue("IntermediateOutputPath");
			var projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
			var targetFramework = context.GetMSBuildPropertyValue("TargetFramework");

			if (intermediateOutputPath != null)
			{
				intermediateOutputPath = Path.IsPathRooted(intermediateOutputPath)
					? intermediateOutputPath
					: Path.Combine(Path.GetDirectoryName(projectFullPath)!, intermediateOutputPath);

				var runFilePath = Path.Combine(intermediateOutputPath, "build-time-generator.touch");

				if (File.Exists(runFilePath))
				{
					var lastWriteTime = new FileInfo(runFilePath).LastWriteTime;
					var writeTimeKey = (projectFullPath, targetFramework);

					lock (_previousWriteTime)
					{
						_ = _previousWriteTime.TryGetValue(writeTimeKey, out var previousWriteTime);

						if (lastWriteTime > previousWriteTime)
						{
							_previousWriteTime[writeTimeKey] = lastWriteTime;

							// Clear the existing runs if a full build has been started
							ClearRuns(projectFullPath, targetFramework);
						}
					}
				}
			}

			if (GetIsDesignTimeBuild(context)
				&& !GetIsHotReloadHost(context)
				&& !Process.GetCurrentProcess().ProcessName.Equals("devenv", StringComparison.InvariantCultureIgnoreCase))
			{
				// Design-time builds need to clear runs for the x:Name values to be regenerated, in the context of OmniSharp.
				// In the context of HotReload, we need to skip this, as the HotReload service sets DesignTimeBuild to build
				// to true, preventing existing runs to be kept active.
				//
				// Devenv is also added to the conditions as there's no explicit way
				// for knowing that we're in a hot-reload session.
				//
				ClearRuns(projectFullPath, targetFramework);
			}
		}

		private void ClearRuns(string projectFullPath, string targetFramework)
			=> _runs.Remove(r => r.TargetFramework == targetFramework && r.ProjectFile == projectFullPath);

		private bool GetIsDesignTimeBuild(GeneratorExecutionContext context)
			=> bool.TryParse(context.GetMSBuildPropertyValue("DesignTimeBuild"), out var value) && value;

		private bool GetIsHotReloadHost(GeneratorExecutionContext context)
			=> bool.TryParse(context.GetMSBuildPropertyValue("IsHotReloadHost"), out var value) && value;

		internal bool IsFirstRun(GenerationRunFileInfo generationRunFileInfo)
			=> GetAllRuns(generationRunFileInfo.RunInfo).Count() == 1;

		internal GenerationRunInfo GetFirstValidRun(GenerationRunFileInfo generationRunFileInfo, string fileUniqueId)
			=> GetAllRuns(generationRunFileInfo.RunInfo).FirstOrDefault(r => r.GetRunFileInfo(fileUniqueId)?.ComponentCode != null);

		internal IEnumerable<GenerationRunInfo> GetAllRunsWithoutSelf(GenerationRunFileInfo generationRunFileInfo)
			=> GetAllRuns(generationRunFileInfo.RunInfo).Except(generationRunFileInfo.RunInfo);
	}
}
