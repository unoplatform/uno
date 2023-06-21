using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Uno.UI.SourceGenerators.Internal;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
internal sealed class BoxingCodeFixProvider : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("UnoInternal0001");

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		context.RegisterCodeFix(
			CodeAction.Create("Use 'Uno.UI.Helpers.Boxes'",
			async ct =>
			{
				var document = context.Document;
				var model = await context.Document.GetSemanticModelAsync(ct).ConfigureAwait(false);
				var root = await model.SyntaxTree.GetRootAsync(ct).ConfigureAwait(false);
				var node = root.FindNode(context.Span, getInnermostNodeForTie: true);
				var generator = SyntaxGenerator.GetGenerator(document);
				var boxesIdentifier = (ExpressionSyntax)generator.TypeExpression(model.Compilation.GetTypeByMetadataName("Uno.UI.Helpers.Boxes")).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

				// default(type) => Uno.UI.Helpers.Boxes.DefaultBox<type>.Value
				if (node is DefaultExpressionSyntax defaultExpressionSyntax)
				{
					var newNode = SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							boxesIdentifier,
							SyntaxFactory.GenericName(
								SyntaxFactory.Identifier("DefaultBox"))
							.WithTypeArgumentList(
								SyntaxFactory.TypeArgumentList(
									SyntaxFactory.SingletonSeparatedList(
										defaultExpressionSyntax.Type)))),
						SyntaxFactory.IdentifierName("Value"));
					var newRoot = root.ReplaceNode(node, newNode);
					return document.WithSyntaxRoot(newRoot);
				}
				else if (node is ExpressionSyntax expressionSyntax)
				{
					var typeInfo = model.GetTypeInfo(node);
					if (typeInfo.Type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Boolean)
					{
						var newNode = SyntaxFactory.InvocationExpression(
							SyntaxFactory.MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								boxesIdentifier,
								SyntaxFactory.IdentifierName("Box")))
						.WithArgumentList(
							SyntaxFactory.ArgumentList(
								SyntaxFactory.SingletonSeparatedList(
									SyntaxFactory.Argument(
										expressionSyntax))));
						var newRoot = root.ReplaceNode(node, newNode);
						return document.WithSyntaxRoot(newRoot);
					}
				}

				return document;
			}, nameof(BoxingCodeFixProvider)),
			context.Diagnostics);
		return Task.CompletedTask;
	}
}
