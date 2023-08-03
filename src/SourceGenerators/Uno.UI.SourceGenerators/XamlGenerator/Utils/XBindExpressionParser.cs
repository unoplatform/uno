#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static partial class XBindExpressionParser
	{
		// TODO: isRValue feels like a hack so that we generate compilable code when bindings are generated as LValue.
		// However, we could be incorrectly throwing NRE. This should be handled properly.
		internal static (string? MethodDeclaration, string Expression, ImmutableArray<string> Properties, bool HasFunction) Rewrite(string contextName, string rawFunction, INamedTypeSymbol? contextTypeSymbol, INamespaceSymbol globalNamespace, bool isRValue, int xBindCounter, Func<string, INamedTypeSymbol?> findType)
		{
			var parser = new CoreParser(rawFunction);
			XBindRoot expression = parser.ParseXBind();
			bool hasFunction = expression is XBindInvocation;

			var builder = new CSharpBuilder(contextName, findType);
			var (csharpExpression, properties) = builder.Build(expression);
			if (isRValue && contextTypeSymbol is not null)
			{
				var nullabilityRewriter = new NullabilityRewriter(contextName, contextTypeSymbol, globalNamespace, xBindCounter);
				// TODO: We should probably avoid using Roslyn at all.
				// We could combine nullability rewriter into CSharpBuilder.
				nullabilityRewriter.Visit(SyntaxFactory.ParseExpression(csharpExpression));
				nullabilityRewriter.SetActiveExpressionPropertiesThenCleanup();
				if (!nullabilityRewriter.Failed)
				{
					var methodDeclaration = nullabilityRewriter.MainExpression.ToString();
					return (methodDeclaration, $"TryGetInstance_xBind_{xBindCounter}({contextName}, out var bindResult{xBindCounter}) ? (true, bindResult{xBindCounter}) : (false, default)", properties, hasFunction);
				}
			}

			if (isRValue)
			{
				return (null, $"(true, {csharpExpression})", properties, hasFunction);
			}

			return (null, csharpExpression, properties, hasFunction);
		}

		private class CSharpBuilder
		{
			private readonly StringBuilder _builder = new();
			private readonly Func<string, INamedTypeSymbol?> _findType;
			private readonly string _contextName;

			public CSharpBuilder(string contextName, Func<string, INamedTypeSymbol?> findType)
			{
				_contextName = contextName;
				_findType = findType;
			}

			private int FirstIndexOf(char c)
			{
				for (int i = 0; i < _builder.Length; i++)
				{
					if (_builder[i] == c)
					{
						return i;
					}
				}

				return -1;
			}

			private int LastIndexOf(char c)
			{
				for (int i = _builder.Length - 1; i >= 0; i--)
				{
					if (_builder[i] == c)
					{
						return i;
					}
				}

				return -1;
			}

			private bool StartsWith(string s, int startIndexToSearchFrom = 0)
			{
				if (startIndexToSearchFrom + s.Length > _builder.Length)
				{
					return false;
				}

				for (int i = 0; i < s.Length; i++)
				{
					if (_builder[startIndexToSearchFrom++] != s[i])
					{
						return false;
					}
				}

				return true;
			}

			public (string CSharpExpression, ImmutableArray<string> Properties) Build(XBindRoot root)
			{
				if (root is XBindInvocation invocation)
				{
					var propertiesBuilder = ImmutableArray.CreateBuilder<string>();

					BuildPath(invocation.Path);

					// If we have an invocation on the form of context.SomePath.Method(...)
					// Then we add "SomePath" to the propertiesBuilder
					var firstIndexOf = FirstIndexOf('.');
					var lastIndexOf = LastIndexOf('.');
					if (firstIndexOf != lastIndexOf && StartsWith(_contextName))
					{
						propertiesBuilder.Add(_builder.ToString(firstIndexOf + 1, lastIndexOf - firstIndexOf - 1));
					}

					_builder.Append('(');
					for (int i = 0; i < invocation.Arguments.Length; i++)
					{
						var oldLength = _builder.Length;
						var argument = invocation.Arguments[i];
						if (argument is XBindPathArgument pathArgument)
						{
							BuildPath(pathArgument.Path);
							var newLength = _builder.Length;
							var argPath = _builder.ToString(oldLength, newLength - oldLength);
							if (argPath.StartsWith($"{_contextName}.", StringComparison.Ordinal))
							{
								propertiesBuilder.Add(argPath.Substring(_contextName.Length + 1));
							}
							else if (argPath is not ("null" or "true" or "false")) // TODO: Consider special nodes for these.
							{
								propertiesBuilder.Add(argPath);
							}
						}
						else if (argument is XBindLiteralArgument literalArgument)
						{
							_builder.Append(literalArgument.LiteralArgument);
						}
						else
						{
							throw new Exception("Unexpected XBindArgument type.");
						}

						if (i < invocation.Arguments.Length - 1)
						{
							_builder.Append(", ");
						}
					}
					_builder.Append(')');
					return (_builder.ToString(), propertiesBuilder.ToImmutableArray());
				}
				else if (root is XBindPath path)
				{
					BuildPath(path);
					var csharpPath = _builder.ToString();
					if (csharpPath.StartsWith($"{_contextName}.", StringComparison.Ordinal))
					{
						return (csharpPath, ImmutableArray.Create(csharpPath.Substring(_contextName.Length + 1)));
					}

					return (csharpPath, ImmutableArray.Create(csharpPath));
				}
				else if (root is null)
				{
					return (_contextName, ImmutableArray<string>.Empty);
				}

				throw new Exception("Unexpected XBindRoot");
			}

			public void BuildPath(XBindPath path)
			{
				if (path is XBindMemberAccess memberAccess)
				{
					BuildPath(memberAccess.Path);
					_builder.Append('.');
					_builder.Append(memberAccess.Identifier.IdentifierText);
				}
				else if (path is XBindIndexerAccess indexerAccess)
				{
					BuildPath(indexerAccess.Path);
					_builder.Append('[');
					_builder.Append(indexerAccess.Index);
					_builder.Append(']');
				}
				else if (path is XBindIdentifier identifier)
				{
					if (!identifier.IdentifierText.StartsWith("global::", StringComparison.Ordinal) &&
						identifier.IdentifierText is not ("null" or "true" or "false"))
					{
						_builder.Append(_contextName);
						_builder.Append('.');
					}

					_builder.Append(identifier.IdentifierText);
				}
				else if (path is XBindAttachedPropertyAccess attachedPropertyAccess)
				{
					if (attachedPropertyAccess.PropertyClass is XBindIdentifier attachedClassIdentifier)
					{
						_builder.Append(GetGlobalizedTypeName(attachedClassIdentifier.IdentifierText));
					}
					else
					{
						BuildPath(attachedPropertyAccess.PropertyClass);
					}

					_builder.Append(".Get");
					_builder.Append(attachedPropertyAccess.PropertyName.IdentifierText);
					_builder.Append('(');
					BuildPath(attachedPropertyAccess.Member);
					_builder.Append(')');
				}
				else if (path is XBindParenthesizedExpression parenthesizedExpression)
				{
					_builder.Append('(');
					BuildPath(parenthesizedExpression.Expression);
					_builder.Append(')');

					if (parenthesizedExpression.IsPathlessCast)
					{
						_builder.Append(_contextName);
					}
				}
				else if (path is XBindCast xBindCast)
				{
					_builder.Append('(');
					BuildPath(xBindCast.Type);
					_builder.Append(')');
					BuildPath(xBindCast.Expression);
				}
				else
				{
					throw new Exception("Unexpected XBindPath");
				}
			}

			private string GetGlobalizedTypeName(string typeName)
			{
				var typeSymbol = _findType(typeName);
				if (typeSymbol is null)
				{
					return typeName;
				}

				return typeSymbol.GetFullyQualifiedTypeIncludingGlobal();
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
				_contextTypeString = contextTypeSymbol.GetFullyQualifiedTypeIncludingGlobal();

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

			public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
			{
				if (Failed)
				{
					return;
				}

				if (node.ArgumentList.Arguments.Count != 1 ||
					node.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax index)
				{
					throw new Exception("x:Bind indexing expects a single literal argument");
				}

				Visit(node.Expression);

				var lastType = _lastAccessed.Symbol switch
				{
					IPropertySymbol property => property.Type,
					IFieldSymbol field => field.Type,
					_ => throw new Exception("Only properties and fields can be indexed.")
				};

				var expectedIndexerParameterType = index is LiteralExpressionSyntax indexLiteral && indexLiteral.IsKind(SyntaxKind.StringLiteralExpression)
					? SpecialType.System_String
					: SpecialType.System_Int32;
				var indexer = lastType.GetMemberInlcudingBaseTypes(expectedIndexerParameterType, (m, arg) => m is IPropertySymbol { IsIndexer: true } p && p.Parameters[0].Type.SpecialType == arg);
				if (indexer is not null)
				{
					_lastAccessed = (indexer, IsTopLevelContext: false);
				}
				else
				{
					throw new Exception("Type is unsupported for indexing.");
				}

				_builder.Append('[');

				VisitLiteralExpression(index);

				_builder.Append(']');
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
	}
}
