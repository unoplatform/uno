using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.UI.SourceGenerators.DependencyObject
{
    internal class MethodCallFinder
	{
		public static bool IsCallingInitialize(SemanticModel model, SyntaxTree tree)
		{
			var visitor = new Visitor(model);

			visitor.Visit(tree.GetRoot());

			return visitor.HasInitializeCall;
		}

		private class Visitor : CSharpSyntaxWalker
		{
			private SemanticModel model;

			public bool HasInitializeCall { get; private set; }

			public Visitor(SemanticModel model)
			{
				this.model = model;
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);

				var access = node.Expression as IdentifierNameSyntax;

				if (access != null)
				{
					var name = access.ToString();

					if (name == "InitializeBinder")
					{
						HasInitializeCall = true;
					}
				}
			}
		}
	}
}
