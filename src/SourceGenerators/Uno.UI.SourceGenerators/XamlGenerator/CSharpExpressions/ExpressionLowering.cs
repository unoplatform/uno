#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Produces a <see cref="LoweredExpression"/> suitable for one of three emission paths
/// on the page partial (see <c>contracts/generated-binding-shapes.md</c>):
///
/// <list type="bullet">
/// <item><description><b>Simple-path</b> (§1, §2, §11): pure dotted path against the DataType.
/// Lowered to a <see cref="SimplePathBinding"/>; emitted byte-equivalent to the existing
/// <c>x:Bind</c> pipeline so <see cref="Utils.XBindExpressionParser"/> is reused.</description></item>
/// <item><description><b>Compound</b> (§3, §4, §6, §7, §8, §13): arithmetic / interpolation / ternary
/// / method-call / alias forms that need a synthesized <c>__xcs_Expr_NNN</c> helper and an INPC
/// refresh set.</description></item>
/// <item><description><b>Direct-assignment</b> (§5 one-shot, §11 <c>this.X</c>): no <c>Binding</c>
/// constructed; the value is read once at load time. Companion diagnostic: <c>UNO2011</c>.</description></item>
/// </list>
///
/// The function is pure: inputs are the classified value, the analysis result, the resolution
/// scope (for DataType symbol when synthesizing the helper signature), a monotonically-increasing
/// helper index, and the optional target DP type. It does not emit to a writer, register anything
/// on the generator context, or touch Roslyn's <see cref="Compilation"/> beyond display-string
/// formatting.
/// </summary>
internal static class ExpressionLowering
{
	private const string SourceParameter = "__source";

	/// <summary>
	/// Lowers one classified C# expression attribute value.
	/// </summary>
	/// <param name="classifiedValue">Result of <see cref="CSharpExpressionClassifier.Classify(string?)"/>
	/// wrapped with the XAML location and CDATA flag.</param>
	/// <param name="analysis">Output of <see cref="ExpressionAnalyzer.Analyze"/>.</param>
	/// <param name="scope">Resolution scope — needed when emitting a compound helper so the
	/// DataType parameter can be qualified.</param>
	/// <param name="helperIndex">Zero-based counter per page; formatted as a 3-digit suffix.</param>
	/// <param name="targetPropertyTypeFullName">Fully-qualified target DP type, used as the
	/// helper's return type and as the direct-assignment cast target. When null, falls back to
	/// <see cref="ExpressionAnalysisResult.LeafPropertyType"/>, then to <c>object</c>.</param>
	/// <param name="forceOneWayForTwoWayTarget">When <c>true</c>, indicates the caller has
	/// determined the target DP defaults to <c>BindingMode.TwoWay</c> but the expression is
	/// not settable. The returned <see cref="SimplePathBinding"/> will be <c>OneWay</c>;
	/// the caller is responsible for emitting <c>UNO2012</c>.</param>
	public static LoweredExpression Lower(
		XamlExpressionAttributeValue classifiedValue,
		ExpressionAnalysisResult analysis,
		ResolutionScope scope,
		int helperIndex,
		string? targetPropertyTypeFullName = null,
		bool forceOneWayForTwoWayTarget = false)
	{
		if (classifiedValue is null)
		{
			throw new ArgumentNullException(nameof(classifiedValue));
		}

		if (analysis is null)
		{
			throw new ArgumentNullException(nameof(analysis));
		}

		if (scope is null)
		{
			throw new ArgumentNullException(nameof(scope));
		}

		var kind = classifiedValue.Kind;

		if (analysis.Handlers.Count == 0
			&& analysis.Captures.Count == 0
			&& ExtractRawPath(analysis.TransformedCSharp) is { } rawPath)
		{
			// Unresolved simple-identifier / dotted-path form — either a bare
			// <c>{= Foo}</c> without an x:DataType declaration, or the analyser could not
			// rewrite the root because the DataType does not expose the identifier.
			// Fall back to a runtime-binding path against the element's DataContext —
			// equivalent to the author writing <c>{Binding Path=X}</c> by hand.
			var fallbackMode = kind == ExpressionKind.ForcedDataType || forceOneWayForTwoWayTarget
				? SimplePathBindingMode.OneWay
				: SimplePathBindingMode.TwoWay;
			return new SimplePathBinding(
				Path: rawPath,
				Mode: fallbackMode,
				DataContextSource: DataContextSource.DataType);
		}

		if (analysis.Handlers.Count == 0)
		{
			// Shape §5 / §11 first case: one-shot direct assignment.
			return new DirectAssignment(
				CSharpExpression: analysis.TransformedCSharp,
				LeafType: analysis.LeafPropertyType,
				Captures: analysis.Captures);
		}

		if (IsSimplePathCandidate(kind, analysis))
		{
			var path = ExtractSimplePath(analysis.TransformedCSharp);
			if (path is not null)
			{
				var mode = (kind == ExpressionKind.ForcedDataType, analysis.IsSettable, forceOneWayForTwoWayTarget) switch
				{
					(true, _, _) => SimplePathBindingMode.OneWay,
					(_, true, false) => SimplePathBindingMode.TwoWay,
					_ => SimplePathBindingMode.OneWay,
				};

				return new SimplePathBinding(
					Path: path,
					Mode: mode,
					DataContextSource: DataContextSource.DataType);
			}
		}

		var helperName = BuildHelperName(helperIndex);
		var returnType = ResolveReturnType(analysis.LeafPropertyType, targetPropertyTypeFullName);
		var body = BuildHelperBody(helperName, returnType, scope.DataType, analysis, targetPropertyTypeFullName);

		return new CompoundBinding(
			HelperMethodName: helperName,
			HelperMethodBody: body,
			Handlers: analysis.Handlers,
			LeafType: analysis.LeafPropertyType,
			Captures: analysis.Captures);
	}

