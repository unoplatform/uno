#nullable enable

using Microsoft.CodeAnalysis;

#pragma warning disable RS2008 // Enable analyzer release tracking — the Uno XAML generator opts out project-wide (see XamlCodeGeneration.Diagnostics.cs)
#pragma warning disable RS1032 // Diagnostic messages — matched verbatim to contracts/diagnostics.md; templates include placeholders and punctuation the rule can't interpret

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// UNO2xxx diagnostic descriptors for the XAML C# expressions feature.
/// Codes are stable once published (spec FR-022a).
/// See <c>contracts/diagnostics.md</c> for the authoritative table.
/// </summary>
internal static class Diagnostics
{
	private const string Category = "XAML.CSharpExpressions";

	public static readonly DiagnosticDescriptor AmbiguousExpressionOrMarkup = new(
		id: "UNO2001",
		title: "Expression conflicts with a registered markup extension",
		messageFormat: "Identifier '{0}' matches both a markup extension '{0}Extension' and a member in scope. The markup extension wins; use {{= {0}}} to force the C# expression or use {{prefix:{0}}} to force the markup extension and suppress this warning.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor AmbiguousMemberExpression = new(
		id: "UNO2002",
		title: "Member expression is ambiguous",
		messageFormat: "Identifier '{0}' exists on both the page and the DataType. Use 'this.{0}' to reference the page member or '.{0}' (or 'BindingContext.{0}') to reference the DataType member.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor MemberNotFound = new(
		id: "UNO2003",
		title: "Member not found",
		messageFormat: "'{0}' not found on page type '{1}'{2}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor AmbiguousMemberWithStaticType = new(
		id: "UNO2004",
		title: "Member conflicts with a static type name",
		messageFormat: "Identifier '{0}' collides with type '{1}' imported via {2}. The member wins; use '{0}.Member' form or a different member name to disambiguate.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor AsyncLambdaNotSupported = new(
		id: "UNO2005",
		title: "Async event lambdas are not supported",
		messageFormat: "Async lambda expressions are not supported as XAML event handlers. Define a named async method in code-behind and reference it with Click=\"OnClick\" instead.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidExpressionSyntax = new(
		id: "UNO2006",
		title: "Invalid C# expression syntax",
		messageFormat: "The expression '{0}' is not a valid single C# expression: {1}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor EmptyExpression = new(
		id: "UNO2007",
		title: "Expression is empty",
		messageFormat: "Empty expressions are not allowed. Remove the attribute or provide a valid expression between the braces.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor EventLambdaArityMismatch = new(
		id: "UNO2008",
		title: "Event lambda has wrong number of parameters",
		messageFormat: "Event '{0}' expects {1} parameter(s) but the lambda has {2}. Use '(sender, args) => ...' matching the event's delegate signature.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor NestedMarkupExtensionInExpression = new(
		id: "UNO2009",
		title: "Markup extension nested inside a C# expression",
		messageFormat: "Markup extension '{0}' cannot appear inside a C# expression. Split into a separate attribute or rewrite the expression.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor XDataTypeMissing = new(
		id: "UNO2010",
		title: "x:DataType is missing; DataType-based resolution is disabled",
		messageFormat: "The XAML file '{0}' contains C# expressions but does not declare x:DataType. DataType-relative identifiers will not resolve; use 'this.Member' for page-level references.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor OneShotNonNotifyingSources = new(
		id: "UNO2011",
		title: "Expression has no notifying sources; binding is one-shot",
		messageFormat: "Expression '{0}' references only non-notifying sources (static members or non-INPC instances). The binding will be evaluated once at load and will not refresh.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor ExpressionNotSettableDowngrade = new(
		id: "UNO2012",
		title: "Compound expression downgrades two-way default binding to one-way",
		messageFormat: "Property '{0}' has a two-way default binding mode, but the expression is not a simple settable path. Binding is emitted as one-way.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor OptInDirectiveWhenDisabled = new(
		id: "UNO2020",
		title: "C# expressions are not enabled in this project",
		messageFormat: "The attribute '{0}' uses C# expression syntax but UnoXamlCSharpExpressionsEnabled is not set to true. Set <UnoXamlCSharpExpressionsEnabled>true</UnoXamlCSharpExpressionsEnabled> in the csproj or use a conventional {{Binding}} expression.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor CSharpExpressionsNotSupportedOnWinAppSDK = new(
		id: "UNO2099",
		title: "XAML C# expressions are not supported on WinAppSDK",
		messageFormat: "The XAML file '{0}' uses C# expression syntax (attribute '{1}') but the active target is WinAppSDK. This feature is Uno-only. Exclude this XAML from the WinAppSDK target with <Page Condition=\"'$(IsWinAppSdk)' != 'true'\"/> or rewrite the attribute using a conventional {{Binding}}, {{x:Bind}}, or named event handler.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
