#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record GenerationRunInfo(GenerationRunInfoManager Manager, int Index, int AdditionalFilesHash)
	{
		private ConcurrentDictionary<string, GenerationRunFileInfo> _fileInfo = new();

		internal GenerationRunFileInfo GetRunFileInfo(string fileId)
			=> _fileInfo.GetOrAdd(fileId, f => new GenerationRunFileInfo(this, f));
	}
}
