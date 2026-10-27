using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

/// <summary>
/// Issue #25527: source generator errors are suppressed for the emit so a broken generator input does not
/// freeze hot reload, but an error located in a XAML file means that page was generated up to the error
/// only. Such an error fails the pass instead of applying the truncated page.
/// </summary>
[TestClass]
public sealed class Given_HotReloadManager_GeneratorErrors
{
	private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(30);

	private const string Xaml = "/work/App/MainPage.xaml";
	private const string CodeBehind = "/work/App/MainPage.xaml.cs";
	private const string Resw = "/work/App/Strings/Resources.resw";

	private const string ValidXaml = "<Page><TextBlock Text=\"Second\" /></Page>";
	private const string EditedValidXaml = "<Page><TextBlock Text=\"Third\" /></Page>";
	private const string BrokenXaml = "<Page><TextBlockText=\"Second\" /></Page> error UXAML0001";
	private const string ValidResw = "<root />";

	// The real generator reports a .resw it cannot parse with no location.
	private const string BrokenResw = "<root error UXAML0003 nolocation";

	[TestMethod]
	[Description(
		"#25527: a XAML edit the generator cannot parse fails the pass with its diagnostic and sends no update, " +
		"instead of applying the truncated page as a Success.")]
	public async Task When_EditedXamlHasGeneratorError_Then_Failed_AndNothingEmitted()
	{
		using var scenario = new Scenario();
		scenario.Set(Xaml, BrokenXaml);

		await scenario.Edit(Xaml);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Failed);
		scenario.EmitCount.Should().Be(0, "the truncated generated page must not be emitted");
		scenario.LastUpdate.Deltas.Should().BeEmpty();
		scenario.LastUpdate.Diagnostics.Should().ContainSingle(d => d.Id == "UXAML0001");
		scenario.Harness.Reporter.Outputs.Should().Contain(o => o.Contains("blocked by 1 source generator error(s) in MainPage.xaml", StringComparison.Ordinal));
	}

	[TestMethod]
	[Description(
		"#25527: a XAML error under a code other than UXAML0001 blocks the same way: what blocks is where " +
		"the error is, not its code.")]
	public async Task When_XamlErrorHasAnotherCode_Then_Failed_AndNothingEmitted()
	{
		using var scenario = new Scenario();
		scenario.Set(Xaml, "<Page><Bad /></Page> error UXAML0005");

		await scenario.Edit(Xaml);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Failed);
		scenario.EmitCount.Should().Be(0);
		scenario.LastUpdate.Diagnostics.Should().ContainSingle(d => d.Id == "UXAML0005");
	}

	[TestMethod]
	[Description(
		"#25527: while a XAML error is pending, an edit to another file of the same project still fails. " +
		"Otherwise the truncated page would be applied by the next unrelated edit.")]
	public async Task When_XamlErrorPending_AndOtherFileEdited_Then_Failed_AndNothingEmitted()
	{
		using var scenario = new Scenario();
		scenario.Set(Xaml, BrokenXaml);

		await scenario.Edit(Xaml);
		await scenario.Edit(CodeBehind);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Failed);
		scenario.EmitCount.Should().Be(0);
		scenario.LastUpdate.Diagnostics.Should().ContainSingle(d => d.Id == "UXAML0001");
	}

	[TestMethod]
	[Description(
		"A generator error with no location (a .resw it cannot parse) stays suppressed, even in the file the " +
		"pass edited: the edit applies, and the error is reported on the operation.")]
	public async Task When_EditedReswHasGeneratorError_Then_Applies_WithTheError()
	{
		using var scenario = new Scenario();
		scenario.Set(Resw, BrokenResw);

		await scenario.Edit(Resw);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Success);
		scenario.EmitCount.Should().Be(1);
		scenario.LastUpdate.Diagnostics.Should().ContainSingle(d => d.Id == "UXAML0003");
	}

	[TestMethod]
	[Description("While a .resw is broken, a valid XAML edit still applies.")]
	public async Task When_ReswBroken_AndXamlEdited_Then_Applies()
	{
		using var scenario = new Scenario();
		scenario.Set(Resw, BrokenResw);
		scenario.Set(Xaml, EditedValidXaml);

		await scenario.Edit(Xaml);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Success);
		scenario.EmitCount.Should().Be(1);
		scenario.LastUpdate.Deltas.Should().ContainSingle();
		scenario.LastUpdate.Diagnostics.Should().ContainSingle(d => d.Id == "UXAML0003");
	}

	[TestMethod]
	[Description(
		"A generator error with no location stays suppressed whatever its code (the XAML generator reports its " +
		"own crash as UXAML0001 with no location), so it cannot freeze hot reload for every edit.")]
	public async Task When_GeneratorErrorHasNoLocation_Then_Applies()
	{
		using var scenario = new Scenario();
		scenario.Set(Resw, "<root error UXAML0001 nolocation");

		await scenario.Edit(CodeBehind);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Success);
		scenario.EmitCount.Should().Be(1);
		scenario.LastUpdate.Diagnostics.Should().ContainSingle(d => d.Id == "UXAML0001", "a suppressed error is still reported");
	}

	[TestMethod]
	[Description(
		"With a XAML error and a suppressed .resw error both present, the pass is blocked and the console does " +
		"not also say it is continuing despite the suppressed one.")]
	public async Task When_XamlErrorAndReswError_Then_Failed_WithoutContinuingMessage()
	{
		using var scenario = new Scenario();
		scenario.Set(Resw, BrokenResw);
		scenario.Set(Xaml, BrokenXaml);

		await scenario.Edit(Xaml);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Failed);
		scenario.EmitCount.Should().Be(0);
		scenario.LastUpdate.Diagnostics.Select(d => d.Id).Should().BeEquivalentTo(["UXAML0001", "UXAML0003"]);
		scenario.Harness.Reporter.Outputs.Should().NotContain(o => o.Contains("continuing despite", StringComparison.Ordinal));
		scenario.Harness.Reporter.Outputs.Should().NotContain(o => o.Contains("metadata updates", StringComparison.Ordinal));
	}

	[TestMethod]
	[Description("#25527: once the XAML is fixed, the next pass applies normally.")]
	public async Task When_XamlErrorFixed_Then_EditApplies()
	{
		using var scenario = new Scenario();
		scenario.Set(Xaml, BrokenXaml);

		await scenario.Edit(Xaml);
		scenario.Set(Xaml, ValidXaml);
		await scenario.Edit(Xaml);

		scenario.LastResult.Should().Be(HotReloadOperationResult.Success);
		scenario.EmitCount.Should().Be(1);
	}

	/// <summary>
	/// A project holding a XAML page, its code-behind and a .resw, built by an in-memory generator that
	/// reports an error for any input containing <c>error &lt;ID&gt;</c>, located in that input unless the
	/// input also contains <c>nolocation</c>.
	/// The emitter is stubbed: it counts calls and returns one empty update.
	/// </summary>
	private sealed class Scenario : IDisposable
	{
		private readonly Dictionary<string, string> _content = new()
		{
			[Xaml] = ValidXaml,
			[Resw] = ValidResw,
		};
		private int _emitCount;
		private int _pass;

		public Scenario()
		{
			Harness = new HotReloadManagerHarness(
				_ =>
				{
					Interlocked.Increment(ref _emitCount);
					return Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([HotReloadManagerHarness.CreateEmptyUpdate()], []));
				},
				onUpdate: BuildSolution);
		}

		public HotReloadManagerHarness Harness { get; }

		public int EmitCount => Volatile.Read(ref _emitCount);

		public HotReloadOperationResult? LastResult => Harness.Tracker.Last?.Result;

		public HotReloadUpdate LastUpdate => Harness.Handler.Calls[^1].Update;

		public void Set(string file, string content)
			=> _content[file] = content;

		public Task Edit(string file)
			=> Harness.Manager.ProcessFileChanges(Task.FromResult(ImmutableHashSet.Create(file)), CancellationToken.None).WaitAsync(_testTimeout);

		public void Dispose()
			=> Harness.Dispose();

		private SolutionUpdateResult BuildSolution(Solution solution)
		{
			var appId = solution.ProjectIds[0];
			solution = Clear(solution, appId)
				.AddDocument(DocumentId.CreateNewId(appId), "MainPage.xaml.cs", $"public partial class MainPage {{ public int Pass => {++_pass}; }}", filePath: CodeBehind)
				.AddAdditionalDocument(DocumentId.CreateNewId(appId), "MainPage.xaml", SourceText.From(_content[Xaml]), filePath: Xaml)
				.AddAdditionalDocument(DocumentId.CreateNewId(appId), "Resources.resw", SourceText.From(_content[Resw]), filePath: Resw);

			return new SolutionUpdateResult(solution, ChangeSet.IgnoreAll([]));
		}

		private static Solution Clear(Solution solution, ProjectId projectId)
		{
			var project = solution.GetProject(projectId)!;
			foreach (var document in project.Documents)
			{
				solution = solution.RemoveDocument(document.Id);
			}
			foreach (var document in project.AdditionalDocuments)
			{
				solution = solution.RemoveAdditionalDocument(document.Id);
			}

			return solution.WithProjectAnalyzerReferences(projectId, [new GeneratorReference(new ErrorReportingGenerator().AsSourceGenerator())]);
		}
	}

	private sealed class ErrorReportingGenerator : IIncrementalGenerator
	{
		private const string Marker = "error ";

		public void Initialize(IncrementalGeneratorInitializationContext context)
			=> context.RegisterSourceOutput(context.AdditionalTextsProvider, static (output, file) =>
			{
				var content = file.GetText()?.ToString() ?? "";
				var index = content.IndexOf(Marker, StringComparison.Ordinal);
				if (index < 0)
				{
					return;
				}

				var id = content.Substring(index + Marker.Length, "UXAML0000".Length);
				var location = content.Contains("nolocation", StringComparison.Ordinal) ? Location.None : Location.Create(file.Path, default, default);
				var descriptor = new DiagnosticDescriptor(id, "Generator error", "Generator error in {0}", "Test", DiagnosticSeverity.Error, isEnabledByDefault: true);
				output.ReportDiagnostic(Diagnostic.Create(descriptor, location, Path.GetFileName(file.Path)));
			});
	}

	private sealed class GeneratorReference(ISourceGenerator generator) : AnalyzerReference
	{
		public override string FullPath => "";

		public override object Id => nameof(GeneratorReference);

		public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => [];

		public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => [];

		public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => [generator];
	}
}
