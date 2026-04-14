#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Produces a <see cref="LoweredExpression"/> suitable for the existing emission path
/// (<c>x:Bind</c> back-end for simple paths; a new <c>__xcs_Expr_NNN</c> helper for compounds;
/// a synthesized event handler for lambdas). See <c>contracts/generated-binding-shapes.md</c>.
/// </summary>
internal static class ExpressionLowering
{
	// TODO (T036): simple-path variant — lowers to the same call as the existing x:Bind emitter;
	//   reuse XBindExpressionParser without modification. Two-way vs one-way per inference rules.
	// TODO (T037): compound variant — emits __xcs_Expr_NNN helper on the page partial and wires
	//   Binding { Mode=OneWay, Properties=refreshSet, CompiledSource=... }.
	// TODO (T038): emit UNO2012 (two-way → one-way downgrade) when target DP has
	//   BindsTwoWayByDefault=true AND expression is not settable.
	// TODO (T039): emit UNO2011 (one-shot) and direct-assignment (no Binding) when Handlers is empty.
	// TODO (T049): EventHandler variant — emit __xcs_EventHandler_NNN on the page partial matching
	//   the event's delegate signature; pass author-chosen parameter names through.
	// TODO (T067): {this.Foo} → capture/page-source variant; {.Foo} → DataType-bound variant (US3).
	// TODO (T078): Formatting static types → one-shot direct-assignment form (US4).
	public static LoweredExpression Lower(
		XamlExpressionAttributeValue classifiedValue,
		ExpressionAnalysisResult analysis,
		string helperMethodName)
	{
		_ = classifiedValue;
		_ = analysis;
		_ = helperMethodName;
		return new CompoundBinding(
			HelperMethodName: helperMethodName,
			HelperMethodBody: "/* TODO Phase 3 lowering */",
			Handlers: System.Array.Empty<BindingHandler>(),
			LeafType: null!,
			Captures: System.Array.Empty<LocalCapture>());
	}
}
