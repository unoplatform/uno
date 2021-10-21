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
		}
	}
}
