#:package Microsoft.CodeAnalysis.CSharp@4.14.0
#:property PublishAot=false

// Ports a WinUI interaction test (or MUXTestInfra helper) to Uno's in-process runtime-test shim.
// Usage: dotnet run build/winui-port/port-interaction-test.cs -- <source.cs> <destination.cs> ["<upstream path>, commit <sha>"]
//
// The shim keeps MITA's API, except that anything which must yield to the UI thread returns a Task.
// This rewrite is purely mechanical: it awaits those calls, turns their callers into async methods
// (transitively), drops the TAEF usings and re-indents with tabs. Everything else stays verbatim.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

if (args.Length is not (2 or 3))
{
	Console.Error.WriteLine("Usage: dotnet run port-interaction-test.cs -- <source.cs> <destination.cs> [\"<upstream path>, commit <sha>\"]");
	return 1;
}

var source = File.ReadAllText(args[0]);
var (result, warnings) = InteractionTestPorter.Port(source);
if (args.Length == 3)
{
	result = InteractionTestPorter.AddMuxReference(result, args[2]);
}
var bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
File.WriteAllText(args[1], result.ReplaceLineEndings("\r\n"), bom);

foreach (var warning in warnings)
{
	Console.WriteLine($"warning: {warning}");
}

Console.WriteLine($"Ported {args[0]} -> {args[1]} ({warnings.Count} warning(s)).");
return 0;

static class InteractionTestPorter
{
	// Static helpers whose members wait for the UI (every member, except the listed synchronous ones).
	private static readonly Dictionary<string, HashSet<string>> AsyncStaticTypes = new()
	{
		["Wait"] = new(),
		["KeyboardHelper"] = new() { "GetPressDownModifierKeyStroke", "GetReleaseModifierKeyStroke" },
		["InputHelper"] = new(),
		["FocusHelper"] = new(),
	};

	// Static members that wait, on types whose other members are synchronous.
	private static readonly HashSet<string> AsyncStaticMembers = new()
	{
		"TestSetupHelper.GoBack",
		"TestSetupHelper.SetInnerFrameFlowDirection",
		"TestEnvironment.VerifyAreEqualWithRetry",
		"TestEnvironment.Application.GoBack",
		"CompositableWaiter.WaitAny",
	};

	// Instance members that wait whatever their receiver (waiters and the *AndWait extensions).
	private static readonly HashSet<string> AsyncInstanceMembers = new()
	{
		"Wait",
		"TryWait",
		"InvokeAndWait",
		"ToggleAndWait",
		"CheckAndWait",
		"SetValueAndWait",
		"ExpandAndWait",
		"CollapseAndWait",
		"SelectItemByName",
		"SelectItemById",
		"ScrollToElement",
	};

	private const string TaefUsingPrefix = "WEX.";

	public static (string Text, List<string> Warnings) Port(string source)
	{
		var warnings = new List<string>();
		var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

		root = RemoveTaefUsings(root);
		root = new TestSetupHelperRewriter().Visit(root) as CompilationUnitSyntax ?? root;

		// Fixed point: awaiting a call makes its method async, which makes calls to that method awaitable.
		var asyncLocalMethods = new AsyncLocalMethods();
		while (true)
		{
			var awaiter = new AwaitRewriter(asyncLocalMethods);
			root = (CompilationUnitSyntax)awaiter.Visit(root);

			var asyncifier = new AsyncMethodRewriter(warnings);
			root = (CompilationUnitSyntax)asyncifier.Visit(root);

			if (!asyncLocalMethods.Merge(asyncifier.NewAsyncMethods) && !awaiter.Changed)
			{
				break;
			}
		}

		root = new RunsOnUIThreadRewriter().Visit(root) as CompilationUnitSyntax ?? root;
		root = AddUsingIfMissing(root, "System.Threading.Tasks", r => r.DescendantNodes().OfType<AwaitExpressionSyntax>().Any());
		root = AddUsingIfMissing(root, "Uno.UI.RuntimeTests", r => r.DescendantNodes().OfType<AttributeSyntax>().Any(a => a.Name.ToString() == "RunsOnUIThread"));

		foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
			.Where(c => c.Type.ToString().EndsWith("TestSetupHelper", StringComparison.Ordinal)))
		{
			warnings.Add($"line {Line(creation)}: '{creation.Type}' is constructed directly; await its async factory instead.");
		}

