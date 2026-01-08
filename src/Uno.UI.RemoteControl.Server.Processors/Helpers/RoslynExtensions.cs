using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.RemoteControl.Helpers;

public record WorkspaceData(
	// Options: No effective way to persist them
	// Properties: Provided by the ConfigureServer
	//SolutionData Solution
	SolutionData Solution
);

/// <summary>
/// Represents serializable solution data.
/// </summary>
/// <param name="Id">The solution identifier.</param>
/// <param name="Version">The version stamp of the solution.</param>
/// <param name="FilePath">The file path of the solution file.</param>
/// <param name="Projects">The projects contained in the solution.</param>
/// <param name="AnalyzerReferences">The analyzer references for the solution.</param>
public record SolutionData(
	SolutionId? Id,
	VersionStamp? Version,
	string? FilePath,
	ImmutableArray<ProjectData> Projects,
	ImmutableArray<AnalyzerReferenceData> AnalyzerReferences);

/// <summary>
/// Represents serializable project data.
/// </summary>
/// <param name="Id">The project identifier.</param>
/// <param name="Version">The version stamp.</param>
/// <param name="Name">The name of the project.</param>
/// <param name="AssemblyName">The assembly name.</param>
/// <param name="Language">The programming language.</param>
/// <param name="FilePath">The file path of the project.</param>
/// <param name="OutputFilePath">The output file path.</param>
/// <param name="CompilationOptions">The compilation options.</param>
/// <param name="ParseOptions">The parse options.</param>
/// <param name="Documents">The documents in the project.</param>
/// <param name="ProjectReferences">The project references.</param>
/// <param name="MetadataReferences">The metadata references.</param>
/// <param name="AnalyzerReferences">The analyzer references.</param>
/// <param name="AdditionalDocuments">The additional documents.</param>
/// <param name="IsSubmission">Whether this is a submission project.</param>
public record ProjectData(
	ProjectId? Id,
	VersionStamp? Version,
	string Name,
	string AssemblyName,
	string Language,
	string? FilePath,
	string? OutputFilePath,
	CompilationOptions? CompilationOptions,
	CompilationOutputInfo? CompilationOutputInfo,
	ParseOptions? ParseOptions,
	ImmutableArray<DocumentData> Documents,
	ImmutableArray<ProjectReferenceData> ProjectReferences,
	ImmutableArray<MetadataReferenceData> MetadataReferences,
	ImmutableArray<AnalyzerReferenceData> AnalyzerReferences,
	ImmutableArray<DocumentData> AnalyzerConfigDocuments,
	ImmutableArray<DocumentData> AdditionalDocuments,
	bool IsSubmission
);

/// <summary>
/// Represents serializable document data.
/// </summary>
/// <param name="Id">The document identifier.</param>
/// <param name="Name">The name of the document.</param>
/// <param name="Folders">The folders containing the document.</param>
/// <param name="SourceCodeKind">The kind of source code.</param>
/// <param name="FilePath">The file path of the document.</param>
public record DocumentData(
	DocumentId? Id,
	string Name,
	ImmutableArray<string> Folders,
	SourceCodeKind SourceCodeKind,
	string? FilePath
);

public record ProjectReferenceData(
	ProjectId? ProjectId,
	ImmutableArray<string> Aliases,
	bool EmbedInteropTypes);

public record MetadataReferenceData(
	string FilePath,
	MetadataReferenceProperties Properties);

public record AnalyzerReferenceData(
	string FilePath);

public static class RoslynExtensions
{
	[Flags]
	public enum RoslynInfoOptions
	{
		NoLoader = 1 << 0,
	}

	public static SolutionInfo GetInfo(this Solution solution, RoslynInfoOptions opts = default)
		=> SolutionInfo.Create(
			solution.Id,
			solution.Version,
			solution.FilePath,
			[.. solution.Projects.Select(p => GetInfo(p, opts))],
			solution.AnalyzerReferences);

	public static SolutionInfo GetInfo(this SolutionData solution, IAnalyzerAssemblyLoader analyzerLoader)
	{
		ArgumentNullException.ThrowIfNull(analyzerLoader);
		return SolutionInfo.Create(
			solution.Id ?? SolutionId.CreateNewId(),
			solution.Version ?? VersionStamp.Default,
			solution.FilePath,
			[.. solution.Projects.Select(project => GetInfo(project, analyzerLoader))],
			[.. solution.AnalyzerReferences.Select(reference => CreateAnalyzerReference(reference, analyzerLoader))]);
	}

