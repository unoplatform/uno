using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static class XBindExpressionParser
	{
		internal static string[] ParseProperties(string rawFunction)
		{
			if (!string.IsNullOrEmpty(rawFunction))
			{
				var csu = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseCompilationUnit(
					$"class __Temp {{ private Func<object> __prop => {rawFunction} }}");

				if (csu.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().FirstOrDefault() is ArrowExpressionClauseSyntax arrow)
				{
					var v = new Visitor();
					v.Visit(arrow);

					return v.IdentifierNames.ToArray();
				}
			}

			return new string[0];
		}

		class Visitor : CSharpSyntaxWalker
		{
			public List<string> IdentifierNames { get; } = new List<string>();

			public Visitor()
			{
			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				base.VisitMemberAccessExpression(node);

				var isValidParent = !IsInsideMethod(node) && !IsInsideMemberAccessExpression(node); ;

				if (isValidParent)
				{
					IdentifierNames.Add(node.ToString());
				}
			}

			public override void VisitIdentifierName(IdentifierNameSyntax node)
			{
				base.VisitIdentifierName(node);

				var isValidParent = !IsInsideMethod(node) && !IsInsideMemberAccessExpression(node);

				if (isValidParent)
				{
					IdentifierNames.Add(node.Identifier.Text);
				}
			}

			private bool IsInsideMemberAccessExpression(SyntaxNode node)
			{
				var currentNode = node.Parent;

				do
				{
					if (currentNode is MemberAccessExpressionSyntax invocation)
					{
						return true;
					}

					currentNode = currentNode.Parent;
				}
				while (currentNode != null);

				return false;
			}

			private bool IsInsideMethod(SyntaxNode node)
			{
				var currentNode = node.Parent;
				var child = node;

				do
				{
					if (currentNode is InvocationExpressionSyntax invocation
						&& invocation.Expression == child)
					{
						return true;
					}

					child = currentNode;
					currentNode = currentNode.Parent;
				}
				while (currentNode != null);

				return false;
			}
		}

	}
}
