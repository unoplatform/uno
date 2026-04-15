#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileGenerator
	{
		/// <summary>
		/// Monotonic counter for synthesized <c>__xcs_Expr_NNN</c> helper methods emitted
		/// on the page partial when a compound C# expression cannot be represented as a
		/// pure path. Reset per <see cref="XamlFileGenerator"/> because each page generates
		/// into its own partial class.
		/// </summary>
		private int _xcsExprCounter;

		/// <summary>
		/// Synthetic <see cref="XamlType"/> stand-in for the <c>{x:Bind}</c> markup extension.
		/// Only <see cref="XamlType.Name"/> is consulted by the downstream pipeline
		/// (<see cref="HasMarkupExtension"/>, <see cref="BuildXBindEvalFunction"/>).
		/// </summary>
		private static readonly XamlType _syntheticBindXamlType = new XamlType(
			XamlConstants.XamlXmlNamespace,
			"Bind",
			new List<XamlType>(),
			new XamlSchemaContext());

		/// <summary>
		/// Routes a classified C# expression through the existing <c>{x:Bind}</c> emission
		/// pipeline by synthesizing a bind node on the member and falling through to
		/// <see cref="HasMarkupExtension"/>. Simple / dotted-path / directive forms map to
		/// <c>{x:Bind Path, Mode=...}</c>; compound forms register a <c>__xcs_Expr_NNN</c>
		/// helper on the page partial and map to <c>{x:Bind __xcs_Expr_NNN(), Mode=OneWay}</c>,
		/// reusing x:Bind's function-call path, refresh-set computation, lifecycle, and
		/// weak-reference handling.
		/// </summary>
		/// <returns>
		/// <c>true</c> when a diagnostic was emitted and the member has been consumed;
		/// <c>false</c> when the member was either left untouched (fall-through) or rewritten
		/// into a synthesized <c>{x:Bind}</c> node for the existing pipeline to process.
		/// </returns>
		private bool TryEmitCSharpExpression(XamlMemberDefinition member, string rawText)
		{
			var kind = CSharpExpressionClassifier.Classify(rawText);
			if (kind is null)
			{
				return false;
			}

			var inner = CSharpExpressionClassifier.ExtractInnerCSharp(rawText, kind.Value);
			if (string.IsNullOrWhiteSpace(inner))
			{
				ReportExpressionDiagnostic(Diagnostics.EmptyExpression, member, rawText);
				return true;
			}

			var (_, hasErrors) = CSharpExpressionParser.Parse(inner!);
			if (hasErrors)
			{
				ReportExpressionDiagnostic(Diagnostics.InvalidExpressionSyntax, member, rawText, inner!, "parser error");
				return true;
			}

			// Convert operator aliases + single-quoted strings into standard C# before the
			// x:Bind pipeline sees them. Directive / simple-path forms are pure identifiers
			// so transforms are no-ops; compound forms need the transforms so the synthesized
			// helper method body is valid C#.
			var normalized = QuoteTransform.Transform(OperatorAliases.Replace(inner!));

			string bindPath;
			string mode;

			EnsureXClassName();
			var pageType = _xClassName?.Symbol;

			switch (kind.Value)
			{
				case ExpressionKind.SimpleIdentifier:
				case ExpressionKind.DottedPath:
				case ExpressionKind.Explicit:
					bindPath = normalized;
					mode = IsPageSettablePath(pageType, normalized) ? "TwoWay" : "OneWay";
					break;

				case ExpressionKind.ForcedDataType:
					// Leading '.' tells x:Bind to root the path on the DataTemplate's x:DataType.
					bindPath = "." + normalized;
					mode = "OneWay";
					break;

				case ExpressionKind.ForcedThis:
					// x:Bind's root is already the x:Class page — strip the redundant `this.`
					// so `{this.Foo}` compiles to `{x:Bind Foo, Mode=TwoWay|OneWay}`.
					bindPath = normalized.StartsWith("this.", StringComparison.Ordinal)
						? normalized.Substring("this.".Length)
						: normalized;
					mode = IsPageSettablePath(pageType, bindPath) ? "TwoWay" : "OneWay";
					break;

				case ExpressionKind.Compound:
					bindPath = RegisterCompoundHelper(normalized, member) + "()";
					mode = "OneWay";
					break;

				default:
					// EventLambda / MarkupExtension — not produced by Classify today; fall through.
					return false;
			}

			InjectSyntheticBindNode(member, bindPath, mode);
			return false;
		}

		/// <summary>
		/// Registers a <c>private ReturnType __xcs_Expr_NNN() => &lt;normalized&gt;;</c>
		/// helper method on the page partial's scope and returns its effective name.
		/// The expression body is the transformed C# (operator aliases and quote-transforms
		/// applied) — the x:Bind pipeline will call it from the compiled binding's getter.
		/// </summary>
		private string RegisterCompoundHelper(string normalizedExpression, XamlMemberDefinition member)
		{
			var declaringType = FindType(member.Member.DeclaringType);
			var targetPropertyType = declaringType is not null
				? _metadataHelper.FindPropertyTypeByOwnerSymbol(declaringType, member.Member.Name)
				: null;

			var returnType = ResolveHelperReturnType(targetPropertyType);
			var body = CoerceExpressionForTargetType(normalizedExpression, targetPropertyType);

			var suggestedName = "__xcs_Expr_" + (_xcsExprCounter++).ToString("D3", CultureInfo.InvariantCulture);

			return CurrentScope.RegisterMethod(
				suggestedName,
				(effectiveName, methodWriter) =>
				{
					TryAnnotateWithGeneratorSource(methodWriter, suffix: "CSharpExpression.CompoundHelper");
					methodWriter.AppendLineIndented($"private {returnType} {effectiveName}() => {body};");
				});
		}

		/// <summary>
		/// Walks a dotted path against <paramref name="pageType"/> (the x:Class root) and
		/// returns <c>true</c> only when every hop resolves to a public property AND the
		/// final leaf exposes a public setter. Used to decide <c>Mode=TwoWay</c> vs
		/// <c>Mode=OneWay</c> for synthesized <c>{x:Bind}</c> nodes.
		/// </summary>
		private static bool IsPageSettablePath(INamedTypeSymbol? pageType, string dottedPath)
		{
			if (pageType is null || string.IsNullOrEmpty(dottedPath))
			{
				return false;
			}

			var parts = dottedPath.Split('.');
			ITypeSymbol? current = pageType;
			IPropertySymbol? leaf = null;

			foreach (var part in parts)
			{
				if (current is null)
				{
					return false;
				}

				leaf = FindPublicInstanceProperty(current, part);
				if (leaf is null)
				{
					return false;
				}

				current = leaf.Type;
			}

			return leaf is { SetMethod.DeclaredAccessibility: Accessibility.Public };
		}

		private static IPropertySymbol? FindPublicInstanceProperty(ITypeSymbol type, string name)
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

		private static string ResolveHelperReturnType(ITypeSymbol? targetType)
		{
			if (targetType is null)
			{
				return "object";
			}

			return targetType.SpecialType == SpecialType.System_String
				? "string"
				: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		private static string CoerceExpressionForTargetType(string expression, ITypeSymbol? targetType)
		{
			if (targetType is null)
			{
				return expression;
			}

			return targetType.SpecialType == SpecialType.System_String
				? $"global::System.Convert.ToString(({expression}), global::System.Globalization.CultureInfo.CurrentCulture)!"
				: expression;
		}

		/// <summary>
		/// Mutates <paramref name="member"/> to look as if the author had written
		/// <c>{x:Bind <paramref name="bindPath"/>, Mode=<paramref name="mode"/>}</c>.
		/// The downstream <see cref="BuildXBindEvalFunction"/> pipeline reads
		/// <see cref="XamlMemberDefinition.Objects"/> first and ignores the raw string value.
		/// </summary>
		private void InjectSyntheticBindNode(XamlMemberDefinition member, string bindPath, string mode)
		{
			var bindObject = new XamlObjectDefinition(_syntheticBindXamlType, member.Owner);

			// XAML parser stores x:Bind positional parameters as UTF-16 bytes, Base64-encoded,
			// with '=' replaced by '_' so the string survives the XAML attribute-value lexer
			// (see XBindExpressionParser.PathRewrite.GetEncodedPath / RestoreSinglePath).
			// Synthesized bind nodes have to match that encoding — BuildXBindEvalFunction will
			// call RestoreSinglePath on whatever we store here.
			var encodedPath = Convert
				.ToBase64String(Encoding.Unicode.GetBytes(bindPath))
				.Replace("=", "_");

			var pathMember = new XamlMemberDefinition(
				new XamlMember("_PositionalParameters", _syntheticBindXamlType, false),
				member.LineNumber,
				member.LinePosition,
				bindObject)
			{
				Value = encodedPath,
			};
			bindObject.Members.Add(pathMember);

			var modeMember = new XamlMemberDefinition(
				new XamlMember("Mode", _syntheticBindXamlType, false),
				member.LineNumber,
				member.LinePosition,
				bindObject)
			{
				Value = mode,
			};
			bindObject.Members.Add(modeMember);

			member.Objects.Clear();
			member.Objects.Add(bindObject);
			member.Value = null;
		}

		private void ReportExpressionDiagnostic(
			DiagnosticDescriptor descriptor,
			XamlMemberDefinition member,
			string rawText,
			params object[] messageArgs)
		{
			var line = Math.Max(0, member.LineNumber - 1);
			var column = Math.Max(0, member.LinePosition - 1);
			var span = new LinePositionSpan(
				new LinePosition(line, column),
				new LinePosition(line, column + rawText.Length));
			var location = Location.Create(member.FilePath, new TextSpan(0, rawText.Length), span);
			_generatorContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
		}
	}
}