	public static ProjectInfo GetInfo(this Project project, RoslynInfoOptions opts = default)
		=> ProjectInfo
			.Create(
				project.Id,
				project.Version,
				project.Name,
				project.AssemblyName,
				project.Language,
				project.FilePath,
				project.OutputFilePath,
				project.CompilationOptions,
				project.ParseOptions,
				[.. project.Documents.Select(d => GetInfo(d, opts))],
				project.ProjectReferences,
				project.MetadataReferences,
				project.AnalyzerReferences,
				[.. project.AdditionalDocuments.Select(p => GetInfo(p, opts))],
				project.IsSubmission,
				default)
			.WithCompilationOutputInfo(project.CompilationOutputInfo)
			.WithAnalyzerConfigDocuments(project.AnalyzerConfigDocuments.Select(doc => GetInfo(doc, opts)));

	public static ProjectInfo GetInfo(this ProjectData project, IAnalyzerAssemblyLoader analyzerLoader)
	{
		var projectId = project.Id ?? ProjectId.CreateNewId();
		return ProjectInfo
			.Create(
				projectId,
				project.Version ?? VersionStamp.Default,
				project.Name,
				project.AssemblyName,
				project.Language,
				project.FilePath,
				project.OutputFilePath,
				project.CompilationOptions,
				project.ParseOptions,
				[.. project.Documents.Select(d => GetInfo(d, projectId))],
				[.. project.ProjectReferences.Select(CreateProjectReference)],
				[.. project.MetadataReferences.Select(CreateMetadataReference)],
				[.. project.AnalyzerReferences.Select(reference => CreateAnalyzerReference(reference, analyzerLoader))],
				[.. project.AdditionalDocuments.Select(p => GetInfo(p, projectId))],
				project.IsSubmission,
				default)
			.WithCompilationOutputInfo(project.CompilationOutputInfo ?? new())
			.WithAnalyzerConfigDocuments(project.AnalyzerConfigDocuments.Select(p => GetInfo(p, projectId)));
	}


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

	public static DocumentInfo GetInfo(this DocumentData document, ProjectId projectId)
		=> DocumentInfo.Create(
			document.Id ?? DocumentId.CreateNewId(projectId),
			document.Name,
			document.Folders,
			document.SourceCodeKind,
			document.FilePath is null ? null : new FileTextLoader(document.FilePath!, Encoding.UTF8),
			document.FilePath);

	public static DocumentInfo GetInfo(this Document document, RoslynInfoOptions opts = default)
		=> DocumentInfo.Create(
			document.Id,
			document.Name,
			document.Folders,
			document.SourceCodeKind,
			document.FilePath is null || opts.HasFlag(RoslynInfoOptions.NoLoader) ? null : new FileTextLoader(document.FilePath!, Encoding.UTF8),
			document.FilePath);

	public static DocumentInfo GetInfo(this TextDocument document, RoslynInfoOptions opts = default)
		=> DocumentInfo.Create(
			document.Id,
			document.Name,
			document.Folders,
			default,
			document.FilePath is null || opts.HasFlag(RoslynInfoOptions.NoLoader) ? null : new FileTextLoader(document.FilePath!, Encoding.UTF8),
			document.FilePath);

	private static ProjectReference CreateProjectReference(ProjectReferenceData reference)
		=> new(reference.ProjectId ?? ProjectId.CreateNewId(), reference.Aliases.IsDefault ? ImmutableArray<string>.Empty : reference.Aliases, reference.EmbedInteropTypes);

	private static MetadataReference CreateMetadataReference(MetadataReferenceData reference)
		=> MetadataReference.CreateFromFile(reference.FilePath, reference.Properties);

	private static AnalyzerReference CreateAnalyzerReference(AnalyzerReferenceData reference, IAnalyzerAssemblyLoader analyzerLoader)
	{
		ArgumentNullException.ThrowIfNull(analyzerLoader);
		if (string.IsNullOrWhiteSpace(reference.FilePath))
		{
			throw new InvalidOperationException("Analyzer reference path is missing.");
		}

		return new AnalyzerFileReference(reference.FilePath, analyzerLoader);
	}

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
