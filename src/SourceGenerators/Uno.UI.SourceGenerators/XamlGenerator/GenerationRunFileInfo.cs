#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record GenerationRunFileInfo(GenerationRunInfo RunInfo, string FileId)
	{
		private Dictionary<string, int> _appliedTypes = new Dictionary<string, int>();

		internal string? ComponentCode { get; set; }

		internal IReadOnlyDictionary<string, int> AppliedTypes => _appliedTypes;

		internal void SetAppliedTypes(Dictionary<string, int> appliedTypes)
		{
			foreach (var type in appliedTypes)
			{
				_appliedTypes.Add(type.Key, type.Value);
			}
		}
	}
}
