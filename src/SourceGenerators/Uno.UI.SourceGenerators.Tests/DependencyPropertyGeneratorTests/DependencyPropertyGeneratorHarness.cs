using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.UI.SourceGenerators.DependencyObject;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

/// <summary>
/// Runs <see cref="DependencyPropertyGenerator"/> through a <see cref="CSharpGeneratorDriver"/>, so tests can assert on
/// generated sources, generator diagnostics, the final compilation and incremental step states.
/// </summary>
internal static partial class DependencyPropertyGeneratorHarness
{
	public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp13);

	/// <summary>
	/// The referenced Uno.WinUI 5.0.118 package still ships the old attribute shape, so tests declare the current one.
	/// </summary>
	public const string AttributeSource = """
		#nullable disable
		namespace Uno.UI.Xaml
		{
			[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
			internal sealed class GeneratedDependencyPropertyAttribute : global::System.Attribute
			{
				public global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions Options { get; set; }
				public object DefaultValue { get; set; }
				public bool CoerceCallback { get; set; }
				public bool ChangedCallback { get; set; }
				public bool LocalCache { get; set; } = true;
				public global::System.Type AttachedBackingFieldOwner { get; set; }
				public string ChangedCallbackName { get; set; }
			}
		}
		""";

	/// <summary>
	/// DependencyObject is an interface in the referenced package, so tests derive from a class implementing it.
	/// </summary>
	public const string TestDependencyObjectSource = """
		#nullable disable
		namespace TestHelpers
		{
			public class TestDependencyObject : global::Microsoft.UI.Xaml.DependencyObject
			{
				public global::Windows.UI.Core.CoreDispatcher Dispatcher { get; }
				public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; }
				public object GetValue(global::Microsoft.UI.Xaml.DependencyProperty dp) => null;
				public void SetValue(global::Microsoft.UI.Xaml.DependencyProperty dp, object value) { }
				public void ClearValue(global::Microsoft.UI.Xaml.DependencyProperty dp) { }
				public object ReadLocalValue(global::Microsoft.UI.Xaml.DependencyProperty dp) => null;
				public object GetAnimationBaseValue(global::Microsoft.UI.Xaml.DependencyProperty dp) => null;
				public long RegisterPropertyChangedCallback(global::Microsoft.UI.Xaml.DependencyProperty dp, global::Microsoft.UI.Xaml.DependencyPropertyChangedCallback callback) => 0;
				public void UnregisterPropertyChangedCallback(global::Microsoft.UI.Xaml.DependencyProperty dp, long token) { }
			}
		}
		""";

	/// <summary>
	/// A stand-in for Uno.UI's internal boxes, which are inaccessible in the referenced package.
	/// </summary>
	public const string BoxesSource = """
		#nullable disable
		namespace Uno.UI.Helpers.Boxes
		{
			internal static class BoolBoxes
			{
				public static readonly object True = true;
				public static readonly object False = false;
			}

			internal static class IntBoxes
			{
				public static readonly object NegativeOne = -1;
				public static readonly object Zero = 0;
				public static readonly object One = 1;
			}

			internal static class DoubleBoxes
			{
				public static readonly object Zero = 0.0d;
				public static readonly object One = 1.0d;
			}

			internal static class Boxer
			{
				public static object Box(bool value) => value ? BoolBoxes.True : BoolBoxes.False;

				public static object Box(int value) => value switch
				{
					-1 => IntBoxes.NegativeOne,
					0 => IntBoxes.Zero,
					1 => IntBoxes.One,
					_ => value,
				};

				public static object Box(double value) => global::System.BitConverter.DoubleToInt64Bits(value) switch
				{
					0 => DoubleBoxes.Zero,
					0x3FF0000000000000 => DoubleBoxes.One,
					_ => value,
				};
			}
		}
		""";

	private static readonly Lazy<Task<ImmutableArray<MetadataReference>>> s_references = new(
		() => _Dotnet.Current.WithUnoPackage().ResolveAsync(LanguageNames.CSharp, CancellationToken.None));

	/// <summary>
	/// Compiles the sources (after the attribute and the test DependencyObject) and runs the generator.
	/// </summary>
	public static Task<DependencyPropertyGeneratorRun> RunAsync(params string[] sources)
		=> RunCoreAsync([AttributeSource, TestDependencyObjectSource, .. sources]);

	/// <summary>
	/// Same as <see cref="RunAsync"/>, with the <see cref="BoxesSource"/> stub in the compilation.
	/// </summary>
	public static Task<DependencyPropertyGeneratorRun> RunWithBoxesAsync(params string[] sources)
		=> RunCoreAsync([AttributeSource, TestDependencyObjectSource, BoxesSource, .. sources]);

	private static async Task<DependencyPropertyGeneratorRun> RunCoreAsync(string[] sources)
	{
		var trees = sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, path: $"/0/Test{index}.cs"));
		var compilation = CSharpCompilation.Create("TestProject", trees, await s_references.Value, CreateCompilationOptions());

		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[new DependencyPropertyGenerator().AsSourceGenerator()],
			parseOptions: ParseOptions,
			driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

		return DependencyPropertyGeneratorRun.Create(driver, compilation);
	}

	private static CSharpCompilationOptions CreateCompilationOptions()
	{
		var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
			.WithMetadataImportOptions(MetadataImportOptions.All);

		// Same as CSharpSourceGeneratorVerifier.IgnoreAccessibility: generated code uses internal Uno APIs of the package.
		var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
		topLevelBinderFlagsProperty!.SetValue(options, (uint)1 << 22);

		return options;
	}

	public static string NormalizeLineEndings(string code) => code.ReplaceLineEndings("\n");

	public static string NormalizeWhitespace(string code) => Whitespace().Replace(code, " ").Trim();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}