		foreach (var lambda in root.DescendantNodes().OfType<LambdaExpressionSyntax>()
			.Where(l => l.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)))
		{
			warnings.Add($"line {Line(lambda)}: lambda became async; check its delegate type accepts a Task-returning lambda.");
		}

		var text = Reindent(root.ToFullString());
		if (root.DescendantNodes().OfType<AttributeSyntax>().Any(a => a.Name.ToString() == "TestClass"))
		{
			text = AddAfterUsings(text, """
				// Upstream TAEF conventions, kept for line-by-line fidelity.
				#pragma warning disable MSTEST0007 // [TestProperty] on [ClassInitialize]
				#pragma warning disable MSTEST0029 // public TestCleanup() without [TestCleanup]
				""");
		}

		return (text, warnings);
	}

	private static string AddAfterUsings(string text, string block)
	{
		var lines = text.Split('\n').ToList();
		var lastUsing = lines.FindLastIndex(l => l.StartsWith("using ", StringComparison.Ordinal));
		lines.Insert(lastUsing + 1, "\n" + block.ReplaceLineEndings("\n"));
		return string.Join('\n', lines);
	}

	public static string AddMuxReference(string text, string reference)
	{
		const string license = "// Licensed under the MIT License.";
		var lines = text.Split('\n').ToList();
		var licenseLine = lines.FindIndex(l => l.TrimStart('﻿').StartsWith(license, StringComparison.Ordinal));
		if (lines.Any(l => l.StartsWith("// MUX Reference", StringComparison.Ordinal)))
		{
			return text;
		}

		lines.Insert(licenseLine + 1, "");
		lines.Insert(licenseLine + 2, $"// MUX Reference {reference}");
		return string.Join('\n', lines);
	}

	private static CompilationUnitSyntax AddUsingIfMissing(CompilationUnitSyntax root, string ns, Func<CompilationUnitSyntax, bool> isNeeded)
	{
		if (!isNeeded(root) || root.Usings.Any(u => u.Name?.ToString() == ns))
		{
			return root;
		}

		var anchor = root.Usings.LastOrDefault(u => u.Name?.ToString().StartsWith("System", StringComparison.Ordinal) == true) ?? root.Usings.FirstOrDefault();
		var directive = UsingDirective(ParseName(ns)).NormalizeWhitespace().WithTrailingTrivia(EndOfLine("\n"));
		return anchor is null
			? root.AddUsings(directive)
			: root.WithUsings(root.Usings.Insert(root.Usings.IndexOf(anchor) + 1, directive));
	}

	private static CompilationUnitSyntax RemoveTaefUsings(CompilationUnitSyntax root)
	{
		var taef = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
			.Where(u => u.Name?.ToString().StartsWith(TaefUsingPrefix, StringComparison.Ordinal) == true)
			.ToList();
		return root.RemoveNodes(taef, SyntaxRemoveOptions.KeepNoTrivia)!;
	}

	internal static AwaitExpressionSyntax Await(ExpressionSyntax expression)
		=> SyntaxFactory.AwaitExpression(Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(Space), expression);

	internal static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

	internal static bool IsAsyncCall(InvocationExpressionSyntax invocation, AsyncLocalMethods asyncLocalMethods)
	{
		switch (invocation.Expression)
		{
			case MemberAccessExpressionSyntax member:
				var name = member.Name.Identifier.ValueText;
				var receiver = member.Expression.ToString();
				if (AsyncStaticTypes.TryGetValue(receiver, out var syncMembers))
				{
					return !syncMembers.Contains(name);
				}

				if (AsyncStaticMembers.Contains($"{receiver}.{name}"))
				{
					return true;
				}

				return AsyncInstanceMembers.Contains(name)
					|| (member.Expression is ThisExpressionSyntax && asyncLocalMethods.Unqualified.Contains(name))
					|| asyncLocalMethods.Static.Contains($"{receiver}.{name}")
					|| asyncLocalMethods.Instance.Contains(name);

			case MemberBindingExpressionSyntax binding:
				return AsyncInstanceMembers.Contains(binding.Name.Identifier.ValueText)
					|| asyncLocalMethods.Instance.Contains(binding.Name.Identifier.ValueText);

			case IdentifierNameSyntax identifier:
				return asyncLocalMethods.Unqualified.Contains(identifier.Identifier.ValueText);

			case GenericNameSyntax generic:
				return asyncLocalMethods.Unqualified.Contains(generic.Identifier.ValueText);

			default:
				return false;
		}
	}

	private static string Reindent(string text)
	{
		var builder = new StringBuilder(text.Length);
		foreach (var rawLine in text.ReplaceLineEndings("\n").Split('\n'))
		{
			var spaces = 0;
			while (spaces < rawLine.Length && rawLine[spaces] == ' ')
			{
				spaces++;
			}

			var line = new string('\t', spaces / 4) + new string(' ', spaces % 4) + rawLine[spaces..];
			builder.Append(line.TrimEnd()).Append('\n');
		}

		return builder.ToString().TrimEnd('\n') + "\n";
	}
}

