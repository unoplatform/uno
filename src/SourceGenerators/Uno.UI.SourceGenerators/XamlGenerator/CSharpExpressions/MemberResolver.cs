#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Resolves bare identifiers and member-access roots against the <see cref="ResolutionScope"/>.
/// Implements the decision table in <c>contracts/resolution-algorithm.md</c> §Resolution.
/// </summary>
/// <remarks>
/// Decision table summary:
/// <list type="bullet">
/// <item>Identifier found on <c>PageType</c> only → <c>This</c></item>
/// <item>Identifier found on <c>DataType</c> only → <c>DataType</c></item>
/// <item>Identifier found on both → <c>Both</c> + <c>UNO2002</c></item>
/// <item>Identifier found nowhere + matches a registered markup extension → <c>MarkupExtension</c> + <c>UNO2001</c></item>
/// <item>Identifier found nowhere + matches a static type in <c>GlobalUsings</c> → <c>StaticType</c></item>
/// <item>Identifier collides with a member AND a static type → <c>UNO2004</c></item>
/// <item>Identifier resolves nowhere → <c>Neither</c> + <c>UNO2003</c></item>
/// </list>
/// </remarks>
internal static class MemberResolver
{
	// TODO (T034): implement bare-identifier resolution (US1 scope — UNO2002, UNO2003).
	// TODO (T066): extend with ForcedThis, ForcedDataType, MarkupExtension branches (US3 — UNO2001).
	// TODO (T076): extend with static-type lookup via ResolutionScope.GlobalUsings (US4 — UNO2004).
	public static ResolutionResult Resolve(string identifier, ResolutionScope scope)
	{
		_ = identifier;
		_ = scope;
		return new ResolutionResult(MemberLocation.Neither, Symbol: null, Diagnostic: null);
	}
}
