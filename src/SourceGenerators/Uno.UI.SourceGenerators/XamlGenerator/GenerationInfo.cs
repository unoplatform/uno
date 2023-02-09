#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record GenerationRunInfo(GenerationRunInfoManager Manager, int AdditionalFilesHash)
	{
		private ConcurrentDictionary<string, GenerationRunFileInfo> _fileInfo = new();

		internal GenerationRunFileInfo GetRunFileInfo(string fileId)
			=> _fileInfo.GetOrAdd(fileId, f => new GenerationRunFileInfo(this, f));

		/// <summary>
		/// Generates an identifier string for the current run
		/// </summary>
		public string ToRunIdentifierString()
			=> AdditionalFilesHash.ToString("X8", CultureInfo.InvariantCulture);
	}
}
