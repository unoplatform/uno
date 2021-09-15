#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.NonShippingAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnoStringDependencyPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Uno0002_Internal";

        private static readonly string s_title = "String dependency properties (in *most* cases) shouldn't have null default value.";
		private static readonly string s_messageFormat = "Review dependency property and make sure whether it should be null or not.";

        private const string Category = "Reliability";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, s_title, s_messageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(context =>
			{
				var dependencyPropertyType = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.DependencyProperty");
				if (dependencyPropertyType is null)
				{
					return;
				}

				var registerMethods = dependencyPropertyType.GetMembers("Register").Where(s => s is IMethodSymbol method && method.Parameters.Any(p => p.Name == "propertyType"));
				var registerAttachedMethods = dependencyPropertyType.GetMembers("RegisterAttached").Where(s => s is IMethodSymbol method && method.Parameters.Any(p => p.Name == "propertyType"));
				var targetMethods = registerMethods.Concat(registerAttachedMethods);

				context.RegisterOperationAction(context =>
				{
					var operation = (IInvocationOperation)context.Operation;
					if (targetMethods.Any(m => m.Equals(operation.TargetMethod, SymbolEqualityComparer.Default)) &&
						operation.Arguments.Single(a => a.Parameter.Name == "propertyType").Value is ITypeOfOperation typeOfOperation &&
						typeOfOperation.TypeOperand.SpecialType == SpecialType.System_String)
					{
						var metadataArgumentOperation = operation.Arguments.Single(a => a.Parameter.Name == "typeMetadata");
						if (metadataArgumentOperation.Value is IObjectCreationOperation objectCreationOperation)
						{
							var defaultValueArgumentOperation = objectCreationOperation.Arguments.SingleOrDefault(a => a.Parameter.Name == "defaultValue");
							if (ShouldReportDiagnostic(defaultValueArgumentOperation))
							{
								context.ReportDiagnostic(Diagnostic.Create(Rule, metadataArgumentOperation.Syntax.GetLocation()));
							}
						}
					}
				}, OperationKind.Invocation);
			});
        }

		private static bool ShouldReportDiagnostic(IArgumentOperation? defaultValueArgumentOperation)
		{
			if (defaultValueArgumentOperation is null)
			{
				// A constructor that doesn't accept a default value is used. This will default to null default value.
				return true;
			}

			if (!defaultValueArgumentOperation.ConstantValue.HasValue)
			{
				// Not a constant value, e.g: a method call or a field reference like string.Empty.
				return false;
			}

			// Report a diagnostic if a null constant value is specified.
			return defaultValueArgumentOperation.ConstantValue.Value is null;
		}
	}
}
