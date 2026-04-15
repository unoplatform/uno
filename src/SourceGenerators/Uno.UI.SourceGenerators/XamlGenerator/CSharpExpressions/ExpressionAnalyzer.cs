#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Walks the parsed <see cref="SyntaxTree"/> to produce an <see cref="ExpressionAnalysisResult"/>:
/// transformed C# (DataType identifiers prefixed, page-level identifiers replaced with captures),
/// the refresh-set (<see cref="BindingHandler"/> list), and load-time <see cref="LocalCapture"/>s.
/// See <c>contracts/resolution-algorithm.md</c> §Analysis and §Refresh-set.
/// </summary>
internal static class ExpressionAnalyzer
{
	// TODO (T077): extend so identifiers resolving to static types are emitted with global::
	//   qualification and excluded from the refresh set (US4).
	public static ExpressionAnalysisResult Analyze(
		SyntaxTree tree,
		ResolutionScope scope)
	{
		var expression = ExtractExpression(tree);
		if (expression is null)
		{
			return new ExpressionAnalysisResult(
				TransformedCSharp: string.Empty,
				Handlers: System.Array.Empty<BindingHandler>(),
				Captures: System.Array.Empty<LocalCapture>(),
				LeafPropertyType: null,
				IsOneShot: true,
				IsSettable: false);
		}

		var visitor = new AnalyzerVisitor(scope);
		visitor.Visit(expression);

		var transformed = ApplyEdits(expression, visitor.Edits);
		var (isSettable, leafType) = ComputeSettable(expression, scope);

		return new ExpressionAnalysisResult(
			TransformedCSharp: transformed,
			Handlers: visitor.Handlers,
			Captures: visitor.Captures,
			LeafPropertyType: leafType,
			IsOneShot: visitor.Handlers.Count == 0,
			IsSettable: isSettable);
	}

	private static ExpressionSyntax? ExtractExpression(SyntaxTree tree)
	{
		var root = tree.GetRoot();
		var globalStatement = root.ChildNodes().OfType<GlobalStatementSyntax>().FirstOrDefault();
		if (globalStatement?.Statement is ExpressionStatementSyntax exprStatement)
		{
			return exprStatement.Expression;
		}
		return null;
	}

	private static string ApplyEdits(ExpressionSyntax expression, IReadOnlyList<Edit> edits)
	{
		var originalText = expression.ToString();
		if (edits.Count == 0)
		{
			return originalText;
		}

		var baseStart = expression.SpanStart;
		var sorted = edits.OrderByDescending(e => e.Start).ToList();
		var sb = new StringBuilder(originalText);

		foreach (var edit in sorted)
		{
			var localStart = edit.Start - baseStart;
			if (localStart < 0 || localStart + edit.Length > sb.Length)
			{
				continue;
			}
			sb.Remove(localStart, edit.Length);
			sb.Insert(localStart, edit.Replacement);
		}

		return sb.ToString();
	}

	private static (bool IsSettable, ITypeSymbol? LeafType) ComputeSettable(
		ExpressionSyntax expression,
		ResolutionScope scope)
	{
		if (scope.DataType is null || !TryCollectPurePath(expression, out var hops))
		{
			return (false, null);
		}

		ITypeSymbol? current = scope.DataType;
		IPropertySymbol? leafProperty = null;
		var totalHops = hops.Count;

		for (var i = 0; i < totalHops; i++)
		{
			if (current is null)
			{
				return (false, null);
			}

			var property = FindPublicProperty(current, hops[i]);
			if (property is null)
			{
				return (false, null);
			}

			if (i < totalHops - 1 && !IsNotifyingOrDependencyObject(property.Type))
			{
				return (false, null);
			}

			leafProperty = property;
			current = property.Type;
		}

		if (leafProperty is null)
		{
			return (false, null);
		}

		var hasPublicSetter = leafProperty.SetMethod is { DeclaredAccessibility: Accessibility.Public };
		return (hasPublicSetter, leafProperty.Type);
	}