sealed class AsyncLocalMethods
{
	// Callable without a receiver from anywhere in the file (same or nested type).
	public HashSet<string> Unqualified { get; } = new();

	// "Type.Method" for static methods, called as Type.Method(...).
	public HashSet<string> Static { get; } = new();

	// Instance methods, called on any receiver (e.g. a custom UIObject wrapper).
	public HashSet<string> Instance { get; } = new();

	public bool Merge(IEnumerable<(string Type, string Method, bool IsStatic)> methods)
	{
		var changed = false;
		foreach (var (type, method, isStatic) in methods)
		{
			changed |= Unqualified.Add(method);
			changed |= isStatic ? Static.Add($"{type}.{method}") : Instance.Add(method);
		}

		return changed;
	}
}

// `using (var setup = new TestSetupHelper(...))` -> `await using (var setup = await TestSetupHelper.Create(...))`
sealed class TestSetupHelperRewriter : CSharpSyntaxRewriter
{
	public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
	{
		var visited = (UsingStatementSyntax)base.VisitUsingStatement(node)!;
		var hasSetupHelper = visited.Declaration?.Variables.Any(v => v.Initializer?.Value is AwaitExpressionSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Create" } m } } && m.Expression.ToString() == "TestSetupHelper") == true;
		return hasSetupHelper && visited.AwaitKeyword.IsKind(SyntaxKind.None)
			? visited.WithAwaitKeyword(Token(SyntaxKind.AwaitKeyword).WithLeadingTrivia(visited.UsingKeyword.LeadingTrivia).WithTrailingTrivia(Space))
				.WithUsingKeyword(visited.UsingKeyword.WithLeadingTrivia())
			: visited;
	}

	public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
	{
		if (node.Type.ToString() != "TestSetupHelper")
		{
			return base.VisitObjectCreationExpression(node);
		}

		var create = InvocationExpression(
			MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("TestSetupHelper"), IdentifierName("Create")),
			node.ArgumentList ?? ArgumentList());
		return InteractionTestPorter.Await(create.WithoutTrivia()).WithTriviaFrom(node);
	}
}

sealed class AwaitRewriter : CSharpSyntaxRewriter
{
	private readonly AsyncLocalMethods _asyncLocalMethods;

	public AwaitRewriter(AsyncLocalMethods asyncLocalMethods) => _asyncLocalMethods = asyncLocalMethods;

	public bool Changed { get; private set; }

	public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
	{
		var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
		if (node.Parent is AwaitExpressionSyntax
			|| node.Parent is ParenthesizedExpressionSyntax { Parent: AwaitExpressionSyntax }
			|| node.Expression is MemberBindingExpressionSyntax
			|| !InteractionTestPorter.IsAsyncCall(node, _asyncLocalMethods)
			|| IsInsideNameof(node))
		{
			return visited;
		}

		Changed = true;
		ExpressionSyntax awaited = InteractionTestPorter.Await(visited.WithoutLeadingTrivia())
			.WithLeadingTrivia(visited.GetLeadingTrivia());

		// `x.M().P` must become `(await x.M()).P`.
		if (node.Parent is MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or ConditionalAccessExpressionSyntax)
		{
			awaited = ParenthesizedExpression(awaited.WithoutLeadingTrivia()).WithLeadingTrivia(visited.GetLeadingTrivia());
		}

		return awaited;
	}

	public override SyntaxNode? VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
	{
		// `waiter?.Wait()` -> `await (waiter?.Wait() ?? Task.CompletedTask)`
		if (node.Parent is not AwaitExpressionSyntax
			&& node.Parent is not BinaryExpressionSyntax { Parent: ParenthesizedExpressionSyntax { Parent: AwaitExpressionSyntax } }
			&& node.WhenNotNull is InvocationExpressionSyntax invocation
			&& InteractionTestPorter.IsAsyncCall(invocation, _asyncLocalMethods))
		{
			Changed = true;
			var coalesced = BinaryExpression(
				SyntaxKind.CoalesceExpression,
				node.WithoutTrivia(),
				Token(SyntaxKind.QuestionQuestionToken).WithLeadingTrivia(Space).WithTrailingTrivia(Space),
				MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Task"), IdentifierName("CompletedTask")));
			return InteractionTestPorter.Await(ParenthesizedExpression(coalesced)).WithTriviaFrom(node);
		}

		return base.VisitConditionalAccessExpression(node);
	}

	private static bool IsInsideNameof(SyntaxNode node)
		=> node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });
}

