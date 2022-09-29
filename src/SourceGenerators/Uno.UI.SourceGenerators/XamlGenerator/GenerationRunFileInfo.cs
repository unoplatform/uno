using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class GenerationRunFileInfo
	{
		private string _fileId;
		private Dictionary<INamedTypeSymbol, int> _appliedTypes = new Dictionary<INamedTypeSymbol, int>();

		public GenerationRunFileInfo(GenerationRunInfo runInfo,string fileId)
		{
			_fileId = fileId;
			RunInfo = runInfo;
		}

		internal GenerationRunInfo RunInfo { get; }

		internal string? ComponentCode { get; set; }

		internal IReadOnlyDictionary<INamedTypeSymbol, int> AppliedTypes => _appliedTypes;

		internal void SetAppliedTypes(Dictionary<INamedTypeSymbol, int> appliedTypes)
		{
			foreach(var type in appliedTypes)
			{
				_appliedTypes.Add(type.Key, type.Value);
			}
		}
	}
}
