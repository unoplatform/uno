using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Helpers;

public static class RoslynExtensions
{
	public static ProjectInfo GetInfo(this Project project)
		=> ProjectInfo.Create(
			ProjectId.CreateNewId(),
			project.Version,
			project.Name,
			project.AssemblyName,
			project.Language,
			project.FilePath,
			project.OutputFilePath,
			project.CompilationOptions,
			project.ParseOptions,
			[.. project.Documents.Select(GetInfo)],
			project.ProjectReferences,
			project.MetadataReferences,
			project.AnalyzerReferences,
			[.. project.AdditionalDocuments.Select(GetInfo)],
			project.IsSubmission,
			default);

	public static DocumentInfo GetInfo(this Document document)
		=> DocumentInfo.Create(
			document.Id,
			document.Name,
			document.Folders,
			document.SourceCodeKind,
			document.FilePath is null ? null : new FileTextLoader(document.FilePath!, Encoding.UTF8),
			document.FilePath);

	public static DocumentInfo GetInfo(this TextDocument document)
		=> DocumentInfo.Create(
			document.Id,
			document.Name,
			document.Folders,
			default,
			document.FilePath is null ? null : new FileTextLoader(document.FilePath!, Encoding.UTF8),
			document.FilePath);
}