	internal static string BuildHelperName(int helperIndex)
		=> "__xcs_Expr_" + helperIndex.ToString("D3", CultureInfo.InvariantCulture);

	private static bool IsSimplePathCandidate(ExpressionKind kind, ExpressionAnalysisResult analysis)
	{
		if (analysis.Captures.Count > 0)
		{
			return false;
		}

		return kind is ExpressionKind.SimpleIdentifier
			or ExpressionKind.DottedPath
			or ExpressionKind.ForcedDataType;
	}

	/// <summary>
	/// Returns the dotted path portion following <c>__source.</c> if the transformed text is
	/// a pure path; otherwise <c>null</c>.
	/// </summary>
	/// <summary>
	/// Returns <paramref name="transformed"/> when it is already a pure dotted-path identifier
	/// (no <c>__source.</c> prefix, no operators). Used when the analyser could not rewrite the
	/// expression because no DataType was declared.
	/// </summary>
	private static string? ExtractRawPath(string transformed)
	{
		if (string.IsNullOrEmpty(transformed))
		{
			return null;
		}

		foreach (var c in transformed)
		{
			if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
			{
				return null;
			}
		}

		if (transformed[0] == '.' || transformed[transformed.Length - 1] == '.')
		{
			return null;
		}

		return transformed;
	}

	private static string? ExtractSimplePath(string transformed)
	{
		if (string.IsNullOrEmpty(transformed))
		{
			return null;
		}

		const string prefix = SourceParameter + ".";
		if (!transformed.StartsWith(prefix, StringComparison.Ordinal))
		{
			return null;
		}

		var path = transformed.Substring(prefix.Length);
		if (path.Length == 0)
		{
			return null;
		}

		foreach (var c in path)
		{
			if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
			{
				return null;
			}
		}

		if (path[0] == '.' || path[path.Length - 1] == '.')
		{
			return null;
		}

		return path;
	}

	private static string ResolveReturnType(
		ITypeSymbol? leafType,
		string? targetPropertyTypeFullName)
	{
		if (!string.IsNullOrEmpty(targetPropertyTypeFullName))
		{
			return targetPropertyTypeFullName!;
		}

		if (leafType is not null)
		{
			return leafType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		return "object";
	}

	private static string BuildHelperBody(
		string helperName,
		string returnType,
		INamedTypeSymbol? dataType,
		ExpressionAnalysisResult analysis,
		string? targetPropertyTypeFullName)
	{
		var sb = new StringBuilder();
		sb.Append("private ").Append(returnType).Append(' ').Append(helperName).Append('(');

		var first = true;

		if (dataType is not null)
		{
			sb.Append(dataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			sb.Append(' ').Append(SourceParameter);
			first = false;
		}

		foreach (var capture in analysis.Captures)
		{
			if (!first)
			{
				sb.Append(", ");
			}
			first = false;

			var captureTypeName = capture.Type is not null
				? capture.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
				: "object";

			sb.Append(captureTypeName).Append(' ').Append(capture.CaptureVariableName);
		}

		sb.Append(") => ").Append(WrapExpressionForTarget(analysis, targetPropertyTypeFullName)).Append(';');
		return sb.ToString();
	}

	/// <summary>
	/// Emits the expression text with a conversion wrapper when the target property type
	/// differs from the analyser's leaf type. Covers the common <c>string</c> target case
	/// with <c>Convert.ToString</c>; uses an explicit cast for other mismatches when both
	/// types are known; passes the expression through when no coercion is required.
	/// </summary>
	private static string WrapExpressionForTarget(
		ExpressionAnalysisResult analysis,
		string? targetPropertyTypeFullName)
	{
		var expression = analysis.TransformedCSharp;

		if (string.IsNullOrEmpty(targetPropertyTypeFullName))
		{
			return expression;
		}

		var leaf = analysis.LeafPropertyType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		if (leaf is not null && string.Equals(leaf, targetPropertyTypeFullName, StringComparison.Ordinal))
		{
			return expression;
		}

		return targetPropertyTypeFullName switch
		{
			"string" or "global::System.String"
				=> $"global::System.Convert.ToString(({expression}), global::System.Globalization.CultureInfo.CurrentCulture)!",
			_ => expression,
		};
	}
}
