#nullable enable

using System.Collections.Immutable;
using System.Composition;
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
public sealed class BoxingCodeFixProvider : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("UnoInternal0002");

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		context.RegisterCodeFix(
			CodeAction.Create("Use 'Uno.UI.Helpers.Boxes'",
			async ct =>
			{
				var document = context.Document;
				var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
				var root = await model!.SyntaxTree.GetRootAsync(ct).ConfigureAwait(false);
				var node = root.FindNode(context.Span, getInnermostNodeForTie: true);
				var boxesType = model.Compilation.GetTypeByMetadataName("Uno.UI.Helpers.Boxes");
				if (boxesType is null ||
					!model.Compilation.IsSymbolAccessibleWithin(boxesType, model.Compilation.Assembly))
				{
					return document;
				}

				var generator = SyntaxGenerator.GetGenerator(document);
				var boxesIdentifier = (ExpressionSyntax)generator.TypeExpression(boxesType).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

				if (node is LiteralExpressionSyntax literalExpression)
				{
					var typeInfo = model.GetTypeInfo(node, ct);
					string? boxClassName = null;
					string? boxMemberName = null;
					if (typeInfo.Type!.SpecialType == SpecialType.System_Int32)
					{
						boxClassName = "IntegerBoxes";
						boxMemberName = literalExpression.Token.Value switch
						{
							-1 => "NegativeOne",
							0 => "Zero",
							1 => "One",
							_ => null,
						};
					}
					else if (typeInfo.Type!.SpecialType == SpecialType.System_Boolean)
					{
						boxClassName = "BooleanBoxes";
						boxMemberName = literalExpression.Token.Value switch
						{
							true => "BoxedTrue",
							false => "BoxedFalse",
							_ => null,
						};
					}
					else if (typeInfo.Type!.SpecialType == SpecialType.System_Double)
					{
						boxClassName = "DoubleBoxes";
						boxMemberName = literalExpression.Token.Value switch
						{
							0.0 => "Zero",
							1.0 => "One",
							_ => null,
						};
					}

					if (boxMemberName is not null && boxClassName is not null)
					{
						var newNode = SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								boxesIdentifier,
								SyntaxFactory.IdentifierName(boxClassName)),
							SyntaxFactory.IdentifierName(boxMemberName));
						return document.WithSyntaxRoot(root.ReplaceNode(node, newNode));
					}
				}
				else if (node is ExpressionSyntax expressionSyntax)
				{
					var typeInfo = model.GetTypeInfo(node, ct);
					if (typeInfo.Type!.SpecialType is SpecialType.System_Int32 or SpecialType.System_Boolean or SpecialType.System_Double ||
						typeInfo.Type.Name == "RoutedEventFlag")
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
						return document.WithSyntaxRoot(root.ReplaceNode(node, newNode));
					}
				}

				return document;
			}, nameof(BoxingCodeFixProvider)),
			context.Diagnostics);
		return Task.CompletedTask;
	}
}
