#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnoNotImplementedAnalyzer : DiagnosticAnalyzer
	{
		internal const string Title = "Uno type or member is not implemented";
		internal const string MessageFormat = "{0} is not implemented in Uno (https://aka.platform.uno/notimplemented?m={0})";
		internal const string Description = "This member or type is not implemented and will fail when invoked.";
		internal const string Category = "Compatibility";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
			"Uno0001",
#pragma warning restore RS2008 // Enable analyzer release tracking
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description,
			helpLinkUri: "https://aka.platform.uno/notimplemented"
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction(context =>
			{
				var notImplementedSymbol = context.Compilation.GetTypeByMetadataName("Uno.NotImplementedAttribute");
				if (notImplementedSymbol is null)
				{
					return;
				}

				context.RegisterOperationAction(c =>
					AnalyzeOperation(c, notImplementedSymbol)
					, OperationKind.Invocation
					, OperationKind.ObjectCreation
					, OperationKind.FieldReference
					, OperationKind.PropertyReference
					, OperationKind.EventReference
					, OperationKind.TypeOf
					, OperationKind.MethodReference);
			});
		}

		private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol notImplementedSymbol)
		{
			if (IsBindableMetadata(context))
			{
				return;
			}

			var symbol = GetUnoSymbolFromOperation(context.Operation);
			if (symbol != null)
			{
				var directives = GetDirectives(context.Operation.Syntax.SyntaxTree);

				if (HasNotImplementedAttribute(notImplementedSymbol, symbol, directives) ||
					(symbol.ContainingSymbol != null && HasNotImplementedAttribute(notImplementedSymbol, symbol.ContainingSymbol, directives)))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						context.Operation.Syntax.GetLocation(),
						symbol.ToDisplayString()
					);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private ISymbol? GetUnoSymbolFromOperation(IOperation operation)
		{

			ISymbol? symbol = operation switch
			{
				IInvocationOperation invocationOperation => invocationOperation.TargetMethod,
				IObjectCreationOperation objectCreation => objectCreation.Type,
				IFieldReferenceOperation fieldReferenceOperation => fieldReferenceOperation.Field,
				IPropertyReferenceOperation propertyReferenceOperation => propertyReferenceOperation.Property,
				IEventReferenceOperation eventReferenceOperation => eventReferenceOperation.Event,
				ITypeOfOperation typeofOperation => typeofOperation.TypeOperand,
				IMethodReferenceOperation methodReferenceOperation => methodReferenceOperation.Method,
				_ => throw new InvalidOperationException("This code path is unreachable.")
			};


			if (IsUnoSymbol(symbol))
			{
				return symbol;
			}

			return null;
		}

		private static string[] GetDirectives(SyntaxTree tree)
		{
			return tree.Options.PreprocessorSymbolNames.ToArray();
		}

		private static bool HasNotImplementedAttribute(INamedTypeSymbol notImplementedSymbol, ISymbol namedSymbol, string[] directives)
		{
			if (namedSymbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, notImplementedSymbol)) is AttributeData data)
			{
				if (
					data.ConstructorArguments.FirstOrDefault() is TypedConstant constant
					&& constant.Kind != TypedConstantKind.Error)
				{
					Debug.Assert(constant.Kind == TypedConstantKind.Array);

					var notImplementedFlavors = constant.Values.Select(v => v.Value?.ToString()).ToArray();

					return IsPerFlavorAssembly(namedSymbol.ContainingAssembly)
						? IsNotImplementedInFlavor(notImplementedFlavors, directives)
						: IsNotImplementedInSingleBuild(notImplementedFlavors, directives);
				}
				else
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The WinRT-layer assemblies are built once per flavor, and their attribute tokens name those flavors:
		/// __ANDROID__, __IOS__, __TVOS__, __APPLE_UIKIT__, __WASM__, __SKIA__ (desktop) and __NETSTD_REFERENCE__.
		/// </summary>
		private static bool IsPerFlavorAssembly(IAssemblySymbol? assembly)
			=> assembly?.Name is "Uno.WinRT" or "Uno.Foundation" or "Uno.UI.Dispatching";

		private static bool IsNotImplementedInFlavor(string?[] notImplementedFlavors, string[] directives)
		{
			if (GetConsumerFlavors(directives) is { } flavors)
			{
				return notImplementedFlavors.Any(f => flavors.Contains(f));
			}

			// A platform-less library runs on whichever flavor the head deploys, so only report
			// what is missing on both the desktop and WebAssembly flavors.
			return notImplementedFlavors.Contains("__SKIA__") && notImplementedFlavors.Contains("__WASM__");
		}

		private static string[]? GetConsumerFlavors(string[] directives)
		{
			bool Has(string symbol) => directives.Contains(symbol);

			if (Has("__ANDROID__") || Has("ANDROID"))
			{
				return ["__ANDROID__"];
			}
			if (Has("__TVOS__") || Has("TVOS"))
			{
				return ["__TVOS__", "__APPLE_UIKIT__"];
			}
			if (Has("__IOS__") || Has("IOS"))
			{
				return ["__IOS__", "__APPLE_UIKIT__"];
			}
			if (Has("__WASM__") || Has("BROWSERWASM"))
			{
				return ["__WASM__"];
			}
			if (Has("__DESKTOP__") || Has("DESKTOP"))
			{
				return ["__SKIA__"];
			}

			return null;
		}

		/// <summary>
		/// Uno.UI and the libraries built on it compile once, for Skia, so __SKIA__ means "not implemented on any
		/// Uno target". Other tokens are read as the consumer's own conditional compilation symbols.
		/// </summary>
		private static bool IsNotImplementedInSingleBuild(string?[] notImplementedFlavors, string[] directives)
			=> notImplementedFlavors.Any(f => f == "__SKIA__" || directives.Contains(f));

		private static bool IsUnoSymbol(ISymbol? symbol)
		{
			string name = symbol?.ContainingAssembly?.Name ?? "";

			return name.StartsWith("Uno", StringComparison.Ordinal) || name.Equals("TestProject", StringComparison.Ordinal);
		}

		private static bool IsBindableMetadata(OperationAnalysisContext context)
			=> Path.GetFileName(context.Operation.Syntax.SyntaxTree.FilePath) == "BindableMetadata.g.cs";
	}
}
