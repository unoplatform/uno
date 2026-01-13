using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.RemoteControl.Helpers;

public static partial class RoslynExtensions
{
	public static SolutionData GetData(this Solution solution)
		=> new(
			solution.Id,
			solution.Version,
			solution.FilePath,
			[.. solution.Projects.Select(GetData)],
			[.. solution.AnalyzerReferences.Select(GetData)]);

	public static ProjectData GetData(this Project project)
		=> new(
			project.Id,
			project.Version,
			project.Name,
			project.AssemblyName,
			project.Language,
			project.FilePath,
			project.OutputFilePath,
			project.CompilationOptions,
			project.CompilationOutputInfo,
			project.ParseOptions,
			[.. project.Documents.Select(GetData)],
			[.. project.ProjectReferences.Select(GetData)],
			[.. project.MetadataReferences.Select(GetData)],
			[.. project.AnalyzerReferences.Select(GetData)],
			[.. project.AnalyzerConfigDocuments.Select(GetData)],
			[.. project.AdditionalDocuments.Select(GetData)],
			project.IsSubmission);

	public static DocumentData GetData(this Document document)
		=> new(
			document.Id,
			document.Name,
			[.. document.Folders],
			document.SourceCodeKind,
			document.FilePath);

	public static DocumentData GetData(this TextDocument document)
		=> new(
			document.Id,
			document.Name,
			[.. document.Folders],
			default,
			document.FilePath);

	private static ProjectReferenceData GetData(ProjectReference reference)
		=> new(reference.ProjectId, reference.Aliases, reference.EmbedInteropTypes);

	private static MetadataReferenceData GetData(MetadataReference reference)
	{
		if (reference is not PortableExecutableReference { FilePath: { Length: > 0 } filePath })
		{
			throw new InvalidOperationException("Only PortableExecutableReference with valid file path are supported.");
		}

		return new MetadataReferenceData(filePath, reference.Properties);
	}

	private static AnalyzerReferenceData GetData(AnalyzerReference reference)
	{
		if (reference is not AnalyzerFileReference { FullPath: { Length: > 0 } filePath })
		{
			throw new InvalidOperationException("Only AnalyzerFileReference with valid file path are supported.");
		}

		return new AnalyzerReferenceData(filePath);
	}
}
