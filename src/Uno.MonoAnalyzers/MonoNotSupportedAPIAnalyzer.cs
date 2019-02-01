using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MonoNotSupportedAPIAnalyzer : DiagnosticAnalyzer
	{
		internal const string Title = "This API is not available in VS15.8 and ealier";
		internal const string MessageFormat = "{0} is not available in VS15.8 and ealier";
		internal const string Description = "This API is not available in VS15.8 and ealier, choose a different overload";
		internal const string Category = "Compatibility";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			"Uno0002",
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(csa =>
			{
				var container = new Container(csa.Compilation, this);

				csa.RegisterSyntaxNodeAction(c => container.OnMemberAccessExpression(c), SyntaxKind.SimpleMemberAccessExpression);
			});
		}

		public class Container
		{
			private readonly MonoNotSupportedAPIAnalyzer _owner;
			private readonly INamedTypeSymbol _stringSymbol;
			private readonly INamedTypeSymbol _charSymbol;
			private readonly INamedTypeSymbol _stringComparisonSymbol;
			private readonly ValidationEntry[] _validateMembers;

			class ValidationEntry
			{
				public string[] Methods;
				public Func<IMethodSymbol, bool> Validation;
			}

			public Container(Compilation compilation, MonoNotSupportedAPIAnalyzer owner)
			{
				_owner = owner;
				_stringSymbol = compilation.GetTypeByMetadataName("System.String");
				_charSymbol = compilation.GetTypeByMetadataName("System.Char");
				_stringComparisonSymbol = compilation.GetTypeByMetadataName("System.StringComparison");

				_validateMembers = new [] {
					new ValidationEntry{
						Methods = new[] {
							"Split",
							"TrimStart",
							"TrimEnd",
							"Trim",
							"IndexOfAny",
							"Join",
							"StartsWith",
						},
						Validation = new Func<IMethodSymbol, bool>(
							m => m.Parameters.FirstOrDefault()?.Type == _charSymbol
						)
					},
					new ValidationEntry{
						Methods = new[] {
							"IndexOf",
						},
						Validation = new Func<IMethodSymbol, bool>(
							m => m.Parameters.ElementAtOrDefault(0)?.Type == _charSymbol &&
							m.Parameters.ElementAtOrDefault(1)?.Type == _stringComparisonSymbol
						)
					},
					new ValidationEntry{
						Methods = new[] {
							"Contains",
						},
						Validation = new Func<IMethodSymbol, bool>(
							m => m.Parameters.ElementAtOrDefault(1) != null
						)
					},
					new ValidationEntry{
						Methods = new[] {
							"Replace",
						},
						Validation = new Func<IMethodSymbol, bool>(
							m => m.Parameters.Length > 2
						)
					},
					new ValidationEntry{
						Methods = new[] {
							"Split",
						},
						Validation = new Func<IMethodSymbol, bool>(
							m => m.Parameters.FirstOrDefault() == _stringSymbol
						)
					},
					new ValidationEntry{
						Methods = new[] {
							"TrimWhitSpaceHelper",
							"CreateFromChar",
							"ArrayContains",
						},
						Validation = new Func<IMethodSymbol, bool>(
							m => true
						)
					},
				};
			}

			public void OnMemberAccessExpression(SyntaxNodeAnalysisContext contextAnalysis)
			{
				var memberAccess = contextAnalysis.Node as MemberAccessExpressionSyntax;
				var member = contextAnalysis.SemanticModel.GetSymbolInfo(memberAccess);

				if (member.Symbol != null && member.Symbol.ContainingSymbol == _stringSymbol)
				{
					if (member.Symbol is IMethodSymbol method)
					{
						var memberToValidate = _validateMembers.FirstOrDefault(v => v.Methods.Any(m => member.Symbol.Name == m && v.Validation(method)));

						if (memberToValidate != null)
						{
							var diagnostic = Diagnostic.Create(
								_owner.SupportedDiagnostics.First(),
								contextAnalysis.Node.GetLocation(),
								member.Symbol.ToDisplayString()
							);
							contextAnalysis.ReportDiagnostic(diagnostic);
						}
					}
				}
			}
		}
	}
}


