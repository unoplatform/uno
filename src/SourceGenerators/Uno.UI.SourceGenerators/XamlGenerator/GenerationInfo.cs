#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class GenerationRunInfo
	{
		private ConcurrentDictionary<string, GenerationRunFileInfo> _fileInfo = new ConcurrentDictionary<string, GenerationRunFileInfo>();

		internal GenerationRunInfo(GenerationRunInfoManager manager, int index)
		{
			Index = index;
			Manager = manager;
		}

		internal GenerationRunInfoManager Manager { get; }

		internal int Index { get; }

		internal GenerationRunFileInfo GetRunFileInfo(string fileId)
			=> _fileInfo.GetOrAdd(fileId, f => new GenerationRunFileInfo(this, f));
	}
}
