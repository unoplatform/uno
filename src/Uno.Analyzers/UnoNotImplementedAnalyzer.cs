#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnoNotImplementedAnalyzer : DiagnosticAnalyzer
	{
		internal const string Title = "Uno type or member is not implemented";
		internal const string MessageFormat = "{0} is not implemented in Uno";
		internal const string Description = "This member or type is not implemented and will fail when invoked.";
		internal const string Category = "Compatibility";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			"Uno0001",
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(context =>
			{
				var notImplementedSymbol = context.Compilation.GetTypeByMetadataName("Uno.NotImplementedAttribute");
				if (notImplementedSymbol is null)
				{
					return;
				}

				context.RegisterOperationAction(c => AnalyzeOperation(c, notImplementedSymbol), OperationKind.Invocation, OperationKind.ObjectCreation, OperationKind.FieldReference, OperationKind.PropertyReference);
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
				var directives = GetDirectives(context.Operation.Syntax.SyntaxTree, context.CancellationToken);

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
			
			ISymbol symbol = operation switch
			{
				IInvocationOperation invocationOperation => invocationOperation.TargetMethod,
				IObjectCreationOperation objectCreation => objectCreation.Type,
				IFieldReferenceOperation fieldReferenceOperation => fieldReferenceOperation.Field,
				IPropertyReferenceOperation propertyReferenceOperation => propertyReferenceOperation.Property,
				_ => throw new InvalidOperationException("This code path is unreachable.")
			};


			if (IsUnoSymbol(symbol))
			{
				return symbol;
			}

			return null;
		}

		private string[] GetDirectives(SyntaxTree tree, CancellationToken cancellationToken)
		{
			var directives = tree.Options.PreprocessorSymbolNames.ToArray() ?? Array.Empty<string>();

			if (directives.Length == 0)
			{
				// This case is only used during tests where explicit #define statements are
				// present at the top of the file. In common cases, PreprocessorSymbolNames is
				// not empty.
				if (tree.GetRoot(cancellationToken).GetFirstDirective() is DefineDirectiveTriviaSyntax directive)
				{
					directives = new[] { directive.Name.Text };
				}
			}

			return directives;
		}

		private static bool HasNotImplementedAttribute(INamedTypeSymbol notImplementedSymbol, ISymbol namedSymbol, string[] directives)
		{
			if (namedSymbol.GetAttributes().FirstOrDefault(a => Equals(a.AttributeClass, notImplementedSymbol)) is AttributeData data)
			{
				if (
					data.ConstructorArguments.FirstOrDefault() is TypedConstant constant
					&& constant.Kind != TypedConstantKind.Error)
				{
					Debug.Assert(constant.Kind == TypedConstantKind.Array);

					var notImplementedPlatforms = constant.Values.Select(v => v.Value?.ToString()).ToArray();

					if (directives.Contains("UNO_REFERENCE_API")
						&& !directives.Contains("__SKIA__")
						&& !directives.Contains("__WASM__"))
					{
						// Uno reference API is a special case where if a member or symbol
						// is implementer for either __SKIA__ or __WASM__, the member is considered
						// implemented. The code may be running in either environments, and we cannot
						// statically determine if a member will be available.
						return notImplementedPlatforms.Any(p => p == "__SKIA__")
							&& notImplementedPlatforms.Any(p => p == "__WASM__");
					}
					else
					{
						return notImplementedPlatforms.Any(d => directives.Contains(d));
					}
				}
				else
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsUnoSymbol(ISymbol symbol)
		{
			string name = symbol?.ContainingAssembly?.Name ?? "";

			return name.StartsWith("Uno", StringComparison.Ordinal) || name.Equals("TestProject", StringComparison.Ordinal);
		}

		private static bool IsBindableMetadata(OperationAnalysisContext context)
			=> Path.GetFileName(context.Operation.Syntax.SyntaxTree.FilePath) == "BindableMetadata.g.cs";
	}
}
