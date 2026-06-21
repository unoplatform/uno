using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
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
			[.. solution.Projects.Select(p => p.GetInfo(opts))],
			solution.AnalyzerReferences);

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
				[.. project.Documents.Select(d => d.GetInfo(opts))],
				project.ProjectReferences,
				project.MetadataReferences,
				project.AnalyzerReferences,
				[.. project.AdditionalDocuments.Select(p => p.GetInfo(opts))],
				project.IsSubmission,
				default)
			.WithCompilationOutputInfo(project.CompilationOutputInfo)
			.WithAnalyzerConfigDocuments(project.AnalyzerConfigDocuments.Select(doc => doc.GetInfo(opts)));

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
}
