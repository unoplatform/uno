#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.SourceGenerators.Internal;

/// <summary>
/// This analyzer is intended to capture <c>IObservableVector&lt;T&gt;</c> implementations that has an <c>Append</c> method without an <c>Add</c> implementation.
/// <c>IObservableVector&lt;T&gt;</c> is an <c>IVector&lt;T&gt;</c> in C++ which is mapped to <c>IList&lt;T&gt;</c> in C#.
/// <c>IVector&lt;T&gt;</c> has <c>Append</c> method, while IList&lt;T&gt; doesn't have an <c>Append</c> method but has <c>Add</c> instead.
/// WinRT projection maps <c>IList&lt;T&gt;.Add</c> to <c>IVector&lt;T&gt;.Append</c> (https://github.com/microsoft/CsWinRT/blob/2b77d8412adb7b62bf8f1265a6d8b8c3169dde58/src/WinRT.Runtime/Projections/IList.net5.cs#L178)
/// So that a call to <c>IList&lt;T&gt;.Add</c> from C# will call the <c>Append</c> implementation in C++.
/// When we are porting controls from WinUI, we keep the method name "Append", but then usages of "Add" will not work as expected.
/// In our case, it's required that we implement or override <c>Add</c> so that it calls <c>Append</c> to simulate what the WinRT projection does.
/// So, this analyzer tries to capture those missing <c>Add</c> methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ObservableVectorAnalyzer : DiagnosticAnalyzer
{
	private static DiagnosticDescriptor _missingAddOverrideDescriptor = new(
		"UnoInternal0001",
		"Implement or override 'Add' to call 'Append'",
		"Type '{0}' should implement or override 'Add' to call 'Append'.",
		"Correctness",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(_missingAddOverrideDescriptor);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			var observableVectorInterface = context.Compilation.GetTypeByMetadataName("Windows.Foundation.Collections.IObservableVector`1");
			var ivectorInterface = context.Compilation.GetTypeByMetadataName("DirectUI.IVector`1");
			if (observableVectorInterface is null && ivectorInterface is null)
			{
				return;
			}

			context.RegisterSymbolAction(context =>
			{
				var type = (INamedTypeSymbol)context.Symbol;
				if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
				{
					return;
				}

				var implementedObservableVectors = GetIVectorImplementations(type, observableVectorInterface, ivectorInterface);
				foreach (var implementedObservableVector in implementedObservableVectors)
				{
					var appendMethod = TryGetMethod(type, "Append", implementedObservableVector.TypeArguments[0]);

					if (appendMethod is not null)
					{
						// If Append is an override, the base class is responsible for overriding/implementing Add.
						if (appendMethod.IsOverride)
						{
							continue;
						}

						var addMethod = TryGetMethod(type, "Add", implementedObservableVector.TypeArguments[0]);
						if (addMethod is null)
						{
							context.ReportDiagnostic(Diagnostic.Create(_missingAddOverrideDescriptor, type.Locations[0], type.Name));
						}
					}
				}

			}, SymbolKind.NamedType);
		});
	}

	private static IEnumerable<INamedTypeSymbol> GetIVectorImplementations(INamedTypeSymbol type, INamedTypeSymbol? observableVectorInterface, INamedTypeSymbol? ivectorInterface)
	{
		return type.AllInterfaces.Where(i =>
			i.OriginalDefinition.Equals(observableVectorInterface, SymbolEqualityComparer.Default) ||
			i.OriginalDefinition.Equals(ivectorInterface, SymbolEqualityComparer.Default));
	}

	private static IMethodSymbol? TryGetMethod(INamedTypeSymbol type, string methodName, ITypeSymbol parameterType)
		=> (IMethodSymbol?)type.GetMembers(methodName)
			.FirstOrDefault(
				m => m is IMethodSymbol method &&
				method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Equals(parameterType, SymbolEqualityComparer.Default));
}
