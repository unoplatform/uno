#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class GenerationRunInfoManager
	{
		private List<GenerationRunInfo> _runs = new List<GenerationRunInfo>();
		private DateTime _previousWriteTime;

		internal GenerationRunInfoManager()
		{
		}

		public IEnumerable<GenerationRunInfo> PreviousRuns
			=> _runs.AsEnumerable();

		internal GenerationRunInfo CreateRun()
		{
			var runInfo = new GenerationRunInfo(this, _runs.Count);

			_runs.Add(runInfo);

			return runInfo;
		}

		internal void Update(GeneratorExecutionContext context)
		{
			var intermediateOutputPath = context.GetMSBuildPropertyValue("IntermediateOutputPath");

			if (intermediateOutputPath != null)
			{
				var runFilePath = Path.Combine(intermediateOutputPath, "build-time-generator.touch");

				if (File.Exists(runFilePath))
				{
					var lastWriteTime = new FileInfo(runFilePath).LastWriteTime;

					if(lastWriteTime > _previousWriteTime)
					{
						_previousWriteTime = lastWriteTime;

						// Clear the existing runs if a full build has been started
						_runs.Clear();
					}
				}
			}

			if(GetIsDesignTimeBuild(context) && !GetIsHotReloadHost(context))
			{
				// Design-time builds need to clear runs for the x:Name values to be regenerated, in the context of OmniSharp.
				// In the context of HotReload, we need to skip this, as the HotReload service sets DesignTimeBuild to build
				// to true, preventing existing runs to be kept active.
				_runs.Clear();
			}
		}

		private bool GetIsDesignTimeBuild(GeneratorExecutionContext context)
		{
			return bool.TryParse(context.GetMSBuildPropertyValue("DesignTimeBuild"), out var value) && value;
		}

		private bool GetIsHotReloadHost(GeneratorExecutionContext context)
		{
			return bool.TryParse(context.GetMSBuildPropertyValue("IsHotReloadHost"), out var value) && value;
		}
	}
}