sealed class AsyncMethodRewriter : CSharpSyntaxRewriter
{
	private readonly List<string> _warnings;

	public AsyncMethodRewriter(List<string> warnings) => _warnings = warnings;

	public List<(string Type, string Method, bool IsStatic)> NewAsyncMethods { get; } = new();

	public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
	{
		var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
		if (!ContainsOwnAwait(visited))
		{
			return visited;
		}

		var containingType = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "";
		NewAsyncMethods.Add((containingType, visited.Identifier.ValueText, visited.Modifiers.Any(SyntaxKind.StaticKeyword)));
		if (visited.Modifiers.Any(SyntaxKind.AsyncKeyword))
		{
			return visited;
		}

		if (visited.Modifiers.Any(SyntaxKind.OverrideKeyword) || visited.ExplicitInterfaceSpecifier is not null)
		{
			_warnings.Add($"line {InteractionTestPorter.Line(node)}: '{visited.Identifier}' overrides or implements a member; its base signature must become async too.");
		}

		var returnType = visited.ReturnType;
		TypeSyntax asyncReturnType = returnType is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword }
			? IdentifierName("Task")
			: GenericName(Identifier("Task"), TypeArgumentList(SingletonSeparatedList(returnType.WithoutTrivia())));

		var asyncToken = Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space);
		if (visited.Modifiers.Count == 0)
		{
			visited = visited
				.WithModifiers(TokenList(asyncToken.WithLeadingTrivia(returnType.GetLeadingTrivia())))
				.WithReturnType(asyncReturnType.WithTrailingTrivia(returnType.GetTrailingTrivia()));
		}
		else
		{
			visited = visited
				.WithModifiers(visited.Modifiers.Add(asyncToken))
				.WithReturnType(asyncReturnType.WithTriviaFrom(returnType));
		}

		return visited;
	}

	public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
	{
		var visited = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
		if (!ContainsOwnAwait(visited) || visited.Modifiers.Any(SyntaxKind.AsyncKeyword))
		{
			return visited;
		}

		NewAsyncMethods.Add(("", visited.Identifier.ValueText, true));
		var returnType = visited.ReturnType;
		TypeSyntax asyncReturnType = returnType is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword }
			? IdentifierName("Task")
			: GenericName(Identifier("Task"), TypeArgumentList(SingletonSeparatedList(returnType.WithoutTrivia())));
		return visited.WithModifiers(visited.Modifiers.Add(Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space)))
			.WithReturnType(asyncReturnType.WithTriviaFrom(returnType));
	}

	public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
	{
		var visited = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node)!;
		return ContainsOwnAwait(visited) && !visited.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)
			? visited.WithAsyncKeyword(Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space))
			: visited;
	}

	public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
	{
		var visited = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node)!;
		return ContainsOwnAwait(visited) && !visited.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)
			? visited.WithAsyncKeyword(Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space))
			: visited;
	}

	public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
	{
		if (ContainsOwnAwait(node))
		{
			_warnings.Add($"line {InteractionTestPorter.Line(node)}: property '{node.Identifier}' awaits; turn it into an async method.");
		}

		return base.VisitPropertyDeclaration(node);
	}

	// Awaits that belong to this member, not to a nested lambda or local function.
	private static bool ContainsOwnAwait(SyntaxNode member)
		=> member.DescendantNodes(n => n == member || n is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
			.Any(n => n is AwaitExpressionSyntax || n is UsingStatementSyntax { AwaitKeyword.RawKind: (int)SyntaxKind.AwaitKeyword });
}

// Tests drive the UI from the UI thread, as upstream's out-of-process tests drive it through UIA.
sealed class RunsOnUIThreadRewriter : CSharpSyntaxRewriter
{
	public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		var visited = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
		var attributes = visited.AttributeLists.SelectMany(l => l.Attributes).Select(a => a.Name.ToString()).ToList();
		if (!attributes.Contains("TestClass") || attributes.Contains("RunsOnUIThread"))
		{
			return visited;
		}

		var testClassList = visited.AttributeLists.First(l => l.Attributes.Any(a => a.Name.ToString() == "TestClass"));
		var indentation = testClassList.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse();
		var runsOnUIThread = AttributeList(SingletonSeparatedList(Attribute(IdentifierName("RunsOnUIThread"))))
			.WithLeadingTrivia(indentation)
			.WithTrailingTrivia(testClassList.GetTrailingTrivia());
		return visited.WithAttributeLists(visited.AttributeLists.Insert(visited.AttributeLists.IndexOf(testClassList) + 1, runsOnUIThread));
	}
}
