#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static partial class XBindExpressionParser
	{
		internal static (string[] properties, bool hasFunction) ParseProperties(string rawFunction, Func<string, bool> isStaticMethod)
		{
			if (!string.IsNullOrEmpty(rawFunction))
			{
				var expression = ParseExpression(rawFunction);
				var v = new Visitor(isStaticMethod);
				v.Visit(expression);

				return (v.IdentifierNames.ToArray(), v.HasMethodInvocation);
			}

			return (Array.Empty<string>(), false);
		}

		// TODO: isRValue feels like a hack so that we generate compilable code when bindings are generated as LValue.
		// However, we could be incorrectly throwing NRE. This should be handled properly.
		internal static (string? MethodDeclaration, string Expression) Rewrite(string contextName, string rawFunction, INamedTypeSymbol? contextTypeSymbol, INamespaceSymbol globalNamespace, bool isRValue, int xBindCounter, Func<string, INamedTypeSymbol?> findType)
		{
			SyntaxNode expression = ParseExpression(rawFunction);

			expression = new Rewriter(contextName, findType).Visit(expression);
			if (isRValue && contextTypeSymbol is not null)
			{
				var nullabilityRewriter = new NullabilityRewriter(contextName, contextTypeSymbol, globalNamespace, xBindCounter);
				nullabilityRewriter.Visit(expression);
				nullabilityRewriter.SetActiveExpressionPropertiesThenCleanup();
				if (!nullabilityRewriter.Failed)
				{
					var methodDeclaration = nullabilityRewriter.MainExpression.ToString();
					return (methodDeclaration, $"TryGetInstance_xBind_{xBindCounter}({contextName}, out var bindResult{xBindCounter}) ? (true, bindResult{xBindCounter}) : (false, default)");
				}
			}

			if (isRValue)
			{
				return (null, $"(true, {expression.ToFullString()})");
			}

			return (null, expression.ToFullString());
		}

		private class Rewriter : CSharpSyntaxRewriter
		{
			private readonly string _contextName;
			private readonly Func<string, INamedTypeSymbol?> _findType;

			public Rewriter(string contextName, Func<string, INamedTypeSymbol?> findType)
			{
				if (string.IsNullOrEmpty(contextName))
				{
					throw new ArgumentException($"'{contextName}' cannot be null or empty", nameof(contextName));
				}

				_contextName = contextName;
				_findType = findType;
			}

			public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				var e = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;

				var methodName = e.Expression.ToFullString();
				if (!methodName.StartsWith("global::", StringComparison.Ordinal) && !Helpers.IsAttachedPropertySyntax(node.Expression).result)
				{
					return e.WithExpression(SyntaxFactory.ParseExpression($"{_contextName}.{methodName}"));
				}

				return e;
			}

			public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				var e = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;
				var isValidParent = !Helpers.IsInsideMethod(node).result && !Helpers.IsInsideMemberAccessExpression(node).result;

				var isParenthesizedExpression = node.Expression is ParenthesizedExpressionSyntax;
				if (isValidParent
					&& !isParenthesizedExpression
					)
				{
					var isPathLessCast = Helpers.IsPathLessCast(node);
					if (isPathLessCast.result)
					{
						return ParseExpression($"({isPathLessCast.expression?.Expression}){_contextName}");
					}

					var isInsideAttachedPropertySyntax = Helpers.IsInsideAttachedPropertySyntax(node);
					if (isInsideAttachedPropertySyntax.result)
					{
						return ParseExpression($"{_contextName}.{isInsideAttachedPropertySyntax.expression?.Expression.ToString().TrimEnd('.')}");
					}
					else
					{
						var expression = e.ToFullString();
						if (expression.StartsWith("global::", StringComparison.Ordinal))
						{
							return e;
						}
						else
						{
							return ParseExpression($"{_contextName}.{expression}");
						}
					}
				}

				var isAttachedPropertySyntax = Helpers.IsAttachedPropertySyntax(node);
				if (isAttachedPropertySyntax.result)
				{
					if (e.Expression is IdentifierNameSyntax)
					{
						if (
							isAttachedPropertySyntax.expression?.ArgumentList.Arguments.FirstOrDefault() is { } property
							&& property.Expression is MemberAccessExpressionSyntax memberAccessExpression
							)
						{
							return ParseExpression($"{GetGlobalizedTypeName(memberAccessExpression.Expression.ToString())}.Get{memberAccessExpression.Name}");
						}
					}
				}

				return e;
			}

			private string GetGlobalizedTypeName(string typeName)
			{
				var typeSymbol = _findType(typeName);
				if (typeSymbol is null)
				{
					return typeName;
				}

				return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			}

			public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
			{
				var isInsideCast = Helpers.IsInsideCast(node);
				var isValidParent =
					(
						!Helpers.IsInsideMethod(node).result
						&& !Helpers.IsInsideMemberAccessExpression(node).result
						&& !isInsideCast.result
					)
					|| Helpers.IsInsideCastWithParentheses(node).result
					|| Helpers.IsInsideCastAsRoot(node).result
					|| Helpers.IsInsideCastAsMethodArgument(node).result;

				if (isValidParent)
				{
					var newIdentifier = node.ToFullString();

					var rawFunction = string.IsNullOrWhiteSpace(newIdentifier)
						? _contextName
						: $"{_contextName}.{newIdentifier}";

					return ParseExpression(rawFunction);

				}

				return base.VisitIdentifierName(node);
			}
		}

		private class NullabilityRewriter : CSharpSyntaxWalker
		{
			private readonly string _contextName;
			private readonly INamedTypeSymbol _contextTypeSymbol;
			private readonly string _contextTypeString;
			private readonly INamespaceSymbol _globalNamespace;
			private readonly int _xBindCounter;
			private readonly StringBuilder _builder = new();
			private XBindExpressionInfo _activeSubexpression;

			private (ISymbol? Symbol, bool IsTopLevelContext) _lastAccessed;

			public bool Failed { get; private set; }

			public bool HasNullable { get; private set; }

			public XBindExpressionInfo MainExpression { get; }

			public NullabilityRewriter(string contextName, INamedTypeSymbol contextTypeSymbol, INamespaceSymbol globalNamespace, int xBindCounter)
			{
				_contextName = contextName;
				_contextTypeSymbol = contextTypeSymbol;
				// TODO: Modify after https://github.com/unoplatform/uno/pull/11531 is merged.
				_contextTypeString = contextTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

				// We need to take globalNamespace from the compilation itself.
				// Walking ContainingNamespaces from contextTypeSymbol or
				// accessing contextTypeSymbol.ContainingModule.GlobalNamespace doesn't work because
				// the INamespaceSymbol we get has NamespaceKind == NamespaceKind.Module and it doesn't have access
				// to all members.
				_globalNamespace = globalNamespace;
				_xBindCounter = xBindCounter;
				_activeSubexpression = new(xBindCounter, _contextTypeString, contextName);
				MainExpression = _activeSubexpression;
			}

			public override void DefaultVisit(SyntaxNode node)
			{
				if (Failed)
				{
					return;
				}

				base.DefaultVisit(node);
			}

			public override void VisitCastExpression(CastExpressionSyntax node)
			{
				if (Failed)
				{
					return;
				}

				_builder.Append($"(({node.Type.ToFullString()})");
				_lastAccessed = (null, false);
				Visit(node.Expression);
				_builder.Append(')');
			}

			public override void VisitLiteralExpression(LiteralExpressionSyntax node)
			{
				_builder.Append(node.ToString());
			}

			public override void VisitArgumentList(ArgumentListSyntax node)
			{
				if (Failed)
				{
					return;
				}

				MainExpression.Arguments = new();

				for (int i = 0; i < node.Arguments.Count; i++)
				{
					_lastAccessed = (null, false);
					var argument = node.Arguments[i];

					var argumentExpression = new XBindExpressionInfo(_xBindCounter, _contextTypeString, _contextName);
					MainExpression.Arguments.Add(argumentExpression);
					_activeSubexpression = argumentExpression;

					VisitArgument(argument);

					SetActiveExpressionPropertiesThenCleanup();
				}
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				if (Failed)
				{
					return;
				}

				Visit(node.Expression);

				SetActiveExpressionPropertiesThenCleanup();

				Visit(node.ArgumentList);
			}

			public override void VisitIdentifierName(IdentifierNameSyntax node)
			{
				if (Failed)
				{
					return;
				}

				if (node.Parent is AliasQualifiedNameSyntax aliasQualifiedName && node == aliasQualifiedName.Alias && node.Identifier.IsKind(SyntaxKind.GlobalKeyword))
				{
					_lastAccessed = (_globalNamespace, false);
					_builder.Append("global::");
					return;
				}

				if (_lastAccessed.Symbol is null && node.Identifier.ValueText == _contextName)
				{
					_lastAccessed = (_contextTypeSymbol, true);
					_builder.Append(_contextName);
					return;
				}

				if (_lastAccessed.Symbol is not (IPropertySymbol or IFieldSymbol or INamespaceOrTypeSymbol))
				{
					Failed = true;
					return;
				}

				(INamespaceOrTypeSymbol previousType, bool mayBeNullAccessed) = _lastAccessed.Symbol switch
				{
					IPropertySymbol property => (property.Type, true),
					IFieldSymbol field => (field.Type, true),
					INamespaceOrTypeSymbol namespaceOrType => (namespaceOrType, false),
					_ => throw new Exception($"Unexpected _lastAccessed symbol '{_lastAccessed.Symbol?.Kind}'."),
				};

				var member = previousType.GetMemberInlcudingBaseTypes(node.Identifier.ValueText);
				if (member is null)
				{
					Failed = true;
					return;
				}

				if (member is INamespaceOrTypeSymbol)
				{
					if (_lastAccessed.Symbol is INamespaceSymbol { IsGlobalNamespace: true })
					{
						_builder.Append(member.Name);
					}
					else
					{
						_builder.Append($".{member.Name}");
					}

					_lastAccessed = (member, false);
					return;
				}

				if (!mayBeNullAccessed || _lastAccessed.IsTopLevelContext || !IsReferenceTypeOrNullableValueType((ITypeSymbol)previousType))
				{
					_builder.Append('.');
				}
				else
				{
					HasNullable = true;
					_builder.Append("?.");
				}

				_builder.Append(member.Name);
				_lastAccessed = (member, false);
			}

			private static bool IsReferenceTypeOrNullableValueType(ITypeSymbol symbol)
			{
				return symbol.IsReferenceType || symbol.IsNullable();
			}

			internal void SetActiveExpressionPropertiesThenCleanup()
			{
				if (_builder.Length == 0)
				{
					return;
				}

				var lastIndexOfNullAccess = LastIndexOfNullAccess();
				if (lastIndexOfNullAccess == -1)
				{
					_activeSubexpression.ExpressionBeforeLastNullAccess = _builder.ToString();
				}
				else
				{
					_activeSubexpression.ExpressionBeforeLastNullAccess = _builder.ToString(0, lastIndexOfNullAccess);
					_activeSubexpression.ExpressionAfterLastNullAccess = _builder.ToString(lastIndexOfNullAccess + "?.".Length, _builder.Length - (lastIndexOfNullAccess + "?.".Length));
				}

				_builder.Clear();
			}

			private int LastIndexOfNullAccess()
			{
				for (int i = _builder.Length - 1; i >= 1; i--)
				{
					if (_builder[i] == '.' && _builder[i - 1] == '?')
					{
						return i - 1;
					}
				}

				return -1;
			}
		}

		class Visitor : CSharpSyntaxWalker
		{
			private readonly Func<string, bool> _isStaticMethod;

			public bool HasMethodInvocation { get; private set; }

			public List<string> IdentifierNames { get; } = new List<string>();

			public Visitor(Func<string, bool> isStaticMethod)
			{
				_isStaticMethod = isStaticMethod;
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);

				HasMethodInvocation = true;

				if (!_isStaticMethod(node.Expression.ToFullString()) && node.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					IdentifierNames.Add(memberAccess.Expression.ToString());
				}
			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				base.VisitMemberAccessExpression(node);

				var isInsideMethod = Helpers.IsInsideMethod(node);
				var isInsideMemberAccess = Helpers.IsInsideMemberAccessExpression(node);
				var isValidParent = !isInsideMethod.result && !isInsideMemberAccess.result;

				if (isValidParent)
				{
					IdentifierNames.Add(node.ToString());
				}
			}

			public override void VisitIdentifierName(IdentifierNameSyntax node)
			{
				base.VisitIdentifierName(node);

				var isInsideMethod = Helpers.IsInsideMethod(node);
				var isInsideMemberAccess = Helpers.IsInsideMemberAccessExpression(node);
				var isValidParent = !isInsideMethod.result && !isInsideMemberAccess.result;

				if (isValidParent)
				{
					IdentifierNames.Add(node.Identifier.Text);
				}
			}
		}

		private static class Helpers
		{
			internal static (bool result, MemberAccessExpressionSyntax? memberAccess) IsInsideMemberAccessExpression(SyntaxNode node)
				=> IsInside(node, n => n as MemberAccessExpressionSyntax);

			internal static (bool result, InvocationExpressionSyntax? expression) IsInsideMethod(SyntaxNode node)
			{
				var currentNode = node.Parent;
				var child = node;

				do
				{
					if (currentNode is InvocationExpressionSyntax invocation
						&& invocation.Expression == child)
					{
						return (true, invocation);
					}

					child = currentNode;
					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCast(SyntaxNode node)
				=> IsInside(node, n => n as CastExpressionSyntax);

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCastWithParentheses(SyntaxNode node)
				=> IsInside(
					node,
					n => n is CastExpressionSyntax cast
						&& cast.Parent is ParenthesizedExpressionSyntax
						&& cast.Expression == node ? cast : null);

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCastAsMethodArgument(SyntaxNode node)
				=> IsInside(
					node,
					n => n is CastExpressionSyntax cast
						&& cast.Parent is ArgumentSyntax
						&& cast.Expression == node ? cast : null);

			internal static (bool result, CastExpressionSyntax? expression) IsInsideCastAsRoot(SyntaxNode node)
				=> IsInside(
					node,
					n => n is CastExpressionSyntax cast
						&& cast.Parent is null
						&& cast.Expression == node ? cast : null);

			internal static (bool result, T? expression) IsInside<T>(SyntaxNode node, Func<SyntaxNode?, T?> predicate) where T : SyntaxNode
			{
				var currentNode = node.Parent;

				do
				{
					if (predicate(currentNode) is { } cast)
					{
						return (true, cast);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

			internal static (bool result, ParenthesizedExpressionSyntax? expression) IsPathLessCast(SyntaxNode node)
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is ArgumentSyntax arg
						&& arg.Expression is ParenthesizedExpressionSyntax expressionSyntax)
					{
						return (true, expressionSyntax);
					}

					if (currentNode is ParenthesizedExpressionSyntax expressionSyntax2
						&& currentNode.Parent is null)
					{
						return (true, expressionSyntax2);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

			internal static (bool result, InvocationExpressionSyntax? expression) IsAttachedPropertySyntax(SyntaxNode node)
			{
				var currentNode = node.Parent;

				if (node.GetText().ToString().EndsWith(".", StringComparison.Ordinal))
				{
					do
					{
						if (currentNode is InvocationExpressionSyntax arg)
						{
							return (true, arg);
						}

						currentNode = currentNode?.Parent;
					}
					while (currentNode != null);
				}

				return (false, null);
			}

			internal static (bool result, InvocationExpressionSyntax? expression) IsInsideAttachedPropertySyntax(SyntaxNode node)
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is InvocationExpressionSyntax arg
						&& arg.Expression is MemberAccessExpressionSyntax memberAccess
						&& memberAccess.ToString().EndsWith(".", StringComparison.Ordinal))
					{
						return (true, arg);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}
		}
	}
}