internal sealed class DependencyPropertyGeneratorRun
{
	private DependencyPropertyGeneratorRun(GeneratorDriver driver, CSharpCompilation inputCompilation, Compilation outputCompilation, ImmutableArray<Diagnostic> generatorDiagnostics)
	{
		Driver = driver;
		InputCompilation = inputCompilation;
		OutputCompilation = outputCompilation;
		GeneratorDiagnostics = generatorDiagnostics;
	}

	public GeneratorDriver Driver { get; }

	public CSharpCompilation InputCompilation { get; }

	public Compilation OutputCompilation { get; }

	public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

	public GeneratorRunResult RunResult => Driver.GetRunResult().Results.Single();

	public IEnumerable<string> HintNames => RunResult.GeneratedSources.Select(s => s.HintName);

	public string AllSources => string.Join(Environment.NewLine, RunResult.GeneratedSources.Select(s => $"// ==== {s.HintName}{Environment.NewLine}{s.SourceText}"));

	public static DependencyPropertyGeneratorRun Create(GeneratorDriver driver, CSharpCompilation compilation)
	{
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
		return new(driver, compilation, outputCompilation, diagnostics);
	}

	/// <summary>
	/// Replaces one source and runs the same driver again, so incremental state is kept.
	/// </summary>
	public DependencyPropertyGeneratorRun RerunWithReplacedSource(string oldSource, string newSource)
	{
		var oldTree = InputCompilation.SyntaxTrees.Single(t => t.GetText().ToString() == oldSource);
		var newTree = CSharpSyntaxTree.ParseText(newSource, DependencyPropertyGeneratorHarness.ParseOptions, path: oldTree.FilePath);
		return Create(Driver, InputCompilation.ReplaceSyntaxTree(oldTree, newTree));
	}

	public string Source(string hintName)
	{
		var generated = RunResult.GeneratedSources.Where(s => s.HintName == hintName).ToArray();
		generated.Should().ContainSingle($"'{hintName}' should be generated, got: {string.Join(", ", HintNames)}");
		return generated[0].SourceText.ToString();
	}

	/// <summary>
	/// Asserts that the generator reported nothing, the compilation has no errors and the generated code has no warnings.
	/// </summary>
	public DependencyPropertyGeneratorRun ShouldSucceed()
	{
		var generatedTrees = OutputCompilation.SyntaxTrees.Except(InputCompilation.SyntaxTrees).ToHashSet();
		var problems = GeneratorDiagnostics
			.Concat(OutputCompilation.GetDiagnostics().Where(d =>
				d.Severity == DiagnosticSeverity.Error ||
				(d.Severity == DiagnosticSeverity.Warning && d.Location.SourceTree is { } tree && generatedTrees.Contains(tree))))
			.ToArray();

		problems.Should().BeEmpty($"the generated code should compile cleanly:{Environment.NewLine}{AllSources}");
		return this;
	}

