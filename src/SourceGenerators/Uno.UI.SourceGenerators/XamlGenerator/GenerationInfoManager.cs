#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Equality;
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

		internal GenerationRunInfo CreateRun(GeneratorExecutionContext context)
		{
			bool.TryParse(context.GetMSBuildPropertyValue("UnoUseXamlReaderHotReload"), out var useXamlReaderHotReload);

			var hash = context
				.AdditionalFiles
				.Aggregate(0, (hash, f) => ByteSequenceComparer.GetHashCode(f.GetText()?.GetChecksum() ?? ImmutableArray<byte>.Empty) ^ hash);

			// Only create a new run when the previous run additional files are different
			// This ensures that each run produces the same output for a given input.
			if (
				!useXamlReaderHotReload
				&& _runs.Any()
				&& hash == _runs[_runs.Count - 1].AdditionalFilesHash)
			{
				return _runs[_runs.Count - 1];
			}
			else
			{
				var runInfo = new GenerationRunInfo(this, _runs.Count, hash);

				_runs.Add(runInfo);

				return runInfo;
			}
		}
	}
}
