#nullable enable

using System.Collections.Concurrent;
using System.Globalization;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record GenerationRunInfo(GenerationRunInfoManager Manager, string ProjectFile, string TargetFramework, int AdditionalFilesHash)
	{
		private readonly ConcurrentDictionary<string, GenerationRunFileInfo> _fileInfo = new();

		internal GenerationRunFileInfo GetRunFileInfo(string fileId)
			=> _fileInfo.GetOrAdd(fileId, f => new GenerationRunFileInfo(this, f));

		/// <summary>
		/// Generates an identifier string for the current run
		/// </summary>
		public string ToRunIdentifierString()
			=> AdditionalFilesHash.ToString("X8", CultureInfo.InvariantCulture);
	}
}