	/// <summary>
	/// Asserts that the compilation has unresolved types, that the generator reported nothing, and that the type still
	/// declares the identifiers, which is what the WinAppSDK sync tool looks for.
	/// </summary>
	public DependencyPropertyGeneratorRun ShouldTolerateUnresolvedTypes(string typeMetadataName, params string[] identifierNames)
	{
		InputCompilation.GetDiagnostics().Should().Contain(d => d.Id == "CS0246" || d.Id == "CS0103", "the test should use unresolved types");
		GeneratorDiagnostics.Should().BeEmpty($"the generator reported: {string.Join(Environment.NewLine, GeneratorDiagnostics)}");

		var type = OutputCompilation.GetTypeByMetadataName(typeMetadataName);
		type.Should().NotBeNull();
		foreach (var identifierName in identifierNames)
		{
			type!.GetMembers(identifierName).OfType<IPropertySymbol>().Should().ContainSingle(p => p.IsStatic, $"the generated source is:{Environment.NewLine}{AllSources}");
		}

		return this;
	}

	public DependencyPropertyGeneratorRun ShouldGenerate(string hintName, string expected)
	{
		DependencyPropertyGeneratorHarness.NormalizeLineEndings(Source(hintName))
			.Should().Be(DependencyPropertyGeneratorHarness.NormalizeLineEndings(expected));
		return this;
	}

	public DependencyPropertyGeneratorRun ShouldContain(string hintName, params string[] snippets)
	{
		var source = Source(hintName);
		var normalized = DependencyPropertyGeneratorHarness.NormalizeWhitespace(source);
		foreach (var snippet in snippets)
		{
			normalized.Should().Contain(DependencyPropertyGeneratorHarness.NormalizeWhitespace(snippet), $"the generated source is:{Environment.NewLine}{source}");
		}

		return this;
	}

	public DependencyPropertyGeneratorRun ShouldNotContain(string hintName, params string[] snippets)
	{
		var source = Source(hintName);
		var normalized = DependencyPropertyGeneratorHarness.NormalizeWhitespace(source);
		foreach (var snippet in snippets)
		{
			normalized.Should().NotContain(DependencyPropertyGeneratorHarness.NormalizeWhitespace(snippet), $"the generated source is:{Environment.NewLine}{source}");
		}

		return this;
	}

	/// <summary>
	/// Asserts the generator reported exactly one diagnostic, with the given id, and returns it.
	/// </summary>
	public Diagnostic ShouldReportSingle(string id)
	{
		GeneratorDiagnostics.Should().ContainSingle($"the generator reported: {string.Join(Environment.NewLine, GeneratorDiagnostics)}");
		var diagnostic = GeneratorDiagnostics[0];
		diagnostic.Id.Should().Be(id, diagnostic.ToString());
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		return diagnostic;
	}

	public async Task<ImmutableArray<Diagnostic>> GetGeneratedCodeAnalyzerDiagnosticsAsync(string analyzerTypeName)
	{
		var analyzerType = typeof(DependencyPropertyGenerator).Assembly.GetType(analyzerTypeName, throwOnError: true)!;
		var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType, nonPublic: true)!;
		var generatedTrees = OutputCompilation.SyntaxTrees.Except(InputCompilation.SyntaxTrees).ToHashSet();

		var diagnostics = await OutputCompilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
		return diagnostics.Where(d => d.Location.SourceTree is { } tree && generatedTrees.Contains(tree)).ToImmutableArray();
	}

	public IEnumerable<IncrementalStepRunReason> StepReasons(string trackingName)
		=> RunResult.TrackedSteps.TryGetValue(trackingName, out var steps)
			? steps.SelectMany(step => step.Outputs).Select(output => output.Reason)
			: throw new InvalidOperationException($"No step named '{trackingName}', got: {string.Join(", ", RunResult.TrackedSteps.Keys)}");

	public IEnumerable<IncrementalStepRunReason> SourceOutputReasons
		=> RunResult.TrackedOutputSteps.TryGetValue("SourceOutput", out var steps)
			? steps.SelectMany(step => step.Outputs).Select(output => output.Reason)
			: [];
}
