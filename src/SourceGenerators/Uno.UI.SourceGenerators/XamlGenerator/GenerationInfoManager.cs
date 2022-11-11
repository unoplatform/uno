#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

	}
}
