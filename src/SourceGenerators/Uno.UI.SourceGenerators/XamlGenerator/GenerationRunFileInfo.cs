#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record GenerationRunFileInfo(GenerationRunInfo RunInfo, string FileId)
	{
		private Dictionary<INamedTypeSymbol, int> _appliedTypes = new Dictionary<INamedTypeSymbol, int>();

		internal string? ComponentCode { get; set; }

		internal IReadOnlyDictionary<INamedTypeSymbol, int> AppliedTypes => _appliedTypes;

		internal void SetAppliedTypes(Dictionary<INamedTypeSymbol, int> appliedTypes)
		{
			foreach (var type in appliedTypes)
			{
				_appliedTypes.Add(type.Key, type.Value);
			}
		}
	}
}
