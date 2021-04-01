#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static partial class XBindExpressionParser
	{
		internal static (string[] properties, bool hasFunction) ParseProperties(string rawFunction, Func<string, bool> isStaticMethod)
		{
			if (!string.IsNullOrEmpty(rawFunction))
			{
				var csu = ParseCompilationUnit(
					$"class __Temp {{ private Func<object> __prop => {rawFunction} }}");

				if (csu.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().FirstOrDefault() is ArrowExpressionClauseSyntax arrow)
				{
					var v = new Visitor(isStaticMethod);
					v.Visit(arrow);

					return (v.IdentifierNames.ToArray(), v.HasMethodInvocation);
				}
			}

			return (new string[0], false);
		}

		internal static string Rewrite(string contextName, string rawFunction, Func<string, bool> isStaticMethod)
		{
			var csu = ParseCompilationUnit(
				$"class __Temp {{ private Func<object> __prop => {rawFunction}; }}");

			var csuRewritten = new Rewriter(contextName, isStaticMethod).Visit(csu);

			return csuRewritten
				.DescendantNodes()
				.OfType<ArrowExpressionClauseSyntax>()
				.First()
				.Expression
				.ToFullString();
		}

		class Rewriter : CSharpSyntaxRewriter
		{
			private readonly string _contextName;
			private readonly Func<string, bool> _isStaticMember;

			public Rewriter(string contextName, Func<string, bool> isStaticMember)
			{
				_contextName = contextName;
				_isStaticMember = isStaticMember;
			}

			public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				var e = base.VisitInvocationExpression(node);

				var isParentMemberStatic = node.Expression switch
				{
					MemberAccessExpressionSyntax ma => _isStaticMember(ma.Expression.ToFullString()),
					IdentifierNameSyntax ins => _isStaticMember(ins.ToFullString()),
					_ => false
				};

				var isValidParent = !Helpers.IsInsideMethod(node).result
					&& !Helpers.IsInsideMemberAccessExpression(node).result
					&& !Helpers.IsInsideMemberAccessExpression(node.Expression).result;

				if (isValidParent && !_isStaticMember(node.Expression.ToFullString()) && !isParentMemberStatic)
				{
					if (e is InvocationExpressionSyntax newSyntax)
					{
						var methodName = newSyntax.Expression.ToFullString();
						var arguments = newSyntax.ArgumentList.ToFullString();
						var output = ParseCompilationUnit($"class __Temp {{ private Func<object> __prop => {ContextBuilder}{methodName}{arguments}; }}");

						var o2 = output.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().First().Expression;
						return o2;
					}
					else
					{
						throw new Exception();
					}
				}
				else
				{
					return e;
				}
			}

			private string ContextBuilder
				=> string.IsNullOrEmpty(_contextName) ? "" : _contextName + ".";

			public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				var e = base.VisitMemberAccessExpression(node);
				var isValidParent = !Helpers.IsInsideMethod(node).result && !Helpers.IsInsideMemberAccessExpression(node).result;
				var isParentMemberStatic = node.Expression is MemberAccessExpressionSyntax m && _isStaticMember(m.ToFullString());

				if (e!= null && isValidParent && !_isStaticMember(node.Expression.ToFullString()) && !isParentMemberStatic)
				{
					var expression = e.ToFullString();
					var contextBuilder = _isStaticMember(expression) ? "" : ContextBuilder;
					var output = ParseCompilationUnit($"class __Temp {{ private Func<object> __prop => {contextBuilder}{expression}; }}");

					var o2 = output.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().First().Expression;
					return o2;
				}
				else
				{
					return e;
				}
			}

			public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
			{
				var isInsideCast = Helpers.IsInsideCast(node);
				var isValidParent = !Helpers.IsInsideMethod(node).result
					&& !Helpers.IsInsideMemberAccessExpression(node).result
					&& !isInsideCast.result;

				if (isValidParent && !_isStaticMember(node.ToFullString()))
				{
					var newIdentifier = node.ToFullString();

					var rawFunction = string.IsNullOrWhiteSpace(newIdentifier)
						? $"class __Temp {{ private Func<object> __prop => {_contextName}; }}"
						: $"class __Temp {{ private Func<object> __prop => {ContextBuilder}{newIdentifier}; }}";

					var output = ParseCompilationUnit(rawFunction);

					var o2 = output.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().First().Expression;
					return o2;
				}
				else
				{
					return base.VisitIdentifierName(node);
				}
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
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is MemberAccessExpressionSyntax memberAccess)
					{
						return (true, memberAccess);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}

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
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is CastExpressionSyntax cast)
					{
						return (true, cast);
					}

					currentNode = currentNode?.Parent;
				}
				while (currentNode != null);

				return (false, null);
			}
		}
	}
}
