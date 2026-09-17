using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public class Given_GeneratedDependencyProperty
{
	private const string AttributeStub = """
		namespace Uno.UI.Xaml
		{
			[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
			internal sealed class GeneratedDependencyPropertyAttribute : System.Attribute
			{
			}
		}
		""";

	[TestMethod]
	public async Task When_Partial_Property_Is_Generated_DependencyProperty()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:TestRepro">
				<Page.Resources>
					<x:Double x:Key="MyDoubleResource">42</x:Double>
					<Style x:Key="MyControlStyle" TargetType="local:MyControl">
						<Setter Property="MyValue" Value="12" />
					</Style>
				</Page.Resources>

				<StackPanel>
					<local:MyControl Style="{StaticResource MyControlStyle}" MyValue="{ThemeResource MyDoubleResource}" />
					<local:MyControl MyValue="{StaticResource MyDoubleResource}" />
					<local:MyControl MyValue="{Binding SomeValue}" />
				</StackPanel>
			</Page>
			""");

		var test = new GeneratedDependencyPropertyTest(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					AttributeStub,
					"""
					using Microsoft.UI.Xaml.Controls;
					using Uno.UI.Xaml;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class MyControl : Control
						{
							[GeneratedDependencyProperty]
							public partial double MyValue { get; set; }

							public partial double MyValue
							{
								get => (double)GetValue(MyValueProperty);
								set => SetValue(MyValueProperty, value);
							}
						}
					}
					""",
				},
			},
		};

		await test.RunAsync();

		// Without the attribute check the Setter fails generation and the resources are assigned once instead of applied.
		test.GeneratedXamlCode.Should().Contain("Property = global::TestRepro.MyControl.MyValueProperty");
		test.GeneratedXamlCode.Should().Contain("global::TestRepro.MyControl.MyValueProperty, \"MyDoubleResource\", isThemeResourceExtension: true");
		test.GeneratedXamlCode.Should().Contain("global::TestRepro.MyControl.MyValueProperty, \"MyDoubleResource\", isThemeResourceExtension: false");
		test.GeneratedXamlCode.Should().MatchRegex(@"SetBinding\(\s*global::TestRepro\.MyControl\.MyValueProperty,");
	}

	[TestMethod]
	public async Task When_Attached_Getter_Is_Generated_DependencyProperty()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:TestRepro">
				<Page.Resources>
					<x:Double x:Key="MyDoubleResource">42</x:Double>
				</Page.Resources>

				<StackPanel>
					<Border local:MyAttached.Offset="{ThemeResource MyDoubleResource}" />
					<Border local:MyAttached.Offset="{Binding SomeValue}" />
				</StackPanel>
			</Page>
			""");

		var test = new GeneratedDependencyPropertyTest(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					AttributeStub,
					"""
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Uno.UI.Xaml;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public static partial class MyAttached
						{
							[GeneratedDependencyProperty]
							public static partial double GetOffset(DependencyObject element);

							public static partial void SetOffset(DependencyObject element, double value);

							public static partial double GetOffset(DependencyObject element) => (double)element.GetValue(OffsetProperty);

							public static partial void SetOffset(DependencyObject element, double value) => element.SetValue(OffsetProperty, value);
						}
					}
					""",
				},
			},
		};

		await test.RunAsync();

		test.GeneratedXamlCode.Should().Contain("global::TestRepro.MyAttached.OffsetProperty, \"MyDoubleResource\", isThemeResourceExtension: true");
		test.GeneratedXamlCode.Should().MatchRegex(@"SetBinding\(\s*global::TestRepro\.MyAttached\.OffsetProperty,");
	}

	[TestMethod]
	public async Task When_Attributed_Property_Is_From_Metadata()
	{
		var xamlFile = new XamlFile(
			"MainPage.xaml",
			"""
			<Page x:Class="TestRepro.MainPage"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:ext="using:ExternalLibrary">
				<StackPanel>
					<ext:ExternalControl MyValue="{Binding SomeValue}" />
				</StackPanel>
			</Page>
			""");

		var test = new GeneratedDependencyPropertyTest(xamlFile)
		{
			// Shaped like UIElement.KeyboardAccelerators in Uno.UI: the attributed CLR property is public, its identifier isn't.
			ReferencedLibrarySource =
				AttributeStub +
				"""

				namespace ExternalLibrary
				{
					public class ExternalControl : Microsoft.UI.Xaml.Controls.Control
					{
						internal static Microsoft.UI.Xaml.DependencyProperty MyValueProperty { get; } = Microsoft.UI.Xaml.DependencyProperty.Register(
							nameof(MyValue),
							typeof(double),
							typeof(ExternalControl),
							new Microsoft.UI.Xaml.PropertyMetadata(0d));

						[Uno.UI.Xaml.GeneratedDependencyProperty]
						public double MyValue
						{
							get => (double)GetValue(MyValueProperty);
							set => SetValue(MyValueProperty, value);
						}
					}
				}
				""",
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}
					}
					""",
				},
			},
		};

		await test.RunAsync();

		// A referenced assembly already exposes its accessible identifiers, so the attribute must not be trusted there.
		test.GeneratedXamlCode.Should().NotContain("MyValueProperty");
		test.GeneratedXamlCode.Should().MatchRegex(@"SetBinding\(\s*""MyValue"",");
	}

	private sealed class GeneratedDependencyPropertyTest : XamlSourceGeneratorVerifier.TestBase
	{
		public GeneratedDependencyPropertyTest(XamlFile xamlFile, [CallerFilePath] string testFilePath = "", [CallerMemberName] string testMethodName = "")
			: base(xamlFile, testFilePath, testMethodName)
		{
			LanguageVersion = LanguageVersion.CSharp13;

			// The generated code is asserted on directly, so no snapshot is involved.
			TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
		}

		public string GeneratedXamlCode { get; private set; } = "";

		/// <summary>
		/// Source of a library compiled separately and referenced as metadata.
		/// </summary>
		public string? ReferencedLibrarySource { get; init; }

		protected override Project ApplyCompilationOptions(Project project)
		{
			project = base.ApplyCompilationOptions(project);

			if (ReferencedLibrarySource is null)
			{
				return project;
			}

			var library = CSharpCompilation.Create(
				"ExternalLibrary",
				[CSharpSyntaxTree.ParseText(ReferencedLibrarySource, new CSharpParseOptions(LanguageVersion))],
				project.MetadataReferences,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			using var image = new MemoryStream();
			var result = library.Emit(image);
			result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));

			return project.AddMetadataReference(MetadataReference.CreateFromImage(image.ToArray()));
		}

		protected override IEnumerable<Type> GetSourceGenerators()
		{
			foreach (var generatorType in base.GetSourceGenerators())
			{
				yield return generatorType;
			}

			yield return typeof(DependencyPropertyIdentifierGenerator);
		}

		protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
		{
			var result = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);

			GeneratedXamlCode = string.Concat(result.compilation
				.SyntaxTrees
				.Where(tree => tree.FilePath.Contains(typeof(XamlCodeGenerator).FullName!))
				.Select(tree => tree.ToString()));

			return result;
		}
	}

	/// <summary>
	/// Stands in for the internal DependencyPropertyGenerator: it only emits the {X}Property identifiers, and does so
	/// from RegisterSourceOutput, so, like the real generator output, they are invisible to the XAML generator.
	/// </summary>
	private sealed class DependencyPropertyIdentifierGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var identifiers = context.SyntaxProvider.ForAttributeWithMetadataName(
				"Uno.UI.Xaml.GeneratedDependencyPropertyAttribute",
				static (node, _) => node is PropertyDeclarationSyntax or MethodDeclarationSyntax,
				static (context, _) => CreateIdentifier(context.TargetSymbol));

			context.RegisterSourceOutput(identifiers, static (context, identifier) => context.AddSource(
				$"{identifier.TypeName}.{identifier.Name}Property.g.cs",
				$$"""
				namespace {{identifier.Namespace}}
				{
					{{identifier.TypeModifiers}}partial class {{identifier.TypeName}}
					{
						public static global::Microsoft.UI.Xaml.DependencyProperty {{identifier.Name}}Property { get; } =
							global::Microsoft.UI.Xaml.DependencyProperty.{{identifier.RegisterMethod}}(
								"{{identifier.Name}}",
								typeof({{identifier.PropertyType}}),
								typeof({{identifier.TypeName}}),
								new global::Microsoft.UI.Xaml.PropertyMetadata(default({{identifier.PropertyType}})));
					}
				}
				"""));
		}

		private static Identifier CreateIdentifier(ISymbol symbol)
		{
			var type = symbol.ContainingType;

			string name;
			ITypeSymbol propertyType;
			string registerMethod;
			if (symbol is IMethodSymbol getter)
			{
				name = getter.Name.Substring("Get".Length);
				propertyType = getter.ReturnType;
				registerMethod = "RegisterAttached";
			}
			else
			{
				name = symbol.Name;
				propertyType = ((IPropertySymbol)symbol).Type;
				registerMethod = "Register";
			}

			return new(
				type.ContainingNamespace.ToDisplayString(),
				type.IsStatic ? "static " : "",
				type.Name,
				name,
				propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				registerMethod);
		}

		private sealed record Identifier(string Namespace, string TypeModifiers, string TypeName, string Name, string PropertyType, string RegisterMethod);
	}
}
