#if !NETFRAMEWORK

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.Roslyn;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	public partial class XamlCodeGenerator : ISourceGenerator
	{
		private void DumpXamlSourceGeneratorState(GeneratorExecutionContext context, List<KeyValuePair<string, SourceText>> generatedSources)
		{
			var tracingFolder = context.GetMSBuildPropertyValue("XamlSourceGeneratorTracingFolder");

			if (tracingFolder != string.Empty)
			{
				try
				{
					var basePath = Path.Combine(tracingFolder, MakeRunId());
					var sourcesPath = Path.Combine(basePath, "sources");

					Directory.CreateDirectory(sourcesPath);

					DumpCommandLine(basePath);

					DumpMSBuildProperties(basePath, context);
					DumpMSBuildItems(basePath, context);

					DumpGeneratedSourceFiles(sourcesPath, generatedSources);
				}
				catch
				{
				}
			}
		}

		private static long _instanceId;

		private string MakeRunId()
		{
			var instanceId = Interlocked.Increment(ref _instanceId);

			var process = Process.GetCurrentProcess();

			return $"{DateTime.Now:yyyyMMdd_HHmmss_fff}-{process.Id}-{process.ProcessName}-{instanceId}";
		}

		private void DumpCommandLine(string basePath) => File.WriteAllText(Path.Combine(basePath, "CommandLine.txt"), Environment.CommandLine);

		private void DumpMSBuildProperties(string basePath, GeneratorExecutionContext context)
		{
			var projectDirectory = context.GetMSBuildPropertyValue("MSBuildProjectDirectory");
			var projectName = context.GetMSBuildPropertyValue("MSBuildProjectName");
			var intermediateOutputPath = context.GetMSBuildPropertyValue("IntermediateOutputPath");

			var editorConfigPath = Path.Combine(projectDirectory, intermediateOutputPath, $"{projectName}.GeneratedMSBuildEditorConfig.editorconfig");

			if (File.Exists(editorConfigPath))
			{
				File.Copy(editorConfigPath, Path.Combine(basePath, Path.GetFileName(editorConfigPath)));
			}
		}

		private void DumpMSBuildItems(string basePath, GeneratorExecutionContext context)
		{
			var items =
				context
					.Compilation
					.SyntaxTrees
						.Select(t => t.FilePath)
						.Concat(context.AdditionalFiles.Select(f => f.Path))
						.OrderBy(f => f);

			File.WriteAllLines(Path.Combine(basePath, "MSBuildItems.txt"), items);
		}

		private void DumpGeneratedSourceFiles(string sourcesPath, List<KeyValuePair<string, SourceText>> generatedSources)
		{
			foreach (var sourceFile in generatedSources)
			{
				File.WriteAllText(Path.Combine(sourcesPath, $"{sourceFile.Key}.cs"), sourceFile.Value.ToString());
			}
		}
	}
}
#endif
