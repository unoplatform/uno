#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Final codegen IR consumed by the existing x:Bind emitter (simple paths),
/// the new compound helper emitter, the direct-assignment emitter (one-shot),
/// or the inline event-handler emitter (lambdas). See data-model.md §9 and
/// contracts/generated-binding-shapes.md.
/// </summary>
internal abstract record LoweredExpression;

/// <summary>
/// Shape §1/§2/§11: equivalent to <c>{x:Bind Path, Mode=TwoWay|OneWay}</c>.
/// The existing x:Bind emitter consumes <see cref="Path"/> directly.
/// </summary>
internal sealed record SimplePathBinding(
	string Path,
	SimplePathBindingMode Mode,
	DataContextSource DataContextSource) : LoweredExpression;

internal enum SimplePathBindingMode
{
	OneWay,
	TwoWay,
}

internal enum DataContextSource
{
	This,
	DataType,
}

/// <summary>
/// Shape §3/§4/§6/§7/§8/§13: compound expression with a synthesized
/// <c>__xcs_Expr_NNN</c> helper method and an INPC-driven refresh set.
/// </summary>
internal sealed record CompoundBinding(
	string HelperMethodName,
	string HelperMethodBody,
	IReadOnlyList<BindingHandler> Handlers,
	ITypeSymbol? LeafType,
	IReadOnlyList<LocalCapture> Captures) : LoweredExpression;

/// <summary>
/// Shape §5 (one-shot) and §11 (forced-this single capture): direct property
/// assignment at load time. No <c>Binding</c> is constructed; the author opts
/// out of reactive refresh.
/// </summary>
internal sealed record DirectAssignment(
	string CSharpExpression,
	ITypeSymbol? LeafType,
	IReadOnlyList<LocalCapture> Captures) : LoweredExpression;

/// <summary>
/// Shape §9/§10: inline event handler lambda.
/// </summary>
internal sealed record EventHandlerLowered(
	string HandlerMethodName,
	string HandlerMethodBody,
	ITypeSymbol EventDelegateType) : LoweredExpression;
