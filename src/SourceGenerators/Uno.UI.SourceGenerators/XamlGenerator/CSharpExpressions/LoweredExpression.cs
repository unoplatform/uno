#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Final codegen IR consumed by the existing x:Bind emitter (simple paths)
/// or by the new inline event-handler emitter (lambdas). See data-model.md §9.
/// </summary>
internal abstract record LoweredExpression;

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

internal sealed record CompoundBinding(
	string HelperMethodName,
	string HelperMethodBody,
	IReadOnlyList<BindingHandler> Handlers,
	ITypeSymbol LeafType,
	IReadOnlyList<LocalCapture> Captures) : LoweredExpression;

internal sealed record EventHandlerLowered(
	string HandlerMethodName,
	string HandlerMethodBody,
	ITypeSymbol EventDelegateType) : LoweredExpression;