	private static bool TryCollectPurePath(ExpressionSyntax expression, out List<string> hops)
	{
		hops = new List<string>();
		var node = expression;

		var reverse = new List<string>();
		while (node is MemberAccessExpressionSyntax ma && ma.Kind() == SyntaxKind.SimpleMemberAccessExpression)
		{
			if (ma.Name is not IdentifierNameSyntax nameId)
			{
				return false;
			}
			reverse.Add(nameId.Identifier.ValueText);
			node = ma.Expression;
		}

		if (node is not IdentifierNameSyntax rootId)
		{
			return false;
		}

		hops.Add(rootId.Identifier.ValueText);
		for (var i = reverse.Count - 1; i >= 0; i--)
		{
			hops.Add(reverse[i]);
		}
		return true;
	}

	private static IPropertySymbol? FindPublicProperty(ITypeSymbol type, string name)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			foreach (var member in current.GetMembers(name))
			{
				if (member is IPropertySymbol property
					&& !property.IsStatic
					&& property.DeclaredAccessibility == Accessibility.Public)
				{
					return property;
				}
			}
		}
		return null;
	}

	private static bool IsNotifyingOrDependencyObject(ITypeSymbol type)
	{
		foreach (var iface in type.AllInterfaces)
		{
			if (iface.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged")
			{
				return true;
			}
		}
		for (var current = type.BaseType; current is not null; current = current.BaseType)
		{
			var fullName = current.ToDisplayString();
			if (fullName == "Microsoft.UI.Xaml.DependencyObject"
				|| fullName == "Windows.UI.Xaml.DependencyObject"
				|| fullName == "DependencyObject")
			{
				return true;
			}
		}
		return false;
	}

	private readonly struct Edit
	{
		public Edit(int start, int length, string replacement)
		{
			Start = start;
			Length = length;
			Replacement = replacement;
		}

		public int Start { get; }
		public int Length { get; }
		public string Replacement { get; }
	}

	private sealed class AnalyzerVisitor : CSharpSyntaxWalker
	{
		private readonly ResolutionScope _scope;
		private readonly HashSet<string> _capturedNames = new();
		private readonly HashSet<string> _handlerKeys = new();

		public AnalyzerVisitor(ResolutionScope scope)
		{
			_scope = scope;
			Edits = new List<Edit>();
			Handlers = new List<BindingHandler>();
			Captures = new List<LocalCapture>();
		}

		public List<Edit> Edits { get; }
		public List<BindingHandler> Handlers { get; }
		public List<LocalCapture> Captures { get; }

		public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			// Only process at the top of a chain. A MemberAccess that is the .Expression of another
			// MemberAccess is handled when we walk the top.
			if (node.Parent is MemberAccessExpressionSyntax parentMa && parentMa.Expression == node)
			{
				base.VisitMemberAccessExpression(node);
				return;
			}

			// `this.X[.Y...]` — root capture of page member.
			if (TryHandleThisChain(node))
			{
				return;
			}

			// DataType-rooted chain (e.g. User.Address.City).
			if (TryHandleChainFromRootIdentifier(node))
			{
				return;
			}

			base.VisitMemberAccessExpression(node);
		}

		public override void VisitIdentifierName(IdentifierNameSyntax node)
		{
			// Skip member names on the right-hand side of a MemberAccess (they're not roots).
			if (node.Parent is MemberAccessExpressionSyntax ma && ma.Name == node)
			{
				return;
			}

			// Skip argument name-colon identifiers in invocations: Foo(name: value).
			if (node.Parent is NameColonSyntax)
			{
				return;
			}

			HandleBareIdentifier(node);
		}

		private bool TryHandleThisChain(MemberAccessExpressionSyntax topOfChain)
		{
			// Find the innermost MemberAccess in this chain whose .Expression is a ThisExpressionSyntax.
			SyntaxNode current = topOfChain;
			MemberAccessExpressionSyntax? thisAccess = null;
			while (current is MemberAccessExpressionSyntax ma)
			{
				if (ma.Expression is ThisExpressionSyntax)
				{
					thisAccess = ma;
					break;
				}
				current = ma.Expression;
			}

			if (thisAccess is null || thisAccess.Name is not IdentifierNameSyntax memberName)
			{
				return false;
			}

			var identifier = memberName.Identifier.ValueText;
			var captureName = "__capture_" + identifier;
			if (_capturedNames.Add(identifier))
			{
				var resolved = _scope.Resolve(identifier);
				var captureType = ExtractMemberType(resolved.Symbol);
				Captures.Add(new LocalCapture(
					OriginalIdentifier: identifier,
					CaptureVariableName: captureName,
					CaptureInitializer: "this." + identifier,
					Type: captureType!));
			}

			Edits.Add(new Edit(thisAccess.SpanStart, thisAccess.Span.Length, captureName));
			return true;
		}

		private bool TryHandleChainFromRootIdentifier(MemberAccessExpressionSyntax topOfChain)
		{
			// Walk to the root IdentifierNameSyntax.
			SyntaxNode current = topOfChain;
			while (current is MemberAccessExpressionSyntax ma)
			{
				current = ma.Expression;
			}

			if (current is not IdentifierNameSyntax rootId)
			{
				return false;
			}

			var identifier = rootId.Identifier.ValueText;
			var result = _scope.Resolve(identifier);

			if (result.Location == MemberLocation.DataType || result.Location == MemberLocation.Both)
			{
				// Collect hops along the chain.
				var hops = new List<string> { identifier };
				SyntaxNode walker = rootId;
				while (walker.Parent is MemberAccessExpressionSyntax pma
					&& pma.Expression == walker
					&& pma.Name is IdentifierNameSyntax nameId)
				{
					hops.Add(nameId.Identifier.ValueText);
					walker = pma;
				}

				EmitHandlersForChain(hops);
				Edits.Add(new Edit(rootId.SpanStart, rootId.Span.Length, "__source." + identifier));
				return true;
			}

			if (result.Location == MemberLocation.This)
			{
				var captureName = "__capture_" + identifier;
				if (_capturedNames.Add(identifier))
				{
					var captureType = ExtractMemberType(result.Symbol);
					Captures.Add(new LocalCapture(
						OriginalIdentifier: identifier,
						CaptureVariableName: captureName,
						CaptureInitializer: "this." + identifier,
						Type: captureType!));
				}
				Edits.Add(new Edit(rootId.SpanStart, rootId.Span.Length, captureName));
				return true;
			}

			return false;
		}

		private void HandleBareIdentifier(IdentifierNameSyntax node)
		{
			var identifier = node.Identifier.ValueText;
			var result = _scope.Resolve(identifier);

			switch (result.Location)
			{
				case MemberLocation.DataType:
				case MemberLocation.Both:
					EmitHandlersForChain(new List<string> { identifier });
					Edits.Add(new Edit(node.SpanStart, node.Span.Length, "__source." + identifier));
					break;
				case MemberLocation.This:
					var captureName = "__capture_" + identifier;
					if (_capturedNames.Add(identifier))
					{
						var captureType = ExtractMemberType(result.Symbol);
						Captures.Add(new LocalCapture(
							OriginalIdentifier: identifier,
							CaptureVariableName: captureName,
							CaptureInitializer: "this." + identifier,
							Type: captureType!));
					}
					Edits.Add(new Edit(node.SpanStart, node.Span.Length, captureName));
					break;
			}
		}

		private void EmitHandlersForChain(List<string> hops)
		{
			for (var i = 0; i < hops.Count; i++)
			{
				var accessor = BuildAccessor(hops, i);
				var propertyName = hops[i];
				var key = accessor + "\0" + propertyName;
				if (_handlerKeys.Add(key))
				{
					Handlers.Add(new BindingHandler(accessor, propertyName));
				}
			}
		}

		private static string BuildAccessor(List<string> hops, int depth)
		{
			if (depth == 0)
			{
				return "__source => __source";
			}

			var sb = new StringBuilder("__source => __source");
			for (var i = 0; i < depth; i++)
			{
				sb.Append(i == 0 ? "." : "?.");
				sb.Append(hops[i]);
			}
			return sb.ToString();
		}

		private static ITypeSymbol? ExtractMemberType(ISymbol? symbol) => symbol switch
		{
			IPropertySymbol property => property.Type,
			IFieldSymbol field => field.Type,
			_ => null,
		};
	}
}
