using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AvoidMultipleGettersAnalyzer : DiagnosticAnalyzer
{
	private static DiagnosticDescriptor _descriptor = new("UnoMultipleGetValue", "Avoid duplicate DP access", "Avoid duplicate DP access", "Performance", DiagnosticSeverity.Error, isEnabledByDefault: true);
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(_descriptor);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterOperationBlockAction(context =>
		{
			foreach (var operationBlock in context.OperationBlocks)
			{
				var cfg = context.GetControlFlowGraph(operationBlock);
				var entryBlock = cfg.Blocks[0];
				// This is not a good implementation. It only handle the case where the duplicate access is in the same BasicBlock.
				// There are more cases that are currently not handled.
				foreach (var block in cfg.Blocks)
				{
					if (block.Kind != Microsoft.CodeAnalysis.FlowAnalysis.BasicBlockKind.Block)
					{
						continue;
					}

					var accessedProperties = new HashSet<(IPropertySymbol, string)>();
					var operations = block.Operations.SelectMany(op => op.DescendantsAndSelf()).OfType<IPropertyReferenceOperation>();
					foreach (var propertyReference in operations)
					{
						if (propertyReference.Parent is IAssignmentOperation or INameOfOperation)
						{
							continue;
						}

						var property = propertyReference.Property;
						var dps = property.ContainingType.GetMembers($"{property.Name}Property");
						var isDependencyProperty = dps.Length > 0;
						if (isDependencyProperty)
						{
							var dp = dps[0];
							if (dp.GetAttributes().Any(a => a.AttributeClass?.Name == "GeneratedDependencyPropertyAttribute"))
							{
								return;
							}

							if (!accessedProperties.Add((property, propertyReference.Instance?.Syntax.ToString() ?? "")))
							{
								context.ReportDiagnostic(Diagnostic.Create(_descriptor, propertyReference.Syntax.GetLocation()));
							}
						}
					}
				}
			}
		});
	}
}
