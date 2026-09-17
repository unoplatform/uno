using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.UI.SourceGenerators.Internal;

namespace Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests;

internal static class BoxingAnalyzerHarness
{
	public const string BoxingDiagnosticId = "UnoInternal0002";

	private const string BoxesSource = """
		namespace Uno.UI.Helpers
		{
			internal static class Boxes
			{
				public static class BoolBoxes
				{
					public static readonly object BoxedTrue = true;
					public static readonly object BoxedFalse = false;
				}

				public static class IntegerBoxes
				{
					public static readonly object NegativeOne = -1;
					public static readonly object Zero = 0;
					public static readonly object One = 1;
				}

				public static class DoubleBoxes
				{
					public static readonly object Zero = 0.0d;
					public static readonly object One = 1.0d;
				}

				public static object Box(bool value) => value ? BoolBoxes.BoxedTrue : BoolBoxes.BoxedFalse;

				public static object Box(int value) => value;

				public static object Box(double value) => value;

				public static object Box(global::Uno.UI.Xaml.RoutedEventFlag value) => value;
			}
		}

		namespace Uno.UI.Xaml
		{
			[System.Flags]
			public enum RoutedEventFlag : ulong
			{
				None = 0,
				PointerPressed = 1UL << 0,
				PointerReleased = 1UL << 1,
			}
		}

		namespace Microsoft.UI.Xaml
		{
			public sealed class DependencyProperty
			{
			}
		}
		""";

	// Resolved from the running framework rather than downloaded, so these tests work offline.
	private static readonly ImmutableArray<MetadataReference> s_references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
		.Split(Path.PathSeparator)
		.Where(path => Path.GetFileName(path) is "System.Private.CoreLib.dll" or "System.Runtime.dll")
		.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
		.ToImmutableArray();

	public static Document CreateDocument(string source, params string[] preprocessorSymbols)
	{
		AdhocWorkspace workspace = new();
		var project = workspace.AddProject(ProjectInfo.Create(
			ProjectId.CreateNewId(),
			VersionStamp.Create(),
			name: "Test",
			assemblyName: "Test",
			LanguageNames.CSharp,
			compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
			parseOptions: new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
			metadataReferences: s_references));

		workspace.AddDocument(project.Id, "Boxes.cs", SourceText.From(BoxesSource));
		return workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));
	}

	public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document)
	{
		var compilation = (await document.Project.GetCompilationAsync())!;
		compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

		var tree = await document.GetSyntaxTreeAsync();
		var diagnostics = await compilation
			.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BoxingDiagnosticAnalyzer()))
			.GetAnalyzerDiagnosticsAsync();

		return diagnostics.Where(d => d.Location.SourceTree == tree).ToImmutableArray();
	}

	public static async Task<Document> ApplyBoxingFixAsync(Document document)
		=> await ApplyBoxingFixAsync(document, (await GetDiagnosticsAsync(document)).Should().ContainSingle(d => d.Id == BoxingDiagnosticId).Subject);

	/// <summary>
	/// Runs the fix for a diagnostic the analyzer would not report, to check the fix guards itself.
	/// </summary>
	public static async Task<Document> ApplyBoxingFixAtAsync(Document document, string expression)
	{
		var text = (await document.GetTextAsync()).ToString();
		var span = new TextSpan(text.LastIndexOf(expression, StringComparison.Ordinal), expression.Length);
		var descriptor = new DiagnosticDescriptor(BoxingDiagnosticId, "title", "message", "Performance", DiagnosticSeverity.Warning, isEnabledByDefault: true);

		return await ApplyBoxingFixAsync(document, Diagnostic.Create(descriptor, Location.Create((await document.GetSyntaxTreeAsync())!, span)));
	}

	private static async Task<Document> ApplyBoxingFixAsync(Document document, Diagnostic diagnostic)
	{
		List<CodeAction> actions = new();
		CodeFixContext context = new(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
		await new BoxingCodeFixProvider().RegisterCodeFixesAsync(context);

		var operations = await actions.Should().ContainSingle().Subject.GetOperationsAsync(CancellationToken.None);
		return operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
	}
}
